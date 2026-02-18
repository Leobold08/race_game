using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Logitech;
using PurrNet;


 
public class PlayerCarController : BaseCarController
{


    RacerScript racerScript;
    //LogitechMovement LGM;


    private PlayerInput PlayerInput;
    private string CurrentControlScheme = "Keyboard";

    // PurrNet: only the owner should run this controller in multiplayer
    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);
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

    void Awake()
    {
        Controls = new CarInputActions();
        Controls.Enable();
        TurbeMeter = GameObject.Find("turbeFull").GetComponent<Image>();
        AutoAssignWheelsAndMaterials();
    }

    void Start()
    {
        ApplyCarValues();
        PerusMaxAccerelation = MaxAcceleration;
        SmoothedMaxAcceleration = PerusMaxAccerelation;
        PerusTargetTorque = TargetTorque;
        if (CarRb == null)
            CarRb = GetComponent<Rigidbody>();
        CarRb.centerOfMass = _CenterofMass;
        if (GameManager.instance.sceneSelected != "tutorial")
        {
            CanDrift = true;
            CanUseTurbo = true;
        }
        racerScript = FindAnyObjectByType<RacerScript>();


        //LGM.InitializeLogitechWheel(); 


    }

    private void OnControlsChanged(PlayerInput input)
    {
        CurrentControlScheme = input.currentControlScheme;
    }


    void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        steerInput = ctx.ReadValue<Vector2>().x;
    }
    void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        steerInput = 0f;
    }

    // void OnApplicationFocus(bool focus)
    // {
    //     if (focus){
    //         if (//LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0))
    //         {
    //             LogitechGSDK.LogiUpdate();
    //         }
    //     }
    // }


    private void OnEnable()
    {
        Controls.Enable();
        if (PlayerInput != null)
            PlayerInput.onControlsChanged += OnControlsChanged;

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

        // UNSUBSCRIBE!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        Controls.CarControls.Move.performed -= OnMovePerformed;
        Controls.CarControls.Move.canceled  -= OnMoveCanceled;
        Controls.CarControls.Drift.performed -= OnDriftPerformed;
        Controls.CarControls.Drift.canceled  -= OnDriftCanceled;
        //LGM.StopAllForceFeedback();
    }

    private void OnDestroy()
    {
        Controls.Disable();
        Controls.Dispose();
        
        //LGM.StopAllForceFeedback();
    }


    void Update()
    {
        if (!isOwner) return;

        GetInputs();
        Animatewheels();
        // detect connection state changes and print once when it changes
    //     bool currentlyConnected = //LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0);
    //     if (currentlyConnected != //LGM.lastLogiConnected)
    //     {
    //         //LGM.lastLogiConnected = currentlyConnected;
    //         Debug.Log($"[CarController] Logitech connection status: {(currentlyConnected ? "Connected" : "Disconnected")}");
    //     }

    //     if (//LGM.logitechInitialized && LogitechGSDK.LogiIsConnected(0))
    //     {
    //         LogitechGSDK.LogiUpdate();
    //         //LGM.GetLogitechInputs();
    //         //LGM.ApplyForceFeedback(); 
    //     }
    }


    void FixedUpdate()
    {
        if (!isOwner) return;

        float speed = CarRb.linearVelocity.magnitude * 3.6f;
        isOnGrassCachedValid = false;
        ApplySpeedLimit(speed);

        
        UpdateDriftSpeed();
        StopDriftIfRaceFinished();

        ApplyGravity();
        Move();
        Steer();

        Decelerate();
        Applyturnsensitivity(speed);
        OnGrass();
        HandleTurbo();

        WheelEffects(IsDrifting);
    }


    void UpdateDriftSpeed()
    {
        if (!IsDrifting) return;

        if (IsTurboActive)
            Maxspeed = Mathf.Lerp(Maxspeed, BaseSpeed + Turbesped, Time.deltaTime * 0.5f);
        else
            Maxspeed = Mathf.Lerp(Maxspeed, DriftMaxSpeed, Time.deltaTime * 0.03f);

        
        if (Mathf.Abs(steerInput) > 0.1f)
        {
            CarRb.AddTorque(Vector3.up * Time.deltaTime, ForceMode.Acceleration);
        }
    }

    void StopDriftIfRaceFinished()
    {
        if (racerScript == null) return;
        if (!racerScript.raceFinished) return;
        if (Activedrift <= 0) return;

        StopDrifting();
    }



    void GetInputs()
    {
        //lukee inputin valuen ja etenee siittä
        //LGM.logitechInitialized || !LogitechGSDK.LogiIsConnected(0))
        
        steerInput = Controls.CarControls.Move.ReadValue<Vector2>().x;
        
        
        if (Controls.CarControls.MoveForward.IsPressed())
            moveInput = Controls.CarControls.MoveForward.ReadValue<float>();
        else if (Controls.CarControls.MoveBackward.IsPressed())
            moveInput = -Controls.CarControls.MoveBackward.ReadValue<float>();
        else
            moveInput = 0f;

        if (!Controls.CarControls.Drift.IsPressed())
            StopDrifting();
    }

    void Applyturnsensitivity(float speed)
    {
        TurnSensitivty = Mathf.Lerp(
            TurnSensitivtyAtLowSpeed,
            TurnSensitivtyAtHighSpeed,
            Mathf.Clamp01(speed / Maxspeed));
    }


    void Move()
    {
        //HandeSteepSlope();
        UpdateTargetTorgue();
        AdjustSpeedForGrass();
        AdjustSuspension();
        foreach (var wheel in Wheels)
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





    private void UpdateTargetTorgue()
    {
        float inputValue = CurrentControlScheme == "Gamepad"
            ? Controls.CarControls.ThrottleMod.ReadValue<float>()
            : Mathf.Abs(moveInput);

        float power = CurrentControlScheme == "Gamepad" ? 0.9f : 1.0f;

        float throttle = Mathf.Pow(inputValue, power);
        
        // Reduce power during drift but don't eliminate it
        float driftPowerMultiplier = IsDrifting ? 0.9f : 1.0f;
        float targetMaxAcc = PerusMaxAccerelation * Mathf.Lerp(0.4f, 1f, throttle) * driftPowerMultiplier;

        SmoothedMaxAcceleration = Mathf.MoveTowards(
            SmoothedMaxAcceleration,
            targetMaxAcc,
            Time.deltaTime * 250f
        );

        if (moveInput > 0f)
            TargetTorque = SmoothedMaxAcceleration;
        else if (moveInput < 0f)
            TargetTorque = -SmoothedMaxAcceleration;
        else
            TargetTorque = 0f;

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
        if (IsDrifting || GameManager.instance.isPaused || !CanDrift) return;

        Activedrift++;
        IsDrifting = true;

        MaxAcceleration = PerusMaxAccerelation * 0.85f;

        foreach (var wheel in Wheels)
        {
            if (wheel.WheelCollider == null) continue;
            WheelFrictionCurve sideways = wheel.WheelCollider.sidewaysFriction;
            sideways.extremumSlip   = 0.7f;
            sideways.asymptoteSlip  = 1.05f;
            sideways.extremumValue  = 1f;
            sideways.asymptoteValue = 1.2f;
            sideways.stiffness      = 2f;
            wheel.WheelCollider.sidewaysFriction = sideways;
        }

        CarRb.angularDamping = 0.005f;
        AdjustWheelsForDrift();
        WheelEffects(true);
    }

    void OnDriftCanceled(InputAction.CallbackContext ctx)
    {
        StopDrifting();
        AdjustForwardFrictrion();
        MaxAcceleration = PerusMaxAccerelation;
        TargetTorque = PerusTargetTorque;
        WheelEffects(false);
    }
}
