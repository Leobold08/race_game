using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections;
using Logitech;
using System.Linq;
using PurrNet;

public class CarController : NetworkBehaviour
{

    CarInputActions Controls;

    RacerScript racerScript;

    public enum Axel
    {
        Front,
        Rear
    }

    [Serializable]
    public struct Wheel
    {
        public GameObject wheelModel;
        public WheelCollider wheelCollider;

        public GameObject wheelEffectobj;
        public ParticleSystem smokeParticle;
        public Axel axel;
    }

    [Header("Auton asetukset")]
    public float maxAcceleration = 700.0f;
    public float brakeAcceleration = 3.0f;
    [Header("turn asetukset")]
    public float turnSensitivty = 1.0f;
    public float turnSensitivtyAtHighSpeed = 1.0f, turnSensitivtyAtLowSpeed = 1.0f;
    public float deceleration = 1.0f;
    [Min(100.0f)]
    public float maxspeed = 100.0f;
    public float gravityMultiplier = 1.5f;
    public float grassSpeedMultiplier = 0.5f;
    public List<Wheel> wheels;
    float moveInput, steerInput;
    public Vector3 _centerofMass;
    public LayerMask grass;
    public float targetTorque;
    public Material grassMaterial, roadMaterial, driftmaterial;
    public Rigidbody carRb;
    public bool isTurboActive = false;
    private float activedrift = 0.0f;
    public float Turbesped = 150.0f, basespeed = 100.0f, grassmaxspeed = 50.0f, driftMaxSpeed = 40f;
    [Header("Drift asetukset")]
    public float driftMultiplier = 1.0f;
    public bool isTurnedDown = false, isDrifting;
    private float perusMaxAccerelation, perusTargetTorque, throttlemodifier, smoothedMaxAcceleration, modifiedMaxAcceleration;
    //fuck this shit im doing this controller keyboard the fucking lazy/shit way!!!!!!!!!!
    private PlayerInput playerInput;
    private string currentControlScheme = "Keyboard";


    [Header("turbe asetukset")]
    public Image turbeMeter;
    public float turbeAmount = 100.0f, turbeMax = 100.0f, turbepush = 50.0f;
    public float turbeReduce;
    public float turbeRegen;


    public bool isRegenerating = false;
    public int turbeRegenCoroutineAmount = 0;
    Dictionary<string, float> carTurboValues = new Dictionary<string, float>();

    public bool canDrift = false;
    public bool canUseTurbo = false;

    public bool GrassRespawnActive = false;

    

    void Awake()
    {
        Controls = new CarInputActions();
        Controls.Enable();
        turbeMeter = GameObject.Find("turbeFull").GetComponent<Image>();
    }

    void Start()
    {
        perusMaxAccerelation = maxAcceleration;
        smoothedMaxAcceleration = perusMaxAccerelation;
        perusTargetTorque = targetTorque;
        if (carRb == null)
            carRb = GetComponent<Rigidbody>();
        carRb.centerOfMass = _centerofMass;
        if (GameManager.instance.sceneSelected != "tutorial")
        {
            canDrift = true;
            canUseTurbo = true;
        }
        BoostsForEachCar(carsParent: GameObject.Find("cars"));

        racerScript = FindAnyObjectByType<RacerScript>();

        InitializeLogitechWheel(); 
    }

