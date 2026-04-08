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
        inputEvents.Dispose();
    }

    void OnDestroy()
    {
        inputEvents.Disable();
        inputEvents.Dispose();
    }

    bool FilterInput(InputEventPtr inputEventPtr, InputDevice inputDevice)
    {
        return true;
    }

    [ContextMenu("Get types")]
    void GetTypes()
    {
        AbstractAction.GetTypes();
    }

    [ContextMenu("replay trace")]
    void Replay()
    {
        inputEvents.Replay();
    }
}
