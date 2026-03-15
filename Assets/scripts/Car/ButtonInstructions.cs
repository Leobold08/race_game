using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

public class ButtonInstructions : MonoBehaviour
{
    private TextMeshProUGUI InstructionText;
    private Waitbeforestart WaitBeforeStart;

    private CarInputActions InputActions;
    private List<TutorialStep> TutorialSteps;
    private Dictionary<string, InputAction> CachedActions = new();
    private int currentStep = 0;
    private bool tutorialComplete;
    private InputDevice lastUsedDevice;

    public event Action OnTutorialComplete;

    [Serializable]
    public class TutorialStep
    {
        public string Instruction;
        public string ActionName;
        public string CompositePart;
        public bool RequiresHold;
        public float HoldTime = 0.3f;
        public Vector2 RequiredDirection;
        public string[] RequiredComboActions; // for multiple button combos

        [NonSerialized] public float HoldTimer; // per-step timer
    }

    private void Awake()
    {
        Time.timeScale = 0f;

        if (InstructionText == null)
            InstructionText = GetComponent<TextMeshProUGUI>();

        WaitBeforeStart = FindFirstObjectByType<Waitbeforestart>();
        WaitBeforeStart.enabled = false;

        InputActions = new CarInputActions();

        TutorialSteps = new List<TutorialStep>()
        {
            new TutorialStep{ Instruction="Hold {button} to accelerate", ActionName="MoveForward", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Hold {button} to brake", ActionName="Brake", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Steer left with {button}", ActionName="Move", CompositePart="left", RequiredDirection=Vector2.left, HoldTime=0.3f },
            new TutorialStep{ Instruction="Steer right with {button}", ActionName="Move", CompositePart="right", RequiredDirection=Vector2.right, HoldTime=0.3f },
            new TutorialStep{ Instruction="Hold {button} to drift", ActionName="Drift", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Hold {button} to use turbo", ActionName="Turbo", RequiresHold=true, HoldTime=2f },
            //sloppy? yes, will i fix it? no why wont i fix it? idk you tell me, most likely reason for me not fixing it? im lazy and tired
            new TutorialStep{ 
                Instruction="Hold {combo} to turbo while drifting", 
                RequiresHold=true, 
                HoldTime=2f, 
                RequiredComboActions=new string[]{"Drift", "Turbo"} 
            },
            new TutorialStep{ Instruction="Press {button} to respawn", ActionName="Respawn" }
        };
    }

    private void OnEnable()
    {
        InputActions.CarControls.Enable();
        InputActions.CarControls.Get().actionTriggered += OnAnyActionTriggered;
        foreach (var step in TutorialSteps)
        {
            if (!string.IsNullOrEmpty(step.ActionName))
            {
                var action = InputActions.asset.FindAction(step.ActionName);
                if (action != null)
                    CachedActions[step.ActionName] = action;
            }
        }

        StartStep();
    }

    private void OnDisable()
    {
        if (InputActions != null)
        {
            InputActions.CarControls.Get().actionTriggered -= OnAnyActionTriggered;
            InputActions.CarControls.Disable();
            InputActions.Dispose();
        }
    }

    private void Update()
    {
        if (currentStep >= TutorialSteps.Count) return;

        TrackLastUsedDevice();
        ShowStep();

        var step = TutorialSteps[currentStep];

        //just so it can do the combo check
        if (step.RequiredComboActions != null && step.RequiredComboActions.Length > 0)
        {
            bool allPressed = true;
            foreach (var actName in step.RequiredComboActions)
            {
                if (!CachedActions.TryGetValue(actName, out var act) || !act.IsPressed())
                {
                    allPressed = false;
                    break;
                }
            }

            if (allPressed)
            {
                step.HoldTimer += Time.unscaledDeltaTime;
                if (step.HoldTimer >= step.HoldTime) AdvanceStep();
            }
            else step.HoldTimer = 0f;
            return;
        }

        // Directional hold
        if (step.RequiredDirection != Vector2.zero && CachedActions.TryGetValue(step.ActionName, out var dirAction))
        {
            Vector2 input = dirAction.ReadValue<Vector2>();
            if (input.sqrMagnitude >= 0.25f)
            {
                float alignment = Vector2.Dot(input.normalized, step.RequiredDirection.normalized);
                if (alignment >= 0.7f)
                {
                    step.HoldTimer += Time.unscaledDeltaTime;
                    if (step.HoldTimer >= step.HoldTime) AdvanceStep();
                }
                else step.HoldTimer = 0f;
            }
            else step.HoldTimer = 0f;
            return;
        }

        // Single button hold
        if (step.RequiresHold && !string.IsNullOrEmpty(step.ActionName) && CachedActions.TryGetValue(step.ActionName, out var holdAction))
        {
            if (holdAction.IsPressed())
            {
                step.HoldTimer += Time.unscaledDeltaTime;
                if (step.HoldTimer >= step.HoldTime) AdvanceStep();
            }
            else step.HoldTimer = 0f;
        }
    }

    private void TrackLastUsedDevice()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            lastUsedDevice = Keyboard.current;

        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame ||
                                      Mouse.current.rightButton.wasPressedThisFrame ||
                                      Mouse.current.middleButton.wasPressedThisFrame))
            lastUsedDevice = Mouse.current;
    }

    private void StartStep()
    {
        if (currentStep >= TutorialSteps.Count) return;

        var step = TutorialSteps[currentStep];

        if (!string.IsNullOrEmpty(step.ActionName) && CachedActions.TryGetValue(step.ActionName, out var action))
        {
            InstructionText.text = step.Instruction.Replace("{button}", GetBindingDisplay(action, step.CompositePart));
            step.HoldTimer = 0f;
            action.performed -= OnActionPerformed;
            action.performed += OnActionPerformed;
        }
        else if (step.RequiredComboActions != null)
        {
            InstructionText.text = step.Instruction.Replace("{combo}", GetComboBindingDisplay(step.RequiredComboActions));
            step.HoldTimer = 0f;
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        if (tutorialComplete || currentStep < 0 || currentStep >= TutorialSteps.Count)
            return;

        var step = TutorialSteps[currentStep];

        // Only trigger immediate advance for non-hold, non-directional steps
        if (!step.RequiresHold && step.RequiredDirection == Vector2.zero && step.RequiredComboActions == null)
        {
            bool pressed = ctx.ReadValue<float>() > 0.5f;
            if (pressed) AdvanceStep();
        }
    }

    private void OnAnyActionTriggered(InputAction.CallbackContext ctx)
    {
        var control = ctx.action?.activeControl;
        if (control == null)
            return;

        lastUsedDevice = control.device;
    }

    private void AdvanceStep()
    {
        currentStep++;
        if (currentStep >= TutorialSteps.Count)
        {
            CompleteTutorial();
            return;
        }
        StartStep();
    }

    private void CompleteTutorial()
    {
        tutorialComplete = true;

        foreach (var action in CachedActions.Values)
            action.performed -= OnActionPerformed;

        InputActions.CarControls.Get().actionTriggered -= OnAnyActionTriggered;
        InputActions.CarControls.Disable();

        InstructionText.text = "Ready to race!";
        if (WaitBeforeStart != null) WaitBeforeStart.enabled = true;
        Time.timeScale = 1f;
        OnTutorialComplete?.Invoke();
    }

    private void ShowStep()
    {
        if (currentStep >= TutorialSteps.Count) return;

        var step = TutorialSteps[currentStep];

        if (!string.IsNullOrEmpty(step.ActionName) && CachedActions.TryGetValue(step.ActionName, out var action))
        {
            InstructionText.text = step.Instruction.Replace("{button}", GetBindingDisplay(action, step.CompositePart));
        }
        else if (step.RequiredComboActions != null)
        {
            InstructionText.text = step.Instruction.Replace("{combo}", GetComboBindingDisplay(step.RequiredComboActions));
        }
    }

    private string GetBindingDisplay(InputAction action, string CompositePart = null)
    {
        if (action == null) return "";
        List<string> devicePreferences = new();
        if (lastUsedDevice is Gamepad)
            devicePreferences.Add("Gamepad");
        else if (lastUsedDevice is Mouse)
        {
            devicePreferences.Add("Mouse");
            devicePreferences.Add("Keyboard");
        }
        else if (lastUsedDevice is Keyboard)
            devicePreferences.Add("Keyboard");

        foreach (var deviceType in devicePreferences)
        {
            string display = FindBindingDisplay(action, CompositePart, deviceType);
            if (!string.IsNullOrEmpty(display))
                return display;
        }

        string fallback = FindBindingDisplay(action, CompositePart, null);
        if (!string.IsNullOrEmpty(fallback))
            return fallback;

        return "";
    }

    private string FindBindingDisplay(InputAction action, string compositePart, string deviceType)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            if (!string.IsNullOrEmpty(compositePart) && (!binding.isPartOfComposite || !binding.name.Equals(compositePart, StringComparison.OrdinalIgnoreCase)))
                continue;

            if (binding.isPartOfComposite && string.IsNullOrEmpty(compositePart))
                continue;

            if (!string.IsNullOrEmpty(deviceType) && !binding.path.Contains(deviceType, StringComparison.OrdinalIgnoreCase))
                continue;

            return InputControlPath.ToHumanReadableString(
                binding.effectivePath,
                InputControlPath.HumanReadableStringOptions.OmitDevice
            );
        }

        return "";
    }

    private string GetComboBindingDisplay(string[] ActionNames)
    {
        List<string> displays = new();
        foreach (var name in ActionNames)
        {
            if (CachedActions.TryGetValue(name, out var action))
                displays.Add(GetBindingDisplay(action));
        }
        return string.Join(" + ", displays);
    }
}