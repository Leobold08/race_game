using UnityEngine;
using Logitech;

public static class LogitechLedController
{
    private const int WHEEL_INDEX = 0;
    private const float MAX_RPM = 10000f;

    /// <summary>
    /// Set LED bar fill (0.0 to 1.0)
    /// </summary>
    public static void SetNormalized(float value)
    {
        if (!LogitechSDKManager.Initialize())
            return;

        float rpm = Mathf.Clamp01(value) * MAX_RPM;
        LogitechGSDK.LogiPlayLeds(WHEEL_INDEX, rpm, 0f, MAX_RPM);
    }

    /// <summary>
    /// Set LED bar fill (0 to 100 percent)
    /// </summary>
    public static void SetPercent(float percent)
    {
        SetNormalized(percent / 100f);
    }

    /// <summary>
    /// Set LED bar using actual RPM values
    /// </summary>
    public static void SetRPM(float currentRPM, float maxRPM)
    {
        if (!LogitechSDKManager.Initialize())
            return;

        LogitechGSDK.LogiPlayLeds(WHEEL_INDEX, currentRPM, 0f, maxRPM);
    }

    /// <summary>
    /// Turn all LEDs on (redline/warning state)
    /// </summary>
    public static void SetMax()
    {
        SetNormalized(1f);
    }

    /// <summary>
    /// Turn all LEDs off
    /// </summary>
    public static void Clear()
    {
        if (!LogitechSDKManager.Initialize())
            return;

        LogitechGSDK.LogiPlayLeds(WHEEL_INDEX, 0f, 1f, MAX_RPM);
    }
}

