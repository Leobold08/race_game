using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class SelectionMenuUIControls : MonoBehaviour
{
    //TODO: remove hardcoding
    private readonly string[] buttonListings = { "WASD/Arrows: Move | Enter: Confirm/Next | Esc: Back | Q, E: Change car type", "Left Stick: Move | X: Confirm/Next | B/O: Back | LB, RB: Change car type" };
    private TMP_Text text;
    void Awake()
    {
        text = GetComponent<TMP_Text>();
        InputSystem.onEvent.Call((_) =>
        {
            var device = InputSystem.GetDeviceById(_.deviceId);
            if (device is Keyboard || device is Mouse) text.text = buttonListings[0];
            else if (device is Gamepad) text.text = buttonListings[1];
        });
    }
}