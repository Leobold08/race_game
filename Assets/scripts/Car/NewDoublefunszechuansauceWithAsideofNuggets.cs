using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Logitech;


[RequireComponent(typeof(Rigidbody))]
public class NewDoublefunszechuansauceWithAsideofNuggets : BaseCarController
{
    public CarInputActions Controls { get; protected set; } //new CarInputActions();

    LogitechMovement LGM;
    //PlayerInput PlayerInput;

    string CurrentControlScheme;

    [SerializeField] private GameObject carLights;
    private Material carLightsMaterial;
    [SerializeField] private Material PixelCount;

    [SerializeField] private float steerDeadzone = 0.15f;

    [SerializeField] private int minGroundedWheelsForDrive = 2;

    float basePixel;
    float minPixel = 32f;
    float recoverTime = 2f;
    private bool isBraking = false;
    Coroutine PixelRecovery;
    private MultCounter multCounter;
    [Range(0f, 1f)]
    public float driftAmount = 0.5f;


    protected override void Awake()
    {
        CarRb = GetComponent<Rigidbody>();
        carLightsMaterial = GetComponentInChildren<Renderer>().materials[1];
        carLights.SetActive(false);
        carLightsMaterial.SetVector("_EmissionColor", new Vector4(0f, 0f, 0f, 1f) * 2f);
        
        TryGetComponent(out LGM);
        multCounter = GameManager.instance.CarUI.GetComponentInChildren<MultCounter>();
        
        Controls = new CarInputActions();

        //PlayerInput = GetComponent<PlayerInput>();
        LGM = FindAnyObjectByType<LogitechMovement>();

        CarRb = GetComponent<Rigidbody>();

        // TurbeBar = GameManager.instance.CarUI.transform.Find("TurbeDisplay").GetComponentInChildren<Image>();

        Controls.Enable();

        base.Awake();

        CarRb.centerOfMass = _CenterofMass;

        if (turbo != null)
        {
            Controls.CarControls.turbo.started += _ => turbo.Activate();
            Controls.CarControls.turbo.canceled += _ => turbo.Stop();
        }
    }

    protected override void Start()
    {
        if (LGM != null) LGM.InitializeLogitechWheel(); 
        base.Start();

        basePixel = PixelCount.GetFloat("_pixelcount");

        LGM.useLogitechWheel = true;
        LGM.allowAutoEnable = true;
    }

    private void OnEnable()
    {
        Controls.Enable();

        /* if (PlayerInput != null)
            PlayerInput.onControlsChanged += OnControlsChanged; */

        Controls.CarControls.Move.performed += OnMovePerformed;
        Controls.CarControls.Move.canceled += OnMoveCanceled;

        Controls.CarControls.Drift.performed += OnDriftPerformed;
        Controls.CarControls.Drift.canceled += OnDriftCanceled;

        Controls.CarControls.Brake.performed += OnBrakePerformed;
        Controls.CarControls.Brake.canceled += OnBrakeCanceled;
    }

    private void OnDisable()
    {
        Controls.Disable();

        /* if (PlayerInput != null)
            PlayerInput.onControlsChanged -= OnControlsChanged; */

        Controls.CarControls.Move.performed -= OnMovePerformed;
        Controls.CarControls.Move.canceled -= OnMoveCanceled;

        Controls.CarControls.Drift.performed -= OnDriftPerformed;
        Controls.CarControls.Drift.canceled -= OnDriftCanceled;

        Controls.CarControls.Brake.performed -= OnBrakePerformed;
        Controls.CarControls.Brake.canceled -= OnBrakeCanceled;

        if (LGM != null)
            LGM.StopAllForceFeedback();
    }

    private void OnDestroy()
    {
        Controls.Disable();
        Controls.Dispose();

        if (LGM != null)
            LGM.StopAllForceFeedback();
    }

    /* private void OnControlsChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;

        if (LGM == null)
            return;

        if (CurrentControlScheme == "Keyboard")
        {
            LGM.useLogitechWheel = false;
            LGM.allowAutoEnable = true;
        }
        else if (CurrentControlScheme == "Gamepad")
        {
            LGM.allowAutoEnable = true;
        }
    } */

