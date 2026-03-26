using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System;

public class ButtonInstructions : MonoBehaviour
{
    [Serializable]
    private class NamedSprite
    {
        public string name;
        public Sprite sprite;
    }

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image buttonImage;
    [SerializeField] private float comboImageSpacing = 56f;
    [SerializeField] private Transform parent;

    [Header("Button Sprites")]
    [SerializeField] private bool useImageObjects = true;
    [SerializeField] private List<NamedSprite> keyboardNamedSprites = new();
    [SerializeField] private List<NamedSprite> playStationNamedSprites = new();
    [SerializeField] private List<NamedSprite> xboxNamedSprites = new();

    [Header("Controls")]
    [SerializeField] private Key skipKey = Key.Escape;

    private TutorialController controller;
    private TutorialInputHandler input;
    private TutorialUIRenderer ui;

    private readonly List<Transform> hiddenChildren = new();
    private bool hidden;
    private bool tutorialFinished;

    private Waitbeforestart waitBeforeStart;

    private void Awake()
    {
        Time.timeScale = 0f;

        waitBeforeStart = FindFirstObjectByType<Waitbeforestart>();
        if (waitBeforeStart != null)
            waitBeforeStart.enabled = false;

        input = new TutorialInputHandler();
        controller = new TutorialController(input);
        ui = new TutorialUIRenderer(instructionText, progressText, buttonImage);
        ui.SetComboImageSpacing(comboImageSpacing);
        AutoPopulateSprites();

        controller.InitializeSteps();
    }

    private void OnEnable()
    {
        input.Enable();
        controller.Start();
        HideOtherChildren();
    }

    private void OnDisable()
    {
        input.Disable();
        RestoreOtherChildren();
    }

    private void Update()
    {
        HandleDebugInput();

        if (!controller.IsRunning)
        {
            FinishTutorial();
            return;
        }

        input.Update();
        controller.Update();

        ui.Render(controller.CurrentStep, controller.StepIndex, controller.StepCount);
        ui.RenderButtons(controller.CurrentStep, input, useImageObjects, ResolveSpriteForAction);
        ui.UpdateFade(true);
    }

    private void HandleDebugInput()
    {
        if (Keyboard.current?[skipKey].wasPressedThisFrame == true)
        {
            controller.Skip();
            return;
        }

        foreach (var gamepad in Gamepad.all)
        {
            if (gamepad != null && (gamepad.startButton.wasPressedThisFrame || gamepad.selectButton.wasPressedThisFrame))
            {
                controller.Skip();
                return;
            }
        }
    }

    private void FinishTutorial()
    {
        if (tutorialFinished)
            return;

        tutorialFinished = true;
        Time.timeScale = 1f;
        ui.HideAll();

        if (waitBeforeStart != null)
            waitBeforeStart.enabled = true;

        RestoreOtherChildren();
        enabled = false;
    }

    private void HideOtherChildren()
    {
        if (parent == null) return;

        hiddenChildren.Clear();

        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);

            if (child == instructionText.transform ||
                child == progressText.transform ||
                child == buttonImage.transform)
                continue;

            hiddenChildren.Add(child);
            child.gameObject.SetActive(false);
        }

        hidden = true;
    }

    private void RestoreOtherChildren()
    {
        if (!hidden) return;

        foreach (var child in hiddenChildren)
        {
            if (child != null)
                child.gameObject.SetActive(true);
        }

        hidden = false;
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

    private void PopulateFromResources(string path, List<NamedSprite> list)
    {
        var sprites = Resources.LoadAll<Sprite>(path);
        if (sprites == null || sprites.Length == 0) return;

        list.Clear();
        foreach (var sprite in sprites)
            list.Add(new NamedSprite { name = sprite.name, sprite = sprite });
    }

    private Sprite ResolveSpriteForAction(string actionName, string compositePart)
    {
        if (input == null) return null;

        var candidateSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var candidate in input.GetSpriteLookupCandidates(actionName, compositePart))
        {
            if (!string.IsNullOrEmpty(candidate))
                candidateSet.Add(candidate);
        }
        if (candidateSet.Count == 0)
            return null;

        foreach (var list in GetSearchOrder())
        {
            var sprite = FindSpriteInList(list, candidateSet);
            if (sprite != null) return sprite;
        }

        return null;
    }

    private IEnumerable<List<NamedSprite>> GetSearchOrder()
        => input.GetCurrentDeviceFamily() switch
        {
            TutorialInputHandler.DeviceFamily.PlayStation => new[] { playStationNamedSprites, xboxNamedSprites, keyboardNamedSprites },
            TutorialInputHandler.DeviceFamily.Xbox => new[] { xboxNamedSprites, playStationNamedSprites, keyboardNamedSprites },
            _ => new[] { keyboardNamedSprites, xboxNamedSprites, playStationNamedSprites }
        };

    private Sprite FindSpriteInList(List<NamedSprite> list, HashSet<string> candidates)
    {
        if (list == null || candidates == null || candidates.Count == 0)
            return null;

        foreach (var ns in list)
        {
            if (ns == null || ns.sprite == null || string.IsNullOrEmpty(ns.name))
                continue;

            var normalized = NormalizeSpriteName(ns.name);
            if (candidates.Contains(normalized))
                return ns.sprite;
        }

        return null;
    }

    private string NormalizeSpriteName(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var builder = new System.Text.StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetterOrDigit(ch)) builder.Append(char.ToLowerInvariant(ch));
        }

        return builder.ToString();
    }
}