using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[RequireComponent(typeof(PlayerCarController))]
public class InputRecorder : MonoBehaviour
{
    private InputEventTrace inputEvents;
    [SerializeField] private HashSet<InputAction> recordedInputs = new();

    void OnEnable()
    {
        inputEvents = new()
        {
            onFilterEvent = (ptr, device) => FilterInput(ptr, device)
        };
        inputEvents.Enable();
    }

    void OnDisable()
    {
        inputEvents.Disable();
    }

    void OnDestroy()
    {
        inputEvents.Disable();
    }

    bool FilterInput(InputEventPtr inputEventPtr, InputDevice inputDevice)
    {
        return true;
    }
}
