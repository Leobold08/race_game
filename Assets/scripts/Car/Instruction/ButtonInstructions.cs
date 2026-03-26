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
    [SerializeField] private float comboImageSpacing = -22.7f;
    [SerializeField] private float imageToTextGap = -34f;
    [SerializeField] private float imageScaleMultiplier = 1f;
    [SerializeField] private float imageVerticalOffset = 0f;
    [SerializeField] private float maxIconAspect = 4.16f;
    [SerializeField] private float minIconAspect = 1f;
    [SerializeField] private float comboIconScaleMultiplier = 0.58f;
    [SerializeField] private float maxComboGroupWidth = 261f;

    

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
    private readonly List<UnityEngine.UI.Image> runtimeButtonImages = new();
    private Vector2 baseButtonImageSize;
    private Vector3 baseButtonImageScale;

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
        InitializeButtonImagePool();

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

    private bool TryGetCurrentStep(out TutorialStep step)
    {
        step = null;
        if (tutorialSteps == null) return false;
        if (currentStep < 0 || currentStep >= tutorialSteps.Count) return false;

        step = tutorialSteps[currentStep];
        return step != null;
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
            new TutorialStep{ Instruction="{button}: turbo", ActionName="Turbo", RequiresHold=true, HoldTime=2f },
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
            var display = FormatBindingDisplay(GetBindingDisplay(action, step.CompositePart));
            InstructionText.text = BuildInstructionText(step.Instruction, "{button}", display);
            step.HoldTimer = 0f;
            action.performed -= OnActionPerformed;
            action.performed += OnActionPerformed;
        }
        else if (step.RequiredComboActions != null)
        {
            InstructionText.text = BuildInstructionText(step.Instruction, "{combo}", GetComboBindingDisplay(step.RequiredComboActions));
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
        if (!TryGetCurrentStep(out var step)) return;

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
        if (tutorialComplete) return;
        if (!TryGetCurrentStep(out var step)) return;

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
        if (!TryGetCurrentStep(out var step)) return;

        if (!string.IsNullOrEmpty(step.ActionName) && cachedActions.TryGetValue(step.ActionName, out var action))
        {
            var display = FormatBindingDisplay(GetBindingDisplay(action, step.CompositePart));
            InstructionText.text = BuildInstructionText(step.Instruction, "{button}", display);
        }
        else if (step.RequiredComboActions != null)
            InstructionText.text = BuildInstructionText(step.Instruction, "{combo}", GetComboBindingDisplay(step.RequiredComboActions));
    }

    private void InitializeButtonImagePool()
    {
        runtimeButtonImages.Clear();
        if (ButtonImages != null)
        {
            runtimeButtonImages.Add(ButtonImages);

            baseButtonImageSize = ButtonImages.rectTransform.sizeDelta;
            baseButtonImageScale = ButtonImages.rectTransform.localScale;
            ButtonImages.preserveAspect = false;
        }
    }

    private string BuildInstructionText(string template, string placeholder, string replacement)
    {
        if (string.IsNullOrEmpty(template)) return string.Empty;

        string text = template.Replace(placeholder, replacement ?? string.Empty);

        if (string.IsNullOrEmpty(replacement))
        {
            if (text.StartsWith(": ", StringComparison.Ordinal))
                text = text.Substring(2);
            else if (text.StartsWith(":", StringComparison.Ordinal))
                text = text.Substring(1).TrimStart();
        }

        return text.Trim();
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
                var d = GetBindingDisplay(action);
                if (!string.IsNullOrEmpty(d)) displays.Add(d);
            }
        }

        if (TryShowButtonSprites(displays, true))
            return string.Empty;

        HideUnusedButtonSprites(0);
        return string.Join(" + ", displays);
    }

    private string FormatBindingDisplay(string display)
    {
        if (string.IsNullOrEmpty(display)) return string.Empty;

        if (TryShowButtonSprites(new List<string> { display }, false))
            return string.Empty;

        HideUnusedButtonSprites(0);

        return display;
    }

    private bool TryShowButtonSprites(List<string> displays, bool isCombo)
    {
        if (!useImageObjects || ButtonImages == null || displays == null || displays.Count == 0)
            return false;

        List<Sprite> sprites = new();
        foreach (var display in displays)
        {
            if (string.IsNullOrEmpty(display)) return false;

            var sprite = GetNamedSpriteForDevice(display);
            if (sprite == null) return false;

            sprites.Add(sprite);
        }

        EnsureButtonImagePoolSize(sprites.Count);

        List<float> iconWidths = new();
        float comboScale = isCombo ? Mathf.Max(0.5f, comboIconScaleMultiplier) : 1f;
        float targetHeight = Mathf.Max(1f, baseButtonImageSize.y * imageScaleMultiplier * comboScale);
        float spacing = comboImageSpacing;
        float totalWidth = 0f;
        for (int i = 0; i < sprites.Count; i++)
        {
            float width = GetWidthForHeight(sprites[i], targetHeight);
            iconWidths.Add(width);
            totalWidth += width;
        }
        totalWidth += spacing * Mathf.Max(0, sprites.Count - 1);

        if (isCombo && maxComboGroupWidth > 1f && totalWidth > maxComboGroupWidth)
        {
            float shrink = maxComboGroupWidth / totalWidth;
            targetHeight *= shrink;
            spacing *= shrink;

            totalWidth = 0f;
            for (int i = 0; i < iconWidths.Count; i++)
            {
                iconWidths[i] *= shrink;
                totalWidth += iconWidths[i];
            }
            totalWidth += spacing * Mathf.Max(0, sprites.Count - 1);
        }

        Vector2 startPosition = GetIconStartPosition(totalWidth);
        float currentX = startPosition.x;

        for (int i = 0; i < sprites.Count; i++)
        {
            var image = runtimeButtonImages[i];
            image.sprite = sprites[i];
            image.enabled = true;

            var rect = image.rectTransform;
            rect.sizeDelta = new Vector2(iconWidths[i], targetHeight);
            rect.localScale = baseButtonImageScale;
            rect.anchoredPosition = new Vector2(currentX + (iconWidths[i] * 0.5f), startPosition.y);
            currentX += iconWidths[i] + spacing;
        }

        HideUnusedButtonSprites(sprites.Count);
        return true;
    }

    private void EnsureButtonImagePoolSize(int count)
    {
        if (ButtonImages == null) return;

        if (runtimeButtonImages.Count == 0)
            runtimeButtonImages.Add(ButtonImages);

        while (runtimeButtonImages.Count < count)
        {
            var clone = Instantiate(ButtonImages, ButtonImages.transform.parent);
            clone.name = $"{ButtonImages.name}_Combo_{runtimeButtonImages.Count}";
            clone.preserveAspect = false;
            runtimeButtonImages.Add(clone);
        }
    }

    private Vector2 GetIconStartPosition(float totalIconsWidth)
    {
        if (runtimeButtonImages.Count == 0)
            return Vector2.zero;

        if (InstructionText == null)
            return runtimeButtonImages[0].rectTransform.anchoredPosition;

        // Place the icon group immediately before the current text run.
        InstructionText.ForceMeshUpdate();
        RectTransform textRect = InstructionText.rectTransform;
        float textHalfWidth = InstructionText.preferredWidth * 0.5f;
        float startX = textRect.anchoredPosition.x - textHalfWidth - imageToTextGap - totalIconsWidth;
        float y = textRect.anchoredPosition.y + imageVerticalOffset;
        return new Vector2(startX, y);
    }

    private void HideUnusedButtonSprites(int usedCount)
    {
        for (int i = usedCount; i < runtimeButtonImages.Count; i++)
        {
            var image = runtimeButtonImages[i];
            if (image == null) continue;

            image.sprite = null;
            image.enabled = false;
        }
    }

    private float GetWidthForHeight(Sprite sprite, float targetHeight)
    {
        if (sprite == null) return Mathf.Max(1f, targetHeight);

        Rect r = sprite.rect;
        if (r.height <= 0.01f) return Mathf.Max(1f, targetHeight);

        float aspect = r.width / r.height;
        float clampedAspect = Mathf.Clamp(aspect, Mathf.Max(0.5f, minIconAspect), Mathf.Max(minIconAspect, maxIconAspect));
        return Mathf.Max(1f, targetHeight * clampedAspect);
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

            var candidate = NormalizeSpriteName(ns.name);
            if (candidate == normalized) return ns.sprite;
            if (AreEquivalentBindingNames(candidate, normalized)) return ns.sprite;
        }
        return null;
    }

    private bool AreEquivalentBindingNames(string candidate, string binding)
    {
        if (string.IsNullOrEmpty(candidate) || string.IsNullOrEmpty(binding))
            return false;

        var normalizedCandidate = NormalizeBindingAlias(candidate);
        var normalizedBinding = NormalizeBindingAlias(binding);
        if (normalizedCandidate == normalizedBinding)
            return true;

        // Only collapse left/right for keyboard-like modifiers (e.g. leftshift -> shift).
        if (!ShouldCollapseLeftRight(normalizedCandidate) && !ShouldCollapseLeftRight(normalizedBinding))
            return false;

        var candidateNoSide = RemoveSidePrefix(normalizedCandidate);
        var bindingNoSide = RemoveSidePrefix(normalizedBinding);
        return candidateNoSide == bindingNoSide;
    }

    private bool ShouldCollapseLeftRight(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        return value.EndsWith("shift", StringComparison.Ordinal)
            || value.EndsWith("control", StringComparison.Ordinal)
            || value.EndsWith("alt", StringComparison.Ordinal)
            || value.EndsWith("meta", StringComparison.Ordinal)
            || value.EndsWith("command", StringComparison.Ordinal)
            || value.EndsWith("windows", StringComparison.Ordinal);
    }

    private string NormalizeBindingAlias(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        string normalized = value;
        normalized = normalized.Replace("ctrl", "control", StringComparison.Ordinal);
        normalized = normalized.Replace("return", "enter", StringComparison.Ordinal);
        normalized = normalized.Replace("escape", "esc", StringComparison.Ordinal);
        normalized = normalized.Replace("spacebar", "space", StringComparison.Ordinal);
        return normalized;
    }

    private string RemoveSidePrefix(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        if (value.StartsWith("left", StringComparison.Ordinal) && value.Length > 4)
            return value.Substring(4);

        if (value.StartsWith("right", StringComparison.Ordinal) && value.Length > 5)
            return value.Substring(5);

        return value;
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