    // PurrNet: only the owner should run this controller in multiplayer
    protected override void OnSpawned()
    {
        base.OnSpawned();
        // In networked games, disable this behaviour for non-owners so
        // only the local player instance processes input and physics.
        enabled = isOwner;

        // Camera check: if this car has a camera attached (for example a
        // follow or cockpit camera on the prefab), only enable it for
        // the owning player so remote cars do not drive a camera.
        var cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.enabled = isOwner;

            var audioListener = cam.GetComponent<AudioListener>();
            if (audioListener != null)
                audioListener.enabled = isOwner;
        }
    }

    private void OnControlsChanged(PlayerInput input)
    {
        currentControlScheme = input.currentControlScheme;
    }


     // handlers
    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        steerInput = ctx.ReadValue<Vector2>().x;
    }
    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        steerInput = 0f;
    }

    void OnApplicationFocus(bool focus)
    {
        if (focus){
            if (logitechInitialized && LogitechGSDK.LogiIsConnected(0))
            {
                LogitechGSDK.LogiUpdate();
            }
        }
    }


    private void OnEnable()
    {
        Controls.Enable();
        if (playerInput != null)
            playerInput.onControlsChanged += OnControlsChanged;

        // INPUT SUBSCRIPTIONS: KERRAN
        Controls.CarControls.Move.performed += OnMovePerformed;
        Controls.CarControls.Move.canceled  += OnMoveCanceled;

        Controls.CarControls.Drift.performed   += OnDriftPerformed;
        Controls.CarControls.Drift.canceled    += OnDriftCanceled;
    }

    private void OnDisable()
    {
        Controls.Disable();
        if (playerInput != null)
            playerInput.onControlsChanged -= OnControlsChanged;

        // UNSUBSCRIBE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Controls.CarControls.Move.performed -= OnMovePerformed;
        Controls.CarControls.Move.canceled  -= OnMoveCanceled;
        Controls.CarControls.Drift.performed -= OnDriftPerformed;
        Controls.CarControls.Drift.canceled  -= OnDriftCanceled;
        StopAllForceFeedback();
    }

    private void OnDestroy()
    {
        Controls.Disable();
        Controls.Dispose();
        
        StopAllForceFeedback();
    }

    public float GetSpeed()
    {
        GameManager.instance.carSpeed = carRb.linearVelocity.magnitude * 3.6f;
        return carRb.linearVelocity.magnitude * 3.6f;
    }

    public float GetMaxSpeed()
    {
        return maxspeed;
    }

    void Update()
    {
        GetInputs();
        Animatewheels();
        // detect connection state changes and print once when it changes
        bool currentlyConnected = logitechInitialized && LogitechGSDK.LogiIsConnected(0);
        if (currentlyConnected != lastLogiConnected)
        {
            lastLogiConnected = currentlyConnected;
            Debug.Log($"[CarController] Logitech connection status: {(currentlyConnected ? "Connected" : "Disconnected")}");
        }

        if (logitechInitialized && LogitechGSDK.LogiIsConnected(0))
        {
            LogitechGSDK.LogiUpdate();
            GetLogitechInputs();
            ApplyForceFeedback(); 
        }
    }

    bool isOnGrassCached;
    bool isOnGrassCachedValid;

    void FixedUpdate()
    {
        float speed = carRb.linearVelocity.magnitude * 3.6f;
        isOnGrassCachedValid = false;
        ApplySpeedLimit(speed);

        if (!IsCarActive()) return;
        
        UpdateDriftSpeed();
        StopDriftIfRaceFinished();

        ApplyGravity();
        Move();
        Steer();

        Decelerate();
        Applyturnsensitivity(speed);
        OnGrass();
        HandleTurbo();

        WheelEffects(isDrifting);
    }

    bool IsCarActive()
    {
        // jos tulee random paskaa kuten peli paussi tai auto kolaroi, LISÄTKÄÄ SE TÄHÄN EI MISSÄN NIMESSÄ FIXEDUPDATEEN!!!!!!!!!!!
        return true;
    }

    void UpdateDriftSpeed()
    {
        if (!isDrifting) return;


        if (isTurboActive)
            maxspeed = Mathf.Lerp(maxspeed, Turbesped, Time.deltaTime * 0.5f);

        maxspeed = Mathf.Lerp(maxspeed, driftMaxSpeed, Time.deltaTime * 0.1f);
    }

    void StopDriftIfRaceFinished()
    {
        if (racerScript == null) return;
        if (!racerScript.raceFinished) return;
        if (activedrift <= 0) return;

        StopDrifting();
    }



    void HandleTurbo()
    {
        if (!canUseTurbo) return;
        TURBE();
        TURBEmeter();
    }

        void OnGrass()
        {
            int wheelsOnGrass = 0;

            foreach (var wheel in wheels)
            {
                if (wheel.wheelEffectobj == null) continue;

                var trailRenderer = wheel.wheelEffectobj.GetComponentInChildren<TrailRenderer>();
                if (trailRenderer == null) continue;

                bool wheelOnGrass = IsWheelGrounded(wheel) && IsWheelOnGrass(wheel);

                // per‑wheel line material
                trailRenderer.material = wheelOnGrass ? grassMaterial : roadMaterial;

                if (wheelOnGrass)
                    wheelsOnGrass++;
            }

            // 1 oli liian kiree pisteen vähenemiseen, 2 on parempi
            const int wheelsNeededForPenalty = 2;   

            bool onGrassForScore = wheelsOnGrass >= wheelsNeededForPenalty;

            if (ScoreManager.instance != null)
            {
                ScoreManager.instance.SetOnGrass(onGrassForScore);
            }
        }

    void GetInputs()
    {
        // Read steering from Move action (for keyboard/gamepad)
        if (!logitechInitialized || !LogitechGSDK.LogiIsConnected(0))
        {
            steerInput = Controls.CarControls.Move.ReadValue<Vector2>().x;
        }
        
        if (Controls.CarControls.MoveForward.IsPressed())
            moveInput = Controls.CarControls.MoveForward.ReadValue<float>();
        else if (Controls.CarControls.MoveBackward.IsPressed())
            moveInput = -Controls.CarControls.MoveBackward.ReadValue<float>();
        else
            moveInput = 0f;

        if (!Controls.CarControls.Drift.IsPressed())
            StopDrifting();

        throttlemodifier = Controls.CarControls.ThrottleMod.ReadValue<float>();
    }

    bool IsWheelGrounded(Wheel wheel)
    {
        return Physics.Raycast(wheel.wheelCollider.transform.position, -wheel.wheelCollider.transform.up, out RaycastHit hit, wheel.wheelCollider.radius + wheel.wheelCollider.suspensionDistance);
    }

    bool IsWheelOnGrass(Wheel wheel)
    {
        if (Physics.Raycast(
                wheel.wheelCollider.transform.position,
                -wheel.wheelCollider.transform.up,
                out RaycastHit hit,
                wheel.wheelCollider.radius + wheel.wheelCollider.suspensionDistance))
        {
            // check if hit collider is on a grass layer
            return (grass.value & (1 << hit.collider.gameObject.layer)) != 0;
        }
        return false;
    }


    public bool IsOnGrass()
    {
        foreach (var wheel in wheels)
        {
            if (IsWheelGrounded(wheel) && IsWheelOnGrass(wheel))
            {
                if (GrassRespawnActive)
                    racerScript.RespawnAtLastCheckpoint();
                return true;
            }
        }
        return false;
    }

    bool IsOnGrassCached()
    {
        if (!isOnGrassCachedValid)
        {
            isOnGrassCached = IsOnGrass();
            isOnGrassCachedValid = true;
        }
        return isOnGrassCached;
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 0.02f);
    }

    void ApplySpeedLimit(float speed)
    {
        if (speed <= maxspeed) return;
        carRb.linearVelocity = carRb.linearVelocity.normalized * (maxspeed / 3.6f);
    }

    void Applyturnsensitivity(float speed)
    {
        turnSensitivty = Mathf.Lerp(
            turnSensitivtyAtLowSpeed,
            turnSensitivtyAtHighSpeed,
            Mathf.Clamp01(speed / maxspeed));
    }

    void TURBE()
    {
        //uskon että tää on tarpeeton; viittauksia KOMMENTEISSA yhessä scriptis, ei missää muualla
        //tätä ei myöskää muuteta koskaan...
        if (isTurnedDown)
        {
            isTurboActive = false;
            return;
        }
        isTurboActive = Controls.CarControls.turbo.IsPressed() && turbeAmount > 0;
        if (isTurboActive)
        {
            carRb.AddForce(transform.forward * turbepush, ForceMode.Acceleration);
            targetTorque *= 1.5f;
        }
    }

    void Move()
    {
        //HandeSteepSlope();
        UpdateTargetTorgue();
        AdjustSpeedForGrass();
        AdjustSuspension();
        foreach (var wheel in wheels)
        {
            if (Controls.CarControls.Brake.IsPressed())
            {
                Brakes(wheel);
            }
            else
            {
                MotorTorgue(wheel);
            }
        }
    }

    //kommentoin tämän pois koska jos meille tulee se mistä aarre puhu eli autot menee ylös ja alas tiellä niin tämähän estää sen
    // private void HandeSteepSlope()
    // {
    //     if (IsOnSteepSlope())
    //     {
    //         targetTorque *= 0.5f;
    //         carRb.linearVelocity = Vector3.ClampMagnitude(carRb.linearVelocity, maxspeed / 3.6f);
    //     }
    // }

    private void AdjustSuspension()
    {
        foreach (var wheel in wheels)
        {
            JointSpring suspensionSpring = wheel.wheelCollider.suspensionSpring;
            suspensionSpring.spring = 8000.0f;
            suspensionSpring.damper = 5000.0f;
            wheel.wheelCollider.suspensionSpring = suspensionSpring;
        }
    }

    private void AdjustForwardFrictrion()
    {
        foreach (var wheel in wheels)
        {
            WheelFrictionCurve forwardFriction = wheel.wheelCollider.forwardFriction;
            forwardFriction.extremumSlip = 0.6f;
            forwardFriction.extremumValue = 1;
            forwardFriction.asymptoteSlip = 1.0f;
            forwardFriction.asymptoteValue = 1;
            forwardFriction.stiffness = 3.5f;
            wheel.wheelCollider.forwardFriction = forwardFriction;
        }
    }

    private void UpdateTargetTorgue()
    {
        float inputValue = currentControlScheme == "Gamepad"
            ? Controls.CarControls.ThrottleMod.ReadValue<float>()
            : Mathf.Abs(moveInput);

        float power = currentControlScheme == "Gamepad" ? 0.9f : 0.1f;

        float throttle = Mathf.Pow(inputValue, power);
        
        // Reduce power during drift but don't eliminate it
        float driftPowerMultiplier = isDrifting ? 0.7f : 1.0f;
        float targetMaxAcc = perusMaxAccerelation * Mathf.Lerp(0.4f, 1f, throttle) * driftPowerMultiplier;

        smoothedMaxAcceleration = Mathf.MoveTowards(
            smoothedMaxAcceleration,
            targetMaxAcc,
            Time.deltaTime * 250f
        );

        if (moveInput > 0f)
            targetTorque = smoothedMaxAcceleration;
        else if (moveInput < 0f)
            targetTorque = -smoothedMaxAcceleration;
        else
            targetTorque = 0f;

        if (!isDrifting)
        {
            float targetMaxSpeed = isTurboActive ? Turbesped : basespeed;
            maxspeed = Mathf.Lerp(maxspeed, targetMaxSpeed, Time.deltaTime);
        }
    }

    private void Brakes(Wheel wheel)
    {
        GameManager.instance.StopAddingPoints();
        wheel.wheelCollider.brakeTorque = brakeAcceleration * 15f;
    }

    private void MotorTorgue(Wheel wheel)
    {
        wheel.wheelCollider.motorTorque = targetTorque;
        wheel.wheelCollider.brakeTorque = 0f;
    }

    private void AdjustSpeedForGrass()
    {
        if (IsOnGrassCached() && !isDrifting)
        {
            targetTorque *= grassSpeedMultiplier;
            maxspeed = Mathf.Lerp(maxspeed, grassSpeedMultiplier, Time.deltaTime);
            if (GameManager.instance.carSpeed < 50.0f)
            {
                maxspeed = 50.0f;
            }
        }
    }

    void Decelerate()
    {
        if (moveInput == 0)
        {
            Vector3 velocity = carRb.linearVelocity;

            velocity -= velocity.normalized * deceleration * 2.0f * Time.deltaTime;

            if (velocity.magnitude < 0.1f)
            {
                velocity = Vector3.zero;
            }
            carRb.linearVelocity = velocity;
        }
    }

    float GetSteeringInput()
    {
        if (currentControlScheme == "Keyboard" && Mathf.Abs(steerInput) > 0f)
            return Mathf.Sign(steerInput) * Mathf.Pow(Mathf.Abs(steerInput), 0.8f);
        return steerInput;
    }

    void Steer()
    {
        float effectiveInput = GetSteeringInput();
        foreach (var wheel in wheels.Where(w => w.axel == Axel.Front))
        {
            var _steerAngle = steerInput * turnSensitivty * (isDrifting ? 0.45f : 0.35f);
            wheel.wheelCollider.steerAngle = Mathf.Lerp(wheel.wheelCollider.steerAngle, _steerAngle, 0.6f);            
        }
    }

    
    void ApplyGravity()
    {
        if (!IsGrounded())
        {
            carRb.AddForce(Vector3.down * gravityMultiplier * Physics.gravity.magnitude, ForceMode.Acceleration);
        }

    }




    public float GetDriftSharpness()
    {
        if (isDrifting)
        {
            Vector3 velocity = carRb.linearVelocity;
            Vector3 forward = transform.forward;
            float angle = Vector3.Angle(forward, velocity);
            return angle;  
        }
        //checks the angle between the car's forward direction and its velocity vector constantly while drifting
        return 0.0f;
    }

    private void AdjustWheelsForDrift()
    {
        foreach (var wheel in wheels)
        {
            JointSpring suspensionSpring = wheel.wheelCollider.suspensionSpring;
            suspensionSpring.spring = 4000.0f;
            suspensionSpring.damper = 1000.0f;
            wheel.wheelCollider.suspensionSpring = suspensionSpring;
        }

        foreach (var wheel in wheels)
        {
            WheelFrictionCurve forwardFriction = wheel.wheelCollider.forwardFriction;
            forwardFriction.extremumSlip = 0.4f;
            forwardFriction.asymptoteSlip = 0.6f;
            forwardFriction.extremumValue = 1;
            forwardFriction.asymptoteValue = 1;
            forwardFriction.stiffness = 3f;
            wheel.wheelCollider.forwardFriction = forwardFriction;
            
            if (wheel.axel == Axel.Front)
            {
                WheelFrictionCurve sidewaysFriction = wheel.wheelCollider.sidewaysFriction;
                sidewaysFriction.stiffness = 2.0f;
                wheel.wheelCollider.sidewaysFriction = sidewaysFriction;
            }
        }
    }

    void StopDrifting()
    {
        activedrift = 0;
        isDrifting = false;
        maxAcceleration = perusMaxAccerelation;
        carRb.angularDamping = 0.05f; 

        if (racerScript != null &&
            (racerScript.raceFinished || GameManager.instance.carSpeed < 20.0f))
        {
            GameManager.instance.StopAddingPoints();
            return;
        }
        GameManager.instance.StopAddingPoints();

        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider == null) continue;

            WheelFrictionCurve sidewaysFriction = wheel.wheelCollider.sidewaysFriction;
            sidewaysFriction.extremumSlip = 0.2f;
            sidewaysFriction.asymptoteSlip = 0.4f;
            sidewaysFriction.extremumValue = 1.0f;
            sidewaysFriction.asymptoteValue = 1f;
            sidewaysFriction.stiffness = 5f;
            wheel.wheelCollider.sidewaysFriction = sidewaysFriction;
        }
    }

    void Animatewheels()
    {
        foreach (var wheel in wheels)
        {
            Quaternion rot;
            Vector3 pos;
            wheel.wheelCollider.GetWorldPose(out pos, out rot);
            wheel.wheelModel.transform.position = pos;
            wheel.wheelModel.transform.rotation = rot;
        }
    }


    void BoostsForEachCar(GameObject carsParent)
    {
        int childCount = carsParent.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            GameObject car = carsParent.transform.GetChild(i).gameObject;
            if (car.activeInHierarchy)
            {
                string carName = car.name;
                switch (carName)
                {
                    //blue car  
                    case "REALCAR":
                        turbepush = 15.0f;
                        ScoreManager.instance.SetScoreMultiplier(1.5f);
                        break;
                    //gray car
                    case "REALCAR_x":
                        turbeMax = 75.0f;
                        turbeRegen = 30.0f;
                        turbepush = 20.0f;
                        break;
                    //purple car
                    case "REALCAR_y":
                        turbepush = 7.0f;
                        ScoreManager.instance.SetScoreMultiplier(1.0f);
                        break;
                    //da Lada
                    case "Lada":
                        turbeMax = 30.0f;
                        turbepush = 500.0f;
                        ScoreManager.instance.SetScoreMultiplier(0.75f);
                        break;
                    default:
                        Debug.LogWarning($"Unknown car name: {carName}");
                        break;
                }
                carTurboValues[carName] = turbepush;
                return;
            }
        }
    }
    //bobbing effect

    /// <summary>
    /// does wheel effects
    /// </summary>
    void WheelEffects(bool enable)
    {
        foreach (var wheel in wheels)
        {
            if (wheel.axel != Axel.Rear)
                continue;

            if (wheel.wheelEffectobj == null)
                continue;

            var trailRenderer = wheel.wheelEffectobj.GetComponentInChildren<TrailRenderer>();
            if (trailRenderer == null)
                continue;


            bool wheelGrounded = IsWheelGrounded(wheel);
            bool shouldEmit = enable && wheelGrounded;

            // Always ensure we can re-enable emitting even if it was previously cleared/disabled.
            if (shouldEmit)
            {
                // Force emitting true when we want it on
                if (!trailRenderer.emitting)
                    trailRenderer.emitting = true;

                if (wheel.smokeParticle != null && !wheel.smokeParticle.isPlaying)
                    wheel.smokeParticle.Play();
            }
            else
            {
                if (trailRenderer.emitting)
                    trailRenderer.emitting = false;

                if (wheel.smokeParticle != null && wheel.smokeParticle.isPlaying)
                    wheel.smokeParticle.Stop();
            }
        }
    }

    public void ClearWheelTrails()
    {
        foreach (var wheel in wheels)
        {
            if (wheel.wheelEffectobj == null) continue;
            var trail = wheel.wheelEffectobj.GetComponentInChildren<TrailRenderer>();
            if (trail == null) continue;

            // explicitly stop emitting and clear the trail so it can be re-enabled later
            trail.emitting = false;
            trail.Clear();
            // keep the component enabled so emitting can be toggled again
            trail.enabled = true;
        }
        
    }

    /// <summary>
    /// käytetään TURBEmeterin päivittämiseen joka frame
    /// </summary>
    void TURBEmeter()
    {
        if (isTurboActive && turbeAmount != 0) //jos käytät turboa ja sitä o jäljellä
        {
            GameManager.instance.turbeActive = true;

            if (turbeRegenCoroutineAmount > 0)
            {
                turbeRegenCoroutines("stop");
            }
            isRegenerating = false;
            turbeRegenCoroutineAmount = 0;

            turbeAmount -= turbeReduce * Time.deltaTime;
        }
        else if (!isTurboActive && turbeAmount < turbeMax) //jos et käytä turboa ja se ei oo täynnä
        {

            GameManager.instance.turbeActive = false;

            if (turbeRegenCoroutineAmount == 0 && isRegenerating == false)
            {
                turbeRegenCoroutines("start");
                turbeRegenCoroutineAmount += 1;
            }
        }

        if (turbeAmount < 0)
        {
            turbeAmount = 0;
        }
        if (turbeAmount > turbeMax)
        {
            //Debug.Log("I bought a property in Egypt, and what they do is they give you the property");
            turbeAmount = turbeMax;

            turbeRegenCoroutines("stop");
            isRegenerating = false;
            turbeRegenCoroutineAmount = 0;
        }

        turbeMeter.fillAmount = turbeAmount / turbeMax;
    }

    /// <summary>
    /// käytetään TURBEn regeneroimiseen
    /// ...koska fuck C#
    /// </summary>
    private IEnumerator turbeRegenerate()
    {
        yield return new WaitForSecondsRealtime(2.0f);
        isRegenerating = true;

        if (isRegenerating && turbeRegenCoroutineAmount == 1)
        {
            while (isRegenerating && turbeRegenCoroutineAmount == 1)
            {
                yield return StartCoroutine(RegenerateTurbeAmount());
            }
        }
        else
        {
            Debug.Log("stopped regen coroutine");
            yield break;
            //scriptin ei pitäs päästä tähä tilanteeseen missään vaiheessa, mutta se on täällä varmuuden vuoksi
        }
    }

    private IEnumerator RegenerateTurbeAmount()
    {
        turbeAmount += turbeRegen * Time.deltaTime;
        yield return null; // Wait for the next frame
    }

    /// <summary>
    /// aloita tai pysäytä TURBEn regenerointi coroutine
    /// </summary>
    /// <param name="option">start / stop</param>
    private void turbeRegenCoroutines(string option)
    {
        switch (option)
        {
            case "start":
                StartCoroutine("turbeRegenerate");
                break;

            case "stop":
                StopCoroutine("turbeRegenerate");
                break;
        }
    }

    //i hate this so much
    void OnDriftPerformed(InputAction.CallbackContext ctx)
    {
        if (isDrifting || GameManager.instance.isPaused || !canDrift) return;

        activedrift++;
        isDrifting = true;

        maxAcceleration = perusMaxAccerelation * 0.6f;

        foreach (var wheel in wheels)
        {
            if (wheel.wheelCollider == null) continue;
            WheelFrictionCurve sideways = wheel.wheelCollider.sidewaysFriction;
            sideways.extremumSlip   = 0.7f;
            sideways.asymptoteSlip  = 1.05f;
            sideways.extremumValue  = 1f;
            sideways.asymptoteValue = 1.2f;
            sideways.stiffness      = 2f;
            wheel.wheelCollider.sidewaysFriction = sideways;
        }

        carRb.angularDamping = 0.01f;
        AdjustWheelsForDrift();
        WheelEffects(true);
    }

    void OnDriftCanceled(InputAction.CallbackContext ctx)
    {
        StopDrifting();
        AdjustForwardFrictrion();
        maxAcceleration = perusMaxAccerelation;
        targetTorque = perusTargetTorque;
        WheelEffects(false);
    }




    [Header("Logitech G923 Settings")]
    public bool useLogitechWheel = true;
    public float forceFeedbackMultiplier = 1.0f;
    private bool logitechInitialized = false;
    private bool lastLogiConnected = false;

    void InitializeLogitechWheel()
    {
        if (!useLogitechWheel)
        {
            Debug.Log("[CarController] useLogitechWheel is false — skipping Logitech initialization.");
            return;
        }

        logitechInitialized = LogitechSDKManager.Initialize();
        if (logitechInitialized)
            Debug.Log("[CarController] Logitech wheel initialized successfully.");
        else
            Debug.LogWarning("[CarController] Logitech wheel failed to initialize.");
    }

    void GetLogitechInputs()
    {
        if (!LogitechGSDK.LogiIsConnected(0)) return;

        var state = LogitechGSDK.LogiGetStateUnity(0);
        steerInput = state.lX / 32768.0f;
        
        // Logitech pedals: -32768 (fully pressed) to 32767 (not pressed)
        // Invert and normalize to 0-1 range
        float throttle = Mathf.Clamp01(-state.lY / 32768.0f);
        
        // Clutch pedal for reverse - rglSlider is an array, index 0 is clutch
        float clutch = Mathf.Clamp01(-state.rglSlider[0] / 32768.0f);
        
        if (clutch > 0.1f)
            moveInput = -clutch;
        else if (throttle > 0.1f)
            moveInput = throttle;
        else
            moveInput = 0f;
    }

    void StopAllForceFeedback()
    {
        if (!logitechInitialized || !LogitechGSDK.LogiIsConnected(0)) return;
        
        LogitechGSDK.LogiStopDirtRoadEffect(0);
    }

    void ApplyForceFeedback()
    {
        if (!LogitechSDKManager.IsReady) return;

        if (GameManager.instance.isPaused)
        {
            LogitechGSDK.LogiStopDirtRoadEffect(0);
            return;
        }
        if (!logitechInitialized || !LogitechGSDK.LogiIsConnected(0)) return;
        
        float speed = carRb.linearVelocity.magnitude * 3.6f;

        // Continuously apply spring force (centering)
        int springStrength = Mathf.RoundToInt(40 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlaySpringForce(0, 0, 100, springStrength);

        // Continuously apply damper force (resistance) for steering
        int damperStrength = Mathf.RoundToInt(10 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlayDamperForce(0, damperStrength);
        
        // Dirt road only when on grass and moving
        if (IsOnGrassCached() && speed >= 10)
            LogitechGSDK.LogiPlayDirtRoadEffect(0, Mathf.RoundToInt(12.5f * forceFeedbackMultiplier));
        else
            LogitechGSDK.LogiStopDirtRoadEffect(0);
    }
}
