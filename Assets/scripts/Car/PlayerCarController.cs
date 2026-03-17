using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Logitech;
using System.Linq;
using System.Collections;



public class PlayerCarController : BaseCarController
{
    internal CarInputActions Controls;
    RacerScript racerScript;
    LogitechMovement LGM;


    private PlayerInput PlayerInput;
    private string CurrentControlScheme = "Keyboard";
    [Header("Turbo Type")]
    [SerializeField] private TurbeType selectedTurboType = TurbeType.TURBO;
    internal int turbeChargeAmount = 3;
    


    internal Coroutine TurbeBoost;
    // timestamps to arbitrate input sources (wheel vs non-wheel)
    public float LastNonWheelInputTime = 0f;
    public float LastWheelInputTime = 0f;


    

    void Awake()
    {
        Controls = new CarInputActions();
        Controls.Enable();
        PlayerInput = GetComponent<PlayerInput>();
        TurbeBar = GameObject.Find("turbeFull").GetComponent<Image>();
        AutoAssignWheelsAndMaterials();
    }

    override protected void Start()
    {

        PerusMaxAccerelation = MaxAcceleration;
        SmoothedMaxAcceleration = PerusMaxAccerelation;
        PerusTargetTorque = TargetTorque;

        if (LGM == null)
        {
            LGM = FindFirstObjectByType<LogitechMovement>();
        }
        if (CarRb == null)
            CarRb = GetComponent<Rigidbody>();
        CarRb.centerOfMass = _CenterofMass;
        if (GameManager.instance.sceneSelected != "tutorial")
        {
            CanDrift = true;
            CanUseTurbo = true;
        }
        racerScript = FindAnyObjectByType<RacerScript>();


        if (LGM != null)
        {
            LGM.InitializeLogitechWheel(); 
        }


        base.Start();
    }

