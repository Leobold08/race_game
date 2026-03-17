using UnityEngine;
using Logitech;
using NUnit.Framework;


#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LogitechSDKManager
{
    private static bool isInitialized = false;
    private static bool initAttempted = false;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    static void EditorInit()
    {
        // This runs when Unity Editor loads/reloads scripts
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // DO NOT shutdown - keep SDK alive for next play session
            Debug.Log("[LogitechSDK] Play mode ending - keeping SDK alive");
        }
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void RuntimeInit()
    {
        if (!initAttempted)
        {
            InitializeSDK();
            initAttempted = true;
        }
    }

    public static bool IsReady =>
        isInitialized && LogitechGSDK.LogiIsConnected(0);

    static void InitializeSDK()
    {
        Debug.Log("[LogitechSDK] Initializing...");

        try
        {
            bool result = false;
            bool connected = false;
            bool sdkAvailable = true;

            try
            {
                result = LogitechGSDK.LogiSteeringInitialize(false);
                Debug.Log($"[LogitechSDK] LogiSteeringInitialize returned: {result}");

                connected = LogitechGSDK.LogiIsConnected(0);
                Debug.Log($"[LogitechSDK] LogiIsConnected returned: {connected}");
            }
            catch (System.DllNotFoundException)
            {
                sdkAvailable = false;
                Debug.Log("[LogitechSDK] Logitech SDK DLL not found. Skipping wheel initialization.");
            }
            catch (System.Exception e)
            {
                sdkAvailable = false;
                Debug.LogError($"[LogitechSDK] Exception: {e.Message}");
            }

            if (sdkAvailable && (result || connected))
            {
                isInitialized = true;
                Debug.Log("[LogitechSDK] SDK initialized successfully!");
            }
            else if (sdkAvailable)
            {
            #if !UNITY_EDITOR
                Debug.LogWarning("[LogitechSDK] Failed - restart G Hub and replug wheel, then restart Unity");
                #endif
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[LogitechSDK] Exception: {e.Message}");
        }
    }

    public static bool Initialize()
    {
        if (!isInitialized && !initAttempted)
        {
            InitializeSDK();
            initAttempted = true;
        }
        return isInitialized && LogitechGSDK.LogiIsConnected(0);
    }

}