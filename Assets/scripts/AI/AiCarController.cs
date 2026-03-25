using UnityEngine;
using System.Linq;
using System;
using UnityEditor.Recorder.Input;
using System.Collections.Generic;
using Unity.Splines.Examples;

public class AiCarController : BaseCarController
{
    #pragma warning disable 0414
    // --- Constants ---
    private const float GROUND_RAY_LENGTH = 0.5f;
    private const float STEERING_DEAD_ZONE = 0.05f;
    private const float NODE_GIZMO_RADIUS = 0.5f;
    private static readonly Vector3 DEFAULT_CENTER_OF_MASS = new(0, -0.0f, 0);
        // --- Path Following ---
    [Header("Path Following Settings")]
    [Tooltip("Distance threshold for reaching a waypoint.")]
    [SerializeField] private float waypointThreshold = 10.0f;
    [Tooltip("Angle threshold for switching between straight lines and curves.")]
    [SerializeField] private float angleThreshold = 35.0f;

    // --- Steering ---
    [Header("Steering Settings")]
    [Tooltip("Left turn radius (how far the front left wheel can rotate).")]
    [SerializeField] private float leftTurnRadius = 10.0f;
    [Tooltip("Right turn radius (how far the front right wheel can rotate).")]
    [SerializeField] private float rightTurnRadius = 30.0f;

    [SerializeField] private int lookAheadIndex = 5;

    // --- Corner Slowdown ---
    [Header("AI Turn Slowdown Settings")]
    [Tooltip("Degrees: Only slow down for turns sharper than this.")]
    [SerializeField] private float slowdownThreshold = 30f;
    [Tooltip("Degrees: Max slowdown at this angle or above.")]
    [SerializeField] private float maxSlowdownAngle = 90f;
    [Tooltip("Minimum speed factor at max angle (e.g. 0.35 = 35% of Maxspeed).")]
    [SerializeField] private float minSlowdown = 0.35f;

    // --- Turn Detection ---
    [Header("Turn Detection Settings")]
    [Tooltip("Radius of the detection sphere for upcoming turns.")]
    [SerializeField] private float detectionRadius = 7.0f;
    [Tooltip("Tolerance in angle for deviation from the path")]
    [SerializeField] private float curveTolerance = 2.0f;

    // --- Avoidance ---
    [Header("Avoidance Settings")]
    [Tooltip("Extra buffer distance added to the safe radius for avoidance checks.")]
    [SerializeField] private float avoidanceBuffer = 5.0f;
    [Tooltip("How far to offset laterally when dodging another car.")]
    [SerializeField] private float avoidanceLateralOffset = 2.0f;
    [SerializeField] private float maxAvoidanceOffset = 8f;
    [SerializeField] private int objectAvoidanceBeams = 10;
    [SerializeField] private float avoidanceBeamLenght = 10.0f;
    [SerializeField] private float beamAngle = 45f;
    private List<Ray> rays = new();
    public float safeRadius { get; private set; }

    // --- Boost ---
    [Header("Boost Settings")]
    [Tooltip("Multiplier applied to speed and acceleration when boosting.")]
    [SerializeField] private float boostMultiplier = 1.25f;
    private bool isBoosting = false;
    // --- References ---
    [Header("References")]
    public AiCarManager aiCarManager;
    private Vector3 targetPoint;
    private int currentWaypointIndex = 0;
    private int waypointSize;
    // Amount of points to sample when looking at a curve to find moving speed
    private int turnLookAhead = 4;
    private BaseCarController.Wheel[] frontWheels = Array.Empty<BaseCarController.Wheel>();
    private float targetTorque;
    private float moveInput = 0f;
    private LayerMask objectLayerMask;
    private float steerInput;
    private float avoidance;
    // Used for calculating the speed on curves
    private float speedLimit;
    public AiCarController Initialize(
        AiCarManager aiCarManager, 
        AiCarManager.DifficultyStats difficultyStats
        )
    {
        this.aiCarManager = aiCarManager;
        Maxspeed = difficultyStats.maxSpeed;
        MaxAcceleration = difficultyStats.maxAcceleration;
        avoidance = difficultyStats.avoidance;
        return this;
    }

    private void Awake()
    {
        
        frontWheels = Wheels.Where(w => w.Axel == Axel.Front).ToArray();
        if (CarRb == null) CarRb = GetComponentInChildren<Rigidbody>();
        CarRb.centerOfMass = DEFAULT_CENTER_OF_MASS;

        carCollider = GetComponentInChildren<Collider>();
        if (carCollider != null)
        {
            CarWidth = carCollider.bounds.size.x;
            CarLength = carCollider.bounds.size.z;
        }
    }

    override protected void Start()
    {
        Grass = LayerMask.NameToLayer("Grass");
        objectLayerMask = ~LayerMask.NameToLayer("Grass");

        waypointSize = aiCarManager.Waypoints.Count();
        targetPoint = aiCarManager.Waypoints[0];
        speedLimit = Mathf.Min(Mathf.Sqrt(Maxspeed * BezierMath.GetRadius(targetPoint, aiCarManager.Waypoints[(currentWaypointIndex + 1) % waypointSize], aiCarManager.Waypoints[(currentWaypointIndex + 2) % waypointSize])), Maxspeed);
        

        base.Start();

        safeRadius = Mathf.Max(CarWidth, CarLength) * 0.5f;
    }