    private void OnControlsChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
        if (LGM != null)
            LGM.ReenableFromControlScheme(CurrentControlScheme);
    }

    void OnAnyActionTriggered(InputAction.CallbackContext ctx)
    {
        var control = ctx.action?.activeControl;
        if (control == null)
            return;

        var device = control.device;
        if (device is Keyboard || device is Mouse)
            CurrentControlScheme = "Keyboard";
        else if (device is Gamepad)
            CurrentControlScheme = "Gamepad";

        LastNonWheelInputTime = Time.time;
        if (LGM != null)
        {
            LGM.useLogitechWheel = false;
            LGM.allowAutoEnable = true;
            LGM.StopAllForceFeedback();
        }
    }


    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        SteerInput = ctx.ReadValue<Vector2>().x;
    }
    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        SteerInput = 0f;
    }

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


    private void OnEnable()
    {
        Controls.Enable();
        if (PlayerInput == null)
            PlayerInput = GetComponent<PlayerInput>();

        if (PlayerInput != null)
            PlayerInput.onControlsChanged += OnControlsChanged;

        Controls.CarControls.Get().actionTriggered += OnAnyActionTriggered;

        // INPUT SUBSCRIPTIONS: KERRAN
        Controls.CarControls.Move.performed += OnMovePerformed;
        Controls.CarControls.Move.canceled  += OnMoveCanceled;

        Controls.CarControls.Drift.performed   += OnDriftPerformed;
        Controls.CarControls.Drift.canceled    += OnDriftCanceled;
    }

    private void OnDisable()
    {
        Controls.Disable();
        if (PlayerInput != null)
            PlayerInput.onControlsChanged -= OnControlsChanged;

        Controls.CarControls.Get().actionTriggered -= OnAnyActionTriggered;

        // UNSUBSCRIBE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Controls.CarControls.Move.performed -= OnMovePerformed;
        Controls.CarControls.Move.canceled  -= OnMoveCanceled;
        Controls.CarControls.Drift.performed -= OnDriftPerformed;
        Controls.CarControls.Drift.canceled  -= OnDriftCanceled;
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


    void Update()
    {
        GetInputs();
        Animatewheels();
        // detect connection state changes and print once when it changes
        bool currentlyConnected = (LGM != null) && LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0);
        if (LGM != null && currentlyConnected != LGM.lastLogiConnected)
        {
            LGM.lastLogiConnected = currentlyConnected;
            Debug.Log($"[CarController] Logitech connection status: {(currentlyConnected ? "Connected" : "Disconnected")}");
        }

        if (LGM != null && LGM.useLogitechWheel && LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0))
        {
            LogitechGSDK.LogiUpdate();
            LGM.GetLogitechInputs();
            LGM.ApplyForceFeedback(); 
        }
    }


    void FixedUpdate()
    {
        float speed = CarRb.linearVelocity.magnitude;
        isOnGrassCachedValid = false;
        UpdateDriftSpeed();
        ApplyGravity();
        Move();
        Steer();
        Decelerate();
        Applyturnsensitivity(speed);
        OnGrass();
        HandleTurbo();

        ApplySpeedLimit(Maxspeed);
        WheelEffects(IsDrifting);
    }


    void UpdateDriftSpeed()
    {
        if (!IsDrifting) return;

        if (IsTurboActive)
            Maxspeed = Mathf.Lerp(Maxspeed, BaseSpeed + Turbesped, Time.deltaTime * 0.5f);
        else
            Maxspeed = Mathf.Lerp(Maxspeed, DriftMaxSpeed, Time.deltaTime * 0.03f);

        
        if (Mathf.Abs(SteerInput) > 0.1f)
        {
            CarRb.AddTorque(Vector3.up * Time.deltaTime, ForceMode.Acceleration);
        }
    }




    void GetInputs()
    {
        //reads inputs and assigns them to values 
        // read non-wheel input (keyboard / gamepad) and mark last-non-wheel time when active
        SteerInput = Controls.CarControls.Move.ReadValue<Vector2>().x;
        float nonWheelMove = Mathf.Abs(SteerInput) + Mathf.Abs(Controls.CarControls.MoveForward.ReadValue<float>()) + Mathf.Abs(Controls.CarControls.MoveBackward.ReadValue<float>());
        if (nonWheelMove > 0.001f || Controls.CarControls.Drift.IsPressed() || Controls.CarControls.Brake.IsPressed())
        {
            LastNonWheelInputTime = Time.time;
            if (LGM != null)
            {
                LGM.useLogitechWheel = false;
                LGM.allowAutoEnable = true;
                LGM.StopAllForceFeedback();
            }
        }
        
        
        if (Controls.CarControls.MoveForward.IsPressed())
            MoveInput = Controls.CarControls.MoveForward.ReadValue<float>();
        else if (Controls.CarControls.MoveBackward.IsPressed())
            MoveInput = -Controls.CarControls.MoveBackward.ReadValue<float>();
        else
            MoveInput = 0f;

        if (!Controls.CarControls.Drift.IsPressed())
            StopDrifting();
    }

    void Applyturnsensitivity(float speed)
    {
        TurnSensitivity = Mathf.Lerp(
            TurnSensitivityAtLowSpeed,
            TurnSensitivityAtHighSpeed,
            Mathf.Clamp01(speed / Maxspeed));
    }

    protected void HandleTurbo()
    {
        if (!CanUseTurbo) return;
        Turbe.Apply(this, selectedTurboType);
        TurbeMeter();
    }



    void Move()
    {
        //HandeSteepSlope();
        UpdateTargetTorque();
        AdjustSpeedForGrass();
        AdjustSuspension();
        foreach (var wheel in Wheels)
        {
            if (Controls.CarControls.Brake.IsPressed()) Brakes(wheel);
            else MotorTorgue(wheel);
        }
    }

    private void UpdateTargetTorque()
    {
        float inputValue = Mathf.Abs(MoveInput);
        if (CurrentControlScheme == "Gamepad")
        {
            Vector2 moveVector = Controls.CarControls.Move.ReadValue<Vector2>();
            inputValue = Mathf.Max(inputValue, Mathf.Abs(moveVector.y));
        }

        float power = CurrentControlScheme == "Gamepad" ? 0.9f : 1.0f;

        float throttle = Mathf.Pow(inputValue, power);
        
        // Reduce power during drift but don'turbe eliminate it


        float steerFactor = Mathf.Clamp01(Mathf.Abs(SteerInput));
        float driftPowerMultiplier = IsDrifting ? Mathf.Lerp(0.65f, 0.85f, steerFactor) : 1.0f;
        float targetMaxAcc = PerusMaxAccerelation * Mathf.Lerp(0.4f, 1f, throttle) * driftPowerMultiplier;

        SmoothedMaxAcceleration = Mathf.MoveTowards(
            SmoothedMaxAcceleration,
            targetMaxAcc,
            Time.deltaTime * 250f
        );

        float rawTorque = MoveInput * SmoothedMaxAcceleration;
        float forwardVel = Vector3.Dot(CarRb.linearVelocity, transform.forward);
        if (IsDrifting && forwardVel > 0.5f && rawTorque < 0f) rawTorque = 0f;

        TargetTorque = rawTorque;

        // Additional hard reduction while drifting so the car loses speed even when not turning
        if (IsDrifting)
        {
            TargetTorque *= 0.5f; // reduce to 50% while drifting
        }

        if (!IsDrifting)
        {
            float targetMaxSpeed = IsTurboActive ? BaseSpeed + Turbesped : BaseSpeed;
            Maxspeed = Mathf.Lerp(Maxspeed, targetMaxSpeed, Time.deltaTime);
        }
    }



    public float GetDriftSharpness()
    {
        //Checks the drifts sharpness so scoremanager can see how good of a drift you're doing
        if (IsDrifting)
        {
            Vector3 velocity = CarRb.linearVelocity;
            Vector3 forward = transform.forward;
            float angle = Vector3.Angle(forward, velocity);
            return angle;  
        }
        return 0.0f;
    }

    //i hate this so much, its always somewhat broken but for now....... its not broken.
    void OnDriftPerformed(InputAction.CallbackContext ctx)
    {
        if (IsDrifting || !CanDrift || racerScript.raceFinished) return;

        Activedrift++;
        IsDrifting = true;

        MaxAcceleration = PerusMaxAccerelation * 0.95f;

        foreach (var wheel in Wheels)
        {
            if (wheel.WheelCollider == null) continue;
            WheelFrictionCurve sideways = wheel.WheelCollider.sidewaysFriction;
            sideways.extremumSlip   = 0.9f;
            sideways.asymptoteSlip  = 1.6f;
            sideways.extremumValue  = 1.0f;
            sideways.asymptoteValue = 1.2f;
            sideways.stiffness      = 2.0f;
            wheel.WheelCollider.sidewaysFriction = sideways;
        }

        CarRb.angularDamping = 0.03f;
        AdjustWheelsForDrift();
        WheelEffects(true);
    }

    void OnDriftCanceled(InputAction.CallbackContext ctx)
    {
        StopDrifting();
        OnDriftEndBoostTheCar();
        MaxAcceleration = PerusMaxAccerelation;
        TargetTorque = PerusTargetTorque;
        WheelEffects(false);
    }

    override protected bool IsOnGrass()
    {
        if (Wheels.Any(wheel => IsWheelGrounded(wheel) && IsWheelOnGrass(wheel)))
        {
            if (GrassRespawnActive) racerScript.RespawnAtLastCheckpoint();
            return true;
        }
        return false;
    }

    internal void StopDrifting()
    {
        if (IsDrifting)
        {
            Activedrift = 0;
            IsDrifting = false;
            MaxAcceleration = PerusMaxAccerelation;
        }
        float DeltaTime = Time.deltaTime * 2.5f;

        CarRb.angularDamping = Mathf.Lerp(CarRb.angularDamping, 0.1f, DeltaTime);
        
        foreach (var wheel in Wheels)
        {
            if (wheel.WheelCollider == null) continue;
            WheelFrictionCurve sideways = wheel.WheelCollider.sidewaysFriction;
            sideways.stiffness = Mathf.Lerp(sideways.stiffness, 5f, DeltaTime);
            sideways.extremumSlip  = Mathf.Lerp(sideways.extremumSlip, 0.15f, DeltaTime);
            sideways.asymptoteSlip = Mathf.Lerp(sideways.asymptoteSlip, 0.1f, DeltaTime);
            wheel.WheelCollider.sidewaysFriction = sideways;
        }
    }



    public void OnDriftEndBoostTheCar()
    {
        float driftmultiplier = ScoreManager.instance.CurrentDriftMultiplier;

        if (driftmultiplier < 4f) return;

        float turbe = Mathf.InverseLerp(4f, 10f, driftmultiplier);
        float TurbeStrength = Mathf.Lerp(1f, 5f, turbe);
        float Duration = 3.5f;

        if (TurbeBoost != null)
            StopCoroutine(TurbeBoost);

        TurbeBoost = StartCoroutine(BoostCoroutine(TurbeStrength, Duration));
    }

    internal IEnumerator BoostCoroutine(float TurbeStrength, float Duration)
    {
        float originalspeed = Maxspeed;

        float boostedMax = Mathf.Max(BaseSpeed + Turbesped, originalspeed + TurbeStrength);

        float duration = Mathf.Lerp(2.5f, 4.5f, Mathf.InverseLerp(2f, 5f, TurbeStrength));
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float time = timer / duration;
            float smooth = Mathf.SmoothStep(0f, 1f, time);

            float force = TurbeStrength * (1f - smooth) * Time.deltaTime;
            force = Mathf.Min(force, 0.5f); 

            CarRb.AddForce(transform.forward * force, ForceMode.VelocityChange);

            Maxspeed = Mathf.Lerp(boostedMax, originalspeed, smooth);
            yield return null;
        }

        Maxspeed = originalspeed;
        TurbeBoost = null;
    }
}
