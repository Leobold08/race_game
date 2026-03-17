using UnityEngine;
using Logitech;
using Unity.Splines.Examples;

public class LogitechMovement : MonoBehaviour
{

    internal static LogitechMovement Instance {get; private set;}
    
    [Header("Logitech G923 Settings")]
    public bool useLogitechWheel = false;
    public float forceFeedbackMultiplier = 1.0f;
    internal bool logitechInitialized = false;
    internal bool lastLogiConnected = false;
    
    PlayerCarController PlayerCar;
    BaseCarController baseCarController;
    public bool enableDebugLogs = false;
    public float statusLogInterval = 1f;

    public float deviceScanInterval = 2f;
    float lastDeviceScanTime = 0f;
    public int maxDeviceIndicesToScan = 8;

    public bool allowAutoEnable = true;
    // active device index discovered by runtime scan
    int activeDeviceIndex = 0;
    bool hasActiveDevice = false;
    const int rawDetectThreshold = 1000; 

    public bool HasActiveDevice => hasActiveDevice;
    public int ActiveDeviceIndex => hasActiveDevice ? activeDeviceIndex : 0;
    

    void Awake()
    {
        Instance = this;
        baseCarController = FindFirstObjectByType<BaseCarController>();
        PlayerCar = FindFirstObjectByType<PlayerCarController>();
    
    }

    void Update()
    {
        bool connected = false;
        try { connected = LogitechGSDK.LogiIsConnected(0); }
        catch { connected = false; }

        // handle connect event
        if (connected && !lastLogiConnected)
        {
            if (useLogitechWheel && !logitechInitialized) InitializeLogitechWheel();
        }

        // handle disconnect event
        if (!connected && lastLogiConnected)
        {
            StopAllForceFeedback();
            logitechInitialized = false;
        }

        lastLogiConnected = connected;

       

        // if wheel is plugged in but user wasn't using it, auto-enable when activity detected
        if (!useLogitechWheel && connected && allowAutoEnable)
        {
            var state = LogitechGSDK.LogiGetStateUnity(0);
            const int threshold = 5000;
            bool activity = Mathf.Abs(state.lX) > threshold ||
                            Mathf.Abs(state.lY) > threshold ||
                            (state.rglSlider != null && state.rglSlider.Length > 0 && Mathf.Abs(state.rglSlider[0]) > threshold);
            if (activity)
            {
                Debug.Log("[CarController] Logitech activity detected — enabling wheel input.");
                useLogitechWheel = true;
                InitializeLogitechWheel();
            }
        }

        // poll inputs each frame when using the wheel and initialized
        if (useLogitechWheel && logitechInitialized && connected)
        {
            GetLogitechInputs();
        }

        // manual re-enable is handled via control-scheme events from PlayerCarController

        // periodically scan device indices to help locate the active device
        if (enableDebugLogs && Time.time - lastDeviceScanTime > deviceScanInterval)
        {
            DumpConnectedDevices();
            lastDeviceScanTime = Time.time;
        }
    }

    void DumpConnectedDevices()
    {
        // Diagnostics removed to reduce log noise.
        return;
    }

    void DiscoverActiveDeviceIndex()
    {
        hasActiveDevice = false;
        for (int i = 0; i < maxDeviceIndicesToScan; i++)
        {
            bool connected = false;
            try { connected = LogitechGSDK.LogiIsConnected(i); }
            catch { connected = false; }

            if (!connected) continue;

            try
            {
                var state = LogitechGSDK.LogiGetStateUnity(i);
                // look for non-zero (or above threshold) raw axis/slider values
                bool active = Mathf.Abs(state.lX) > rawDetectThreshold || Mathf.Abs(state.lY) > rawDetectThreshold;
                if (!active && state.rglSlider != null && state.rglSlider.Length > 0)
                {
                    for (int s = 0; s < state.rglSlider.Length; s++)
                    {
                        if (Mathf.Abs(state.rglSlider[s]) > rawDetectThreshold) { active = true; break; }
                    }
                }

                if (active)
                {
                    activeDeviceIndex = i;
                    hasActiveDevice = true;
                    Debug.Log($"[CarController] Active Logitech device detected at index {i}");
                    return;
                }
            }
            catch { }
        }
    }


