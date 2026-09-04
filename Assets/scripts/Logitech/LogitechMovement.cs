using UnityEngine;
using Logitech;

public class LogitechMovement : MonoBehaviour
{
    public static LogitechMovement Instance { get; private set; }

    [Header("Settings")]
    public bool useLogitechWheel = false;
    public float forceFeedbackMultiplier = 1f;

    internal bool logitechInitialized = false;
    internal bool lastLogiConnected = false;

    public bool allowAutoEnable = true;

    private NewDoublefunszechuansauceWithAsideofNuggets NUGGETS;

    void Awake()
    {
        Instance = this;
        NUGGETS = FindAnyObjectByType<NewDoublefunszechuansauceWithAsideofNuggets>();
    }

    void Update()
    {
        bool connected = false;

        try { connected = LogitechGSDK.LogiIsConnected(0); }
        catch { connected = false; }

        // init on connect
        if (connected && !lastLogiConnected && useLogitechWheel)
        {
            InitializeLogitechWheel();
        }

        // auto enable wheel
        if (!useLogitechWheel && connected && allowAutoEnable)
        {
            var state = LogitechGSDK.LogiGetStateUnity(0);

            bool activity =
                Mathf.Abs(state.lX) > 5000 ||
                Mathf.Abs(state.lY) > 5000;

            if (activity)
            {
                useLogitechWheel = true;
                InitializeLogitechWheel();
            }
        }

        lastLogiConnected = connected;

        if (useLogitechWheel && logitechInitialized && connected)
        {
            ApplyWheelInput();
            ApplyForceFeedback();
        }
    }

    internal void InitializeLogitechWheel()
    {
        logitechInitialized = LogitechSDKManager.Initialize();
    }

    void ApplyWheelInput()
    {
        LogitechGSDK.LogiUpdate();

        var state = LogitechGSDK.LogiGetStateUnity(0);

        float steer = state.lX / 32768f;

        float throttle = -state.lY / 32768f;
        float brake = -state.lRz / 32768f;

        float moveY = Mathf.Clamp(throttle - brake, -1f, 1f);

        if (NUGGETS == null) return;

        NUGGETS.MovementInputs = new Vector2(steer, moveY);

        NUGGETS.Wheels.MotorTorque = moveY * NUGGETS.Acceleration;
    }

    internal void ApplyForceFeedback()
    {
        if (!logitechInitialized) return;
        if (!LogitechGSDK.LogiIsConnected(0)) return;

        int spring = Mathf.RoundToInt(80 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlaySpringForce(0, 0, 100, spring);

        int damper = Mathf.RoundToInt(30 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlayDamperForce(0, damper);
    }

    internal void StopAllForceFeedback()
    {
        if (!logitechInitialized) return;
        LogitechGSDK.LogiStopDirtRoadEffect(0);
    }
}