    private void FixedUpdate()
    {
        // Gravity
        if (Physics.Raycast(CarRb.position, Vector3.down, GROUND_RAY_LENGTH)) CarRb.AddForce(GravityMultiplier * Physics.gravity.magnitude * Vector3.down, ForceMode.Acceleration);

        // Set new waypoint if close enough to current
        if (Vector3.Distance(CarRb.position, aiCarManager.Waypoints[currentWaypointIndex]) < waypointThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypointSize;
            speedLimit = Mathf.Min(Mathf.Sqrt(Maxspeed * aiCarManager.PointRadi[currentWaypointIndex]), Maxspeed) / 3.6f;
        }
        targetPoint = aiCarManager.Waypoints[currentWaypointIndex];


        // Prevent car from jiggling when already pointing at the target
        if (Vector3.Angle(CarRb.rotation.eulerAngles.normalized, targetPoint.normalized) > curveTolerance)
        {
            // Rotate car itself
            CarRb.rotation = Quaternion.Lerp(
                CarRb.rotation,
                Quaternion.LookRotation(
                    new Vector3(targetPoint.x - CarRb.position.x, 0, targetPoint.z - CarRb.position.z)
                ),
                TurnSensitivity
            );

            // Turn wheels
            foreach (Wheel wheel in frontWheels) wheel.WheelCollider.steerAngle = CarRb.rotation.y;
        }

        AvoidObstacles();
        ApplyDriveInputs();
        ApplySpeedLimit(speedLimit);
    }

    private void ApplyDriveInputs()
    {
        moveInput = 1.0f;
        targetTorque = moveInput * MaxAcceleration;

        if (Mathf.Abs(steerInput) > 0.5f)
        {
            targetTorque *= 0.5f;
        }

        if (IsOnGrass())
        {
            targetTorque *= GrassSpeedMultiplier;
        }

        // Apply boost if active
        if (isBoosting)
        {
            speedLimit = (Maxspeed * boostMultiplier) + 20f;
            targetTorque *= boostMultiplier;
        }

        foreach (Wheel wheel in Wheels)
        {
            wheel.WheelCollider.motorTorque = targetTorque;
            wheel.WheelCollider.brakeTorque = 0f;
        }
    }

    private void AvoidObstacles()
    {
        Vector3 localTarget = CarRb.gameObject.transform.InverseTransformPoint(targetPoint);
        Vector3 localPosition = localTarget;
        // HashSet<GameObject> hitObjects = new();

        // for (int i = objectAvoidanceBeams / -2; i <= objectAvoidanceBeams / 2; i++)
        // {
        //     // For some reason the object layer mask doesnt work
        //     if (Physics.Raycast(origin:CarRb.position + CarRb.transform.up, direction:Quaternion.AngleAxis(beamAngle / objectAvoidanceBeams * i - beamAngle / 2f, CarRb.transform.up) * CarRb.transform.forward, maxDistance:avoidanceBeamLenght, hitInfo:out RaycastHit hit))
        //     {
        //         GameObject go = hit.transform.gameObject;
        //         if (go == null || hitObjects.Contains(go)) continue;

        //         BaseCarController carController = go.GetComponentInChildren<BaseCarController>();
        //         if (carController != null || go.layer == objectLayerMask)
        //         {
        //             int sign = Math.Sign(i);
        //             if (sign == 0) sign = -1;
        //             localPosition.x += (Math.Abs(i) - objectAvoidanceBeams / 2f + avoidanceLateralOffset) * sign;
        //             hitObjects.Add(go);
        //             Debug.Log($"doing {(Math.Abs(i) - objectAvoidanceBeams / 2f + avoidanceLateralOffset) * sign}");
        //         }
        //     }
        // }

        foreach (BaseCarController other in GameManager.instance.spawnedCars)
        {
            if (other == this) continue;

            Vector3 toOther = other.CarRb.position - CarRb.position;
            float distance = toOther.magnitude;
            float otherSafeRadius = Mathf.Max(other.CarWidth, other.CarLength) * 0.5f;
            float minSafeDistance = safeRadius + otherSafeRadius + avoidanceBuffer;

            if (distance < minSafeDistance && Vector3.Dot(CarRb.transform.forward, toOther.normalized) > 0.5f)
            {
                Vector3 myFuturePos = CarRb.position + CarRb.linearVelocity * 0.5f;
                Vector3 otherFuturePos = other.CarRb.position + other.CarRb.linearVelocity * 0.5f;
                float futureDist = (myFuturePos - otherFuturePos).magnitude;

                if (futureDist < minSafeDistance)
                {
                    float steerDirection = Vector3.Cross(CarRb.transform.forward, toOther).y > 0 ? -1f : 1f;
                    if (localPosition.x == 0) localPosition.x += avoidanceBuffer * steerDirection;
                    localPosition.x += steerDirection * avoidanceLateralOffset;

                    if (distance < minSafeDistance * 0.5f && CarRb.linearVelocity.magnitude > other.CarRb.linearVelocity.magnitude)
                    {
                        moveInput = 0.7f;
                    }

                }
            }
        }
        
        if (Vector3.Distance(localTarget, localPosition) > maxAvoidanceOffset) localPosition.x = maxAvoidanceOffset * Mathf.Sign(localPosition.x);
        targetPoint = CarRb.gameObject.transform.TransformPoint(localPosition);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPoint, 0.5f);
        Gizmos.DrawLine(CarRb.position, targetPoint);
        
        for (int i = 1; i <= objectAvoidanceBeams; i++)
        {
            Gizmos.DrawRay(CarRb.position + CarRb.transform.up, Quaternion.AngleAxis(beamAngle / objectAvoidanceBeams * i - beamAngle / 2f, CarRb.transform.up) * CarRb.transform.forward * avoidanceBeamLenght);
        } 
    }
    #endif
}