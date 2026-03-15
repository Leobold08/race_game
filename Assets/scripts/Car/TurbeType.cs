using UnityEngine;

public enum TurbeType { TURBO, CHARGETURBO }

public static class Turbe
{
    public static void Apply(PlayerCarController car, TurbeType type)
    {
        switch (type)
        {
            case TurbeType.TURBO: TURBO(car); break;
            case TurbeType.CHARGETURBO: CHARGETURBO(car); break;
        }
    }

    static void TURBO(PlayerCarController car)
    {
        car.IsTurboActive = car.Controls.CarControls.turbo.IsPressed() && car.TurbeAmount > 0;
        if (car.IsTurboActive)
        {
            car.CarRb.AddForce(Vector3.ProjectOnPlane(car.transform.forward, Vector3.up).normalized * car.Turbepush, ForceMode.Acceleration);
            car.TargetTorque = car.PerusTargetTorque * 1.5f;
            car.TargetTorque = Mathf.Min(car.TargetTorque, car.MaxAcceleration);
        }
    }

    static void CHARGETURBO(PlayerCarController car)
    {
        bool turbepressed = car.Controls.CarControls.turbo.IsPressed();
        car.IsTurboActive = turbepressed && car.turbeChargeAmount > 0;

        float turbecharge = Mathf.InverseLerp(8f, 12f, car.turbechargepush);
        float TurbeStrength = Mathf.Lerp(3f, 7f, turbecharge);
        float Duration = 6.3f;

        if (car.IsTurboActive && !turbepressed)
        {
            car.turbeChargeAmount--;
            Debug.Log(car.turbeChargeAmount);

            if (car.TurbeBoost != null)
                car.StopCoroutine(car.TurbeBoost);

            car.TurbeBoost = car.StartCoroutine(car.BoostCoroutine(TurbeStrength, Duration));
        }
    }
}