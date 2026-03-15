//i speak a language beyond work...
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class CursorAutoHide : MonoBehaviour
{
    void Awake()
    {
        InputSystem.onEvent.Call((_) =>
        {
            var device = InputSystem.GetDeviceById(_.deviceId);
            if (device is Mouse) SetCursorVisible(true);
            else SetCursorVisible(false);
        });
    }
    private void SetCursorVisible(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Confined;
    }
}