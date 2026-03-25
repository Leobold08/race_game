using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

public class ButtonInstructions : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI InstructionText;
    [SerializeField] private TextMeshProUGUI ProgressText;
    [SerializeField] private Transform parent;

    [SerializeField] private UnityEngine.UI.Image ButtonImages;

    

    [SerializeField] private bool useImageObjects = true;

    [Serializable]
    private class NamedSprite
    {
        public string name;
        public Sprite sprite;
    }

    [SerializeField] private List<NamedSprite> keyboardNamedSprites = new();
    [SerializeField] private List<NamedSprite> playStationNamedSprites = new();
    [SerializeField] private List<NamedSprite> xboxNamedSprites = new();

    private CarInputActions inputActions;
    private List<TutorialStep> tutorialSteps;
    private Dictionary<string, InputAction> cachedActions = new();
    private List<Transform> otherChildren = new();
    private int currentStep = 0;
    private bool tutorialComplete;
    private bool childrenHiddenByTutorial;
    private InputDevice lastUsedDevice;
    private Waitbeforestart waitBeforeStart;

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
        public string[] RequiredComboActions;

        [NonSerialized] public float HoldTimer;
    }

    
    private void Awake()
    {
        Time.timeScale = 0f;

        InstructionText.alignment = TextAlignmentOptions.Center;
        InstructionText.textWrappingMode = TextWrappingModes.NoWrap;
        InstructionText.overflowMode = TextOverflowModes.Overflow;

        waitBeforeStart = FindFirstObjectByType<Waitbeforestart>();
        if (waitBeforeStart) waitBeforeStart.enabled = false;
        AutoPopulateSprites();

        inputActions = new CarInputActions();
        InitializeTutorialSteps();
        CacheActions();
        HideOtherChildren();
    }

    private void AutoPopulateSprites()
    {
        if (keyboardNamedSprites == null || keyboardNamedSprites.Count == 0)
            PopulateFromResources("Buttons/Keyboard", keyboardNamedSprites);

        if (xboxNamedSprites == null || xboxNamedSprites.Count == 0)
            PopulateFromResources("Buttons/Xbox", xboxNamedSprites);

        if (playStationNamedSprites == null || playStationNamedSprites.Count == 0)
            PopulateFromResources("Buttons/PlayStation", playStationNamedSprites);
    }

    //loads the resources folders sprites and checks from there if the needed sprites are there
    private void PopulateFromResources(string path, List<NamedSprite> list)
    {
        var sprites = Resources.LoadAll<Sprite>(path);
        if (sprites == null || sprites.Length == 0) return;

        list.Clear();
        foreach (var sprite in sprites)
        {
            list.Add(new NamedSprite { name = sprite.name, sprite = sprite });
        }
    }

    private void OnEnable()
    {
        inputActions.CarControls.Enable();
        inputActions.CarControls.Get().actionTriggered += OnAnyActionTriggered;

        StartStep();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.CarControls.Get().actionTriggered -= OnAnyActionTriggered;
            inputActions.CarControls.Disable();
        }


        if (!tutorialComplete)
            RestoreOtherChildren();
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void Update()
    {
        if (tutorialComplete || currentStep >= tutorialSteps.Count) return;

        TrackLastUsedDevice();
        UpdateStepInput();
        ShowStep();
    }



    private void InitializeTutorialSteps()
    {
        tutorialSteps = new List<TutorialStep>
        {
            new TutorialStep{ Instruction="{button}: drive forward", ActionName="MoveForward", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="{button}: brake", ActionName="Brake", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="{button}: Steer left", ActionName="Move", CompositePart="left", RequiredDirection=Vector2.left, HoldTime=0.3f },
            new TutorialStep{ Instruction="{button}: Steer right", ActionName="Move", CompositePart="right", RequiredDirection=Vector2.right, HoldTime=0.3f },
            new TutorialStep{ Instruction="{button}: drift", ActionName="Drift", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction=" {button}: turbo", ActionName="Turbo", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="{combo}: turbo while drifting", RequiresHold=true, HoldTime=2f, RequiredComboActions=new string[]{"Drift", "Turbo"} },
            new TutorialStep{ Instruction="{button}: respawn", ActionName="Respawn" }
        };
    }

    private void CacheActions()
    {
        foreach (var step in tutorialSteps)
        {
            if (!string.IsNullOrEmpty(step.ActionName)) CacheAction(step.ActionName);

            if (step.RequiredComboActions == null) continue;

            foreach (var comboAction in step.RequiredComboActions)
                CacheAction(comboAction);
        }
    }

    private void CacheAction(string actionName)
    {
        if (string.IsNullOrWhiteSpace(actionName) || cachedActions.ContainsKey(actionName)) return;

        var action = FindActionByName(actionName);
        if (action != null) cachedActions[actionName] = action;
    }

    private InputAction FindActionByName(string actionName)
    {
        if (inputActions == null || string.IsNullOrWhiteSpace(actionName)) return null;

        var direct = inputActions.asset.FindAction(actionName, false);
        if (direct != null) return direct;

        foreach (var map in inputActions.asset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                if (string.Equals(action.name, actionName, StringComparison.OrdinalIgnoreCase))
                    return action;
            }
        }

        return null;
    }

    private void StartStep()
    {
        if (currentStep >= tutorialSteps.Count) return;

        ProgressText.text = $"{currentStep + 1}/{tutorialSteps.Count}";

        var step = tutorialSteps[currentStep];

        if (!string.IsNullOrEmpty(step.ActionName) && cachedActions.TryGetValue(step.ActionName, out var action))
        {
            InstructionText.text = step.Instruction.Replace("{button}", FormatBindingDisplay(GetBindingDisplay(action, step.CompositePart)));
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

    private void AdvanceStep()
    {
        currentStep++;
        if (currentStep >= tutorialSteps.Count)
        {
            CompleteTutorial();
            return;
        }

        StartStep();
    }

    private void CompleteTutorial()
    {
        tutorialComplete = true;

        foreach (var action in cachedActions.Values)
            action.performed -= OnActionPerformed;

        inputActions.CarControls.Get().actionTriggered -= OnAnyActionTriggered;
        inputActions.CarControls.Disable();

        RestoreOtherChildren();

        InstructionText.enabled = false;
        ProgressText.enabled = false;

        if (waitBeforeStart != null) waitBeforeStart.enabled = true;
        Time.timeScale = 1f;
        OnTutorialComplete?.Invoke();
    }



    private void UpdateStepInput()
    {
        var step = tutorialSteps[currentStep];

        // Combo Input
        if (step.RequiredComboActions != null && step.RequiredComboActions.Length > 0)
        {
            if (CheckCombo(step.RequiredComboActions))
            {
                step.HoldTimer += Time.unscaledDeltaTime;
                if (step.HoldTimer >= step.HoldTime) AdvanceStep();
            }
            else step.HoldTimer = 0f;
            return;
        }

        // Directional Input
        if (step.RequiredDirection != Vector2.zero && cachedActions.TryGetValue(step.ActionName, out var dirAction))
        {
            Vector2 input = dirAction.ReadValue<Vector2>();
            if (input.sqrMagnitude >= 0.25f && Vector2.Dot(input.normalized, step.RequiredDirection.normalized) >= 0.7f)
            {
                step.HoldTimer += Time.unscaledDeltaTime;
                if (step.HoldTimer >= step.HoldTime) AdvanceStep();
            }
            else step.HoldTimer = 0f;
            return;
        }

        // Hold Input
        if (step.RequiresHold && cachedActions.TryGetValue(step.ActionName, out var holdAction))
        {
            if (holdAction.IsPressed())
            {
                step.HoldTimer += Time.unscaledDeltaTime;
                if (step.HoldTimer >= step.HoldTime) AdvanceStep();
            }
            else step.HoldTimer = 0f;
        }
    }

    private void OnActionPerformed(InputAction.CallbackContext ctx)
    {
        var step = tutorialSteps[currentStep];
        if (!step.RequiresHold && step.RequiredDirection == Vector2.zero && step.RequiredComboActions == null)
        {
            if (ctx.ReadValue<float>() > 0.5f) AdvanceStep();
        }
    }

    private void OnAnyActionTriggered(InputAction.CallbackContext ctx)
    {
        if (ctx.action?.activeControl != null) lastUsedDevice = ctx.action.activeControl.device;
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

    private bool CheckCombo(string[] actions)
    {
        foreach (var actName in actions)
        {
            if (!cachedActions.TryGetValue(actName, out var act) || !act.IsPressed())
                return false;
        }
        return true;
    }



    private void HideOtherChildren()
    {
        if (parent == null) return;

        otherChildren.Clear();
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child == InstructionText.transform || child == ProgressText.transform) continue;
            otherChildren.Add(child);
            child.gameObject.SetActive(false);
        }

        childrenHiddenByTutorial = true;
    }

    private void RestoreOtherChildren()
    {
        if (!childrenHiddenByTutorial) return;

        foreach (var child in otherChildren)
        {
            if (child != null)
                child.gameObject.SetActive(true);
        }

        childrenHiddenByTutorial = false;
    }

    private void ShowStep()
    {
        var step = tutorialSteps[currentStep];
        if (!string.IsNullOrEmpty(step.ActionName) && cachedActions.TryGetValue(step.ActionName, out var action))
            InstructionText.text = step.Instruction.Replace("{button}", FormatBindingDisplay(GetBindingDisplay(action, step.CompositePart)));
        else if (step.RequiredComboActions != null)
            InstructionText.text = step.Instruction.Replace("{combo}", GetComboBindingDisplay(step.RequiredComboActions));
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
        return string.IsNullOrEmpty(fallback) ? "" : fallback;
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

            return InputControlPath.ToHumanReadableString(binding.effectivePath, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        return "";
    }

    private string GetComboBindingDisplay(string[] ActionNames)
    {
        List<string> displays = new();
        foreach (var name in ActionNames)
        {
            if (cachedActions.TryGetValue(name, out var action))
            {
                var d = FormatBindingDisplay(GetBindingDisplay(action));
                if (!string.IsNullOrEmpty(d)) displays.Add(d);
            }
        }

        return string.Join(" + ", displays);
    }

    private string FormatBindingDisplay(string display)
    {
        if (string.IsNullOrEmpty(display)) return string.Empty;

        if (useImageObjects && ButtonImages != null)
        {
            var sprite = GetNamedSpriteForDevice(display);
            if (sprite != null)
            {
                ButtonImages.sprite = sprite;
                ButtonImages.enabled = true;
                return string.Empty;
            }
            else
            {

                if (ButtonImages.sprite == null)
                    ButtonImages.enabled = false;
            }
        }

        return display;
    }
    private Sprite GetNamedSpriteForDevice(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName)) return null;

        var normalized = NormalizeSpriteName(spriteName);

        List<NamedSprite> primary = null;
        if (lastUsedDevice is Gamepad)
        {
            if (DeviceNameContains(lastUsedDevice, "playstation") || DeviceNameContains(lastUsedDevice, "wireless controller"))
                primary = playStationNamedSprites.Count > 0 ? playStationNamedSprites : (xboxNamedSprites.Count > 0 ? xboxNamedSprites : keyboardNamedSprites);
            else if (DeviceNameContains(lastUsedDevice, "xbox"))
                primary = xboxNamedSprites.Count > 0 ? xboxNamedSprites : (playStationNamedSprites.Count > 0 ? playStationNamedSprites : keyboardNamedSprites);
            else
                primary = xboxNamedSprites.Count > 0 ? xboxNamedSprites : (playStationNamedSprites.Count > 0 ? playStationNamedSprites : keyboardNamedSprites);
        }
        else
        {
            primary = keyboardNamedSprites.Count > 0 ? keyboardNamedSprites : (xboxNamedSprites.Count > 0 ? xboxNamedSprites : playStationNamedSprites);
        }

        Sprite found = FindSpriteInList(primary, normalized);
        if (found != null) return found;

        // fallback search in other lists
        found = FindSpriteInList(keyboardNamedSprites, normalized);
        if (found != null) return found;
        found = FindSpriteInList(xboxNamedSprites, normalized);
        if (found != null) return found;
        found = FindSpriteInList(playStationNamedSprites, normalized);
        return found;
    }

    private Sprite FindSpriteInList(List<NamedSprite> list, string normalized)
    {
        if (list == null) return null;
        foreach (var ns in list)
        {
            if (ns == null || ns.sprite == null || string.IsNullOrEmpty(ns.name)) continue;
            if (string.Equals(ns.name, normalized, StringComparison.OrdinalIgnoreCase)) return ns.sprite;
            if (NormalizeSpriteName(ns.name) == normalized) return ns.sprite;
        }
        return null;
    }

    private string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }



    private bool DeviceNameContains(InputDevice device, string value)
    {
        if (device == null || string.IsNullOrEmpty(value)) return false;

        string displayName = device.displayName ?? string.Empty;
        string productName = device.description.product ?? string.Empty;

        return displayName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0
            || productName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    
}