    void OnApplicationFocus(bool focus)
    {
        if (focus)
        {
            if (LGM != null && LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0))
            {
                LogitechGSDK.LogiUpdate();
            }
        }
    }

    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        MovementInputs = ctx.ReadValue<Vector2>();
        Steer();

        if (!isBraking) Wheels.MotorTorque = MovementInputs.y * Acceleration;
    }

    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        MovementInputs = Vector2.zero;
        Steer();
        Wheels.MotorTorque = 0;
    }

    protected void Update()
    {
        Animatewheels();
        Steer();

        if (LGM != null && LGM.useLogitechWheel)
        {
            LGM.allowAutoEnable = true;
            LogitechGSDK.LogiUpdate();
            LGM.ApplyForceFeedback();  
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (IsDrifting){
            DriftPhysics();
        }

        if (GetGroundedWheelCount() >= minGroundedWheelsForDrive)
        {
            Decelerate();
        }
    }

    void DriftPhysics()
    {
        Vector3 angVel = CarRb.angularVelocity;

        angVel.y *= 0.97f; 

        CarRb.angularVelocity = angVel;
    }


    void SetDriftFriction(bool drifting)
    {
        float t = drifting ? driftAmount : 0f;

        float frontSide = Mathf.Lerp(5f, 1.2f, t);
        float frontForward = Mathf.Lerp(7f, 3f, t);

        float rearSide = Mathf.Lerp(5f, 0.9f, t);
        float rearForward = Mathf.Lerp(7f, 1.5f, t);

        foreach (Wheel wheel in Wheels.FrontWheels)
        {
            var side = wheel.collider.sidewaysFriction;
            var forward = wheel.collider.forwardFriction;

            side.stiffness = frontSide;
            forward.stiffness = frontForward;

            wheel.collider.sidewaysFriction = side;
            wheel.collider.forwardFriction = forward;
        }

        foreach (Wheel wheel in Wheels.RearWheels)
        {
            var side = wheel.collider.sidewaysFriction;
            var forward = wheel.collider.forwardFriction;

            side.stiffness = rearSide;
            forward.stiffness = rearForward;

            wheel.collider.sidewaysFriction = side;
            wheel.collider.forwardFriction = forward;
        }
    }
    
    int GetGroundedWheelCount()
    {
        int grounded = 0;

        foreach (Wheel wheel in Wheels)
        {
            if (wheel.IsGrounded())
                grounded++;
        }
        return grounded;
    }


    void OnDriftPerformed(InputAction.CallbackContext _)
    {
        if (IsDrifting)
            return;

        IsDrifting = true;

        SetDriftFriction(true);
        WheelEffects(true);
    }

    void OnDriftCanceled(InputAction.CallbackContext _) => EndDrift();

    internal void EndDrift()
    {
        IsDrifting = false;

        SetDriftFriction(false);
        WheelEffects(false);
    }


    void OnBrakePerformed(InputAction.CallbackContext ctx)
    {
        Wheels.BrakeTorque = BrakeAcceleration;
        Wheels.MotorTorque = 0;
        isBraking = true;

        carLights.SetActive(true);
        carLightsMaterial.SetVector("_EmissionColor", new Vector4(1f, 0.0491371f, 0f, 1f) * 2f);
    }
    void OnBrakeCanceled(InputAction.CallbackContext ctx)
    {
        Wheels.BrakeTorque = 0;
        Wheels.MotorTorque = MovementInputs.y * Acceleration;
        isBraking = false;

        carLights.SetActive(false);
        carLightsMaterial.SetVector("_EmissionColor", new Vector4(0f, 0f, 0f, 1f) * 2f);
    }

    void OnCollisionEnter(Collision collision)
    {

        if (PixelCount == null  || collision.impulse.sqrMagnitude < 0.1f ) 
            return;

        if (IsDrifting)
            EndDrift();

        float impact = Mathf.Clamp01(
            collision.relativeVelocity.magnitude / Mathf.Max(MpsMaxSpeed, 0.01f)
        );

        if (impact > 0.9f)
            impact = 1f;

        impact = Mathf.SmoothStep(0f, 1f, impact);

        if (PixelRecovery != null)
        {
            StopCoroutine(PixelRecovery);
        }

        PixelRecovery = StartCoroutine(
            PixelRecover(
                Mathf.Lerp(basePixel, minPixel, impact),
                Mathf.Max(0.1f, recoverTime * impact)
            )
        );
    }

    IEnumerator PixelRecover(float hitPixel, float recover)
    {
        float elapsed = 0f;

        while (elapsed < recover)
        {
            elapsed += Time.deltaTime;

            PixelCount.SetFloat(
                "_pixelcount",
                Mathf.Lerp(hitPixel, basePixel, elapsed / recover)
            );

            //hackfixattu
            if (PixelCount.GetFloat("_pixelcount") <= 0 && PixelRecovery != null)
            {
                StopCoroutine(PixelRecovery);
                float savedPixelCount = PlayerPrefs.GetFloat("pixel_value") * 64f;
                PixelCount.SetFloat("_pixelcount", savedPixelCount);
            }

            yield return null;
        }

        PixelCount.SetFloat("_pixelcount", basePixel);
        PixelRecovery = null;
    }
}