    internal void InitializeLogitechWheel()
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

    internal void GetLogitechInputs()
    {
        // ensure SDK updated this frame before reading state; log result for diagnostics

        LogitechGSDK.LogiUpdate();
        

        // resolve which device index to use
        bool connected = false;
        try {
            if (hasActiveDevice)
                connected = LogitechGSDK.LogiIsConnected(activeDeviceIndex);
            else
                connected = LogitechGSDK.LogiIsConnected(0);
            // Log output removed to reduce noise
        }
        catch (System.Exception ex) { connected = false; if (enableDebugLogs) Debug.LogWarning($"[CarController] LogiIsConnected threw: {ex.Message}"); }

        if (!connected)
        {
            // attempt to discover an active index when none cached or index lost
            if (!hasActiveDevice || !LogitechGSDK.LogiIsConnected(activeDeviceIndex))
            {
                DiscoverActiveDeviceIndex();
            }
            if (!hasActiveDevice)
            {
                return;
            }
            connected = LogitechGSDK.LogiIsConnected(activeDeviceIndex);
            if (!connected)
            {
                return;
            }
        }
        LogitechGSDK.DIJOYSTATE2ENGINES state;
        state = LogitechGSDK.LogiGetStateUnity(hasActiveDevice ? activeDeviceIndex : 0);
        
        float steer = state.lX / 32768.0f;
        float throttle = Mathf.Clamp01(-state.lY / 32768.0f);

        float clutch = 0f;
        if (state.rglSlider != null && state.rglSlider.Length > 0)
            clutch = Mathf.Clamp01(-state.rglSlider[0] / 32768.0f);

        // determine if wheel produced recent activity
        bool wheelActive = Mathf.Abs(steer) > 0.001f || Mathf.Abs(throttle) > 0.001f || Mathf.Abs(clutch) > 0.001f;
        if (wheelActive)
            PlayerCar.LastWheelInputTime = Time.time;

        // only apply wheel inputs if wheel activity is at least as recent as non-wheel activity
        if (PlayerCar.LastWheelInputTime >= PlayerCar.LastNonWheelInputTime)
        {
            PlayerCar.SteerInput = steer;

            if (clutch > 0.1f)
                PlayerCar.MoveInput = -clutch;
            else if (throttle > 0.1f)
                PlayerCar.MoveInput = throttle;
            else
                PlayerCar.MoveInput = 0f;
        }
    }

    // called by input-system driven components (e.g. PlayerCarController) to re-enable wheel
    public void ReenableFromControlScheme(string controlScheme)
    {
        
        if (controlScheme == "Gamepad")
        {
            useLogitechWheel = true;
            allowAutoEnable = true;
            InitializeLogitechWheel();
            Debug.Log("[CarController] Logitech wheel enabled via control-scheme change (Gamepad).");
        }
    }

    internal void StopAllForceFeedback()
    {
        if (!logitechInitialized || !LogitechGSDK.LogiIsConnected(0)) return;
        
        LogitechGSDK.LogiStopDirtRoadEffect(0);
    }

// ...existing code...

    internal void ApplyForceFeedback()
    {
        if (!LogitechSDKManager.IsReady) return;

        if (GameManager.instance.isPaused)
        {
            LogitechGSDK.LogiStopDirtRoadEffect(0);
            return;
        }
        if (!logitechInitialized || !LogitechGSDK.LogiIsConnected(0)) return;
        
        float speed = PlayerCar.CarRb.linearVelocity.magnitude * 3.6f;

        // Continuously apply spring force (centering)
        int springStrength = Mathf.RoundToInt(40 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlaySpringForce(0, 0, 100, springStrength);

        // Continuously apply damper force (resistance) for steering
        int damperStrength = Mathf.RoundToInt(10 * forceFeedbackMultiplier);
        LogitechGSDK.LogiPlayDamperForce(0, damperStrength);
        
        // Dirt road only when on grass and moving
        // if (baseCarController && speed >= 10f)
        //     LogitechGSDK.LogiPlayDirtRoadEffect(0, Mathf.RoundToInt(12.5f * forceFeedbackMultiplier));
        // else
        //     LogitechGSDK.LogiStopDirtRoadEffect(0);
    }

// ...existing code...
}
