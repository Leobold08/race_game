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

    [Header("Sprite Assets")]
    [SerializeField] private TMP_SpriteAsset keyboardSpriteAsset;
    [SerializeField] private TMP_SpriteAsset playStationSpriteAsset;
    [SerializeField] private TMP_SpriteAsset xboxSpriteAsset;
    [SerializeField] private float spriteScalePercent = 720f;
    [SerializeField] private float spriteVerticalOffsetEm = 0.86f;
    [SerializeField] private float spriteHorizontalNudgeEm = 1.1f;
    [SerializeField] private bool logSpriteResolution;

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

        inputActions = new CarInputActions();
        InitializeTutorialSteps();
        CacheActions();
        HideOtherChildren();
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
            new TutorialStep{ Instruction="Hold {button} to accelerate", ActionName="MoveForward", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Hold {button} to brake", ActionName="Brake", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Steer left with {button}", ActionName="Move", CompositePart="left", RequiredDirection=Vector2.left, HoldTime=0.3f },
            new TutorialStep{ Instruction="Steer right with {button}", ActionName="Move", CompositePart="right", RequiredDirection=Vector2.right, HoldTime=0.3f },
            new TutorialStep{ Instruction="Hold {button} to drift", ActionName="Drift", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Hold {button} to use turbo", ActionName="Turbo", RequiresHold=true, HoldTime=2f },
            new TutorialStep{ Instruction="Hold {combo} to turbo while drifting", RequiresHold=true, HoldTime=2f, RequiredComboActions=new string[]{"Drift", "Turbo"} },
            new TutorialStep{ Instruction="Press {button} to respawn", ActionName="Respawn" }
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
                displays.Add(FormatBindingDisplay(GetBindingDisplay(action)));
        }
        return string.Join(" + ", displays);
    }

    private string FormatBindingDisplay(string display)
    {
        if (string.IsNullOrEmpty(display)) return "";

        var spriteAsset = GetSpriteAssetForDevice();
        string resolvedSpriteName = FindSpriteName(spriteAsset, display);

        if (!string.IsNullOrEmpty(resolvedSpriteName))
        {
            InstructionText.spriteAsset = spriteAsset;

            float scalePercent = spriteScalePercent;
            float horizontalNudge = spriteHorizontalNudgeEm;
            if (string.Equals(resolvedSpriteName, "space", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resolvedSpriteName, "spacebar", StringComparison.OrdinalIgnoreCase))
            {
                scalePercent = Mathf.Min(scalePercent, 500f);
                horizontalNudge = Mathf.Min(horizontalNudge, 0.5f);
            }

            string sizeTag = scalePercent != 100f ? $"<size={scalePercent.ToString(CultureInfo.InvariantCulture)}%>" : string.Empty;
            string sizeCloseTag = scalePercent != 100f ? "</size>" : string.Empty;

            float scaleLiftEm = Mathf.Max(0f, (scalePercent - 100f) / 1000f);
            float effectiveOffsetEm = Mathf.Max(-1f, spriteVerticalOffsetEm + scaleLiftEm);

            string offsetTag = effectiveOffsetEm != 0f ? $"<voffset={effectiveOffsetEm.ToString(CultureInfo.InvariantCulture)}em>" : string.Empty;
            string offsetCloseTag = effectiveOffsetEm != 0f ? "</voffset>" : string.Empty;

            string horizontalNudgeTag = horizontalNudge != 0f ? $"<space={horizontalNudge.ToString(CultureInfo.InvariantCulture)}em>" : string.Empty;

            return $"{offsetTag}{horizontalNudgeTag}{sizeTag}<sprite name=\"{resolvedSpriteName}\">{sizeCloseTag}{horizontalNudgeTag}{offsetCloseTag}";
        }

        if (logSpriteResolution)
            DebugMissingSprite(display, display, spriteAsset);

        return display;
    }

    private string FindSpriteName(TMP_SpriteAsset spriteAsset, string spriteName)
    {
        if (spriteAsset == null || string.IsNullOrEmpty(spriteName)) return null;

        if (spriteAsset.GetSpriteIndexFromName(spriteName) != -1)
            return spriteName;

        string normalized = NormalizeSpriteName(spriteName);
        foreach (var spriteCharacter in spriteAsset.spriteCharacterTable)
        {
            if (spriteCharacter == null) continue;
            if (string.Equals(spriteCharacter.name, spriteName, StringComparison.OrdinalIgnoreCase)) return spriteCharacter.name;
            if (NormalizeSpriteName(spriteCharacter.name) == normalized) return spriteCharacter.name;
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

    private void DebugMissingSprite(string display, string spriteName, TMP_SpriteAsset spriteAsset)
    {
        if (spriteAsset == null)
        {
            Debug.LogWarning($"[ButtonInstructions] No sprite asset assigned. Display='{display}', Resolved='{spriteName}'.", this);
            return;
        }

        var names = new List<string>();
        int count = 0;
        foreach (var spriteCharacter in spriteAsset.spriteCharacterTable)
        {
            if (spriteCharacter == null || string.IsNullOrEmpty(spriteCharacter.name)) continue;
            names.Add(spriteCharacter.name);
            count++;
            if (count >= 30) break;
        }

        string sample = names.Count > 0 ? string.Join(", ", names) : "(none)";
        Debug.LogWarning($"[ButtonInstructions] Missing sprite. Display='{display}', Resolved='{spriteName}', Asset='{spriteAsset.name}'. Sample names: {sample}", this);
    }

    private TMP_SpriteAsset GetSpriteAssetForDevice()
    {
        if (lastUsedDevice is Gamepad)
        {
            if (DeviceNameContains(lastUsedDevice, "playstation") || DeviceNameContains(lastUsedDevice, "wireless controller"))
                return playStationSpriteAsset ?? xboxSpriteAsset ?? keyboardSpriteAsset;
            if (DeviceNameContains(lastUsedDevice, "xbox"))
                return xboxSpriteAsset ?? playStationSpriteAsset ?? keyboardSpriteAsset;

            return xboxSpriteAsset ?? playStationSpriteAsset ?? keyboardSpriteAsset;
        }

        if (lastUsedDevice is Keyboard || lastUsedDevice is Mouse)
            return keyboardSpriteAsset ?? xboxSpriteAsset ?? playStationSpriteAsset;

        return keyboardSpriteAsset ?? xboxSpriteAsset ?? playStationSpriteAsset;
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