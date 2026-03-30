using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

public class TutorialUIRenderer
{
    private readonly TextMeshProUGUI instruction;
    private readonly TextMeshProUGUI progress;
    private readonly Image buttonImage;
    private readonly List<Image> imagePool = new();

    private readonly CanvasGroup canvasGroup;
    private readonly Vector3 baseButtonScale;
    private float fadeSpeed = 5f;
    private float comboImageSpacing = 128f;
    private const float EmphasizedKeyScaleMultiplier = 1.3f;
    private const float EmphasizedControllerScaleMultiplier = 1.4f;
    private static readonly string[] KeyboardEmphasisTokens = { "shift", "space", "control", "ctrl" };

    public TutorialUIRenderer(TextMeshProUGUI instruction, TextMeshProUGUI progress, Image buttonImage)
    {
        this.instruction = instruction;
        this.progress = progress;
        this.buttonImage = buttonImage;

        canvasGroup = instruction.GetComponentInParent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = instruction.gameObject.AddComponent<CanvasGroup>();

        baseButtonScale = buttonImage != null ? buttonImage.rectTransform.localScale : Vector3.one;

        if (buttonImage != null)
            imagePool.Add(buttonImage);
    }

    public void SetComboImageSpacing(float spacing) => comboImageSpacing = spacing;

    public void Render(TutorialStep step, int index, int total)
    {
        instruction.enabled = true;
        progress.enabled = true;

        progress.text = $"{index + 1}/{total}";

        if (instruction.text != step.Instruction)
            canvasGroup.alpha = 0f;

        instruction.text = step.Instruction;
    }

    public void UpdateFade(bool visible)
    {
        float target = visible ? 1f : 0f;
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
    }

    public void HideButtons() => HideUnusedImages(0);

    public void HideAll()
    {
        instruction.enabled = false;
        progress.enabled = false;
        canvasGroup.alpha = 0f;
        HideButtons();
    }

    public void RenderButtons(
        TutorialStep step,
        TutorialInputHandler input,
        bool useImageObjects,
        Func<string, string, Sprite> resolveSpriteForAction)
    {
        if (!useImageObjects || buttonImage == null || step == null || input == null || resolveSpriteForAction == null)
            return;

        var actionNames = step.IsCombo ? step.ComboActions : new[] { step.ActionName };
        if (actionNames == null || actionNames.Length == 0)
        {
            HideUnusedImages(0);
            return;
        }

        string compositePart = null;
        if (!step.IsCombo && step.HasDirection)
        {
            if (step.Direction.x < 0f) compositePart = "left";
            else if (step.Direction.x > 0f) compositePart = "right";
            else if (step.Direction.y > 0f) compositePart = "up";
            else if (step.Direction.y < 0f) compositePart = "down";
        }

        var sprites = new List<Sprite>(actionNames.Length);
        foreach (var name in actionNames)
        {
            if (string.IsNullOrEmpty(name))
                continue;

            var sprite = resolveSpriteForAction(name, compositePart);
            if (sprite != null)
                sprites.Add(sprite);
        }

        if (sprites.Count == 0)
        {
            HideUnusedImages(0);
            return;
        }

        int shiftIndex = sprites.FindIndex(s => s != null && s.name.IndexOf("shift", StringComparison.OrdinalIgnoreCase) >= 0);
        if (shiftIndex > 0)
        {
            var shiftSprite = sprites[shiftIndex];
            sprites.RemoveAt(shiftIndex);
            sprites.Insert(0, shiftSprite);
        }

        bool useControllerScale = input.GetCurrentDeviceFamily() != TutorialInputHandler.DeviceFamily.KeyboardMouse;

        EnsureImagePoolSize(sprites.Count);

        Vector2 origin = imagePool[0].rectTransform.anchoredPosition;
        for (int i = 0; i < sprites.Count; i++)
        {
            var image = imagePool[i];
            image.sprite = sprites[i];
            image.enabled = true;
            ApplyButtonScale(image, sprites[i], useControllerScale);

            if (i > 0)
            {
                var layoutElement = image.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = image.gameObject.AddComponent<LayoutElement>();

                layoutElement.ignoreLayout = true;
            }

            float xOffset = i == 0 ? 0f : -i * (comboImageSpacing + 20f);
            image.rectTransform.anchoredPosition = origin + new Vector2(xOffset, 0f);
        }

        HideUnusedImages(sprites.Count);
    }

    private void EnsureImagePoolSize(int count)
    {
        if (buttonImage == null) return;
        if (imagePool.Count == 0)
            imagePool.Add(buttonImage);

        while (imagePool.Count < count)
        {
            var clone = UnityEngine.Object.Instantiate(buttonImage, buttonImage.transform.parent);
            clone.name = $"{buttonImage.name}_Combo_{imagePool.Count}";
            imagePool.Add(clone);
        }
    }

    private void ApplyButtonScale(Image image, Sprite sprite, bool useControllerScale)
    {
        if (image == null) return;

        string name = sprite != null ? sprite.name : string.Empty;
        string normalizedName = NormalizeForSpriteMatch(name);

        if (useControllerScale)
        {
            if (IsRightTriggerSprite(normalizedName))
            {
                image.rectTransform.localScale = baseButtonScale;
                return;
            }

            image.rectTransform.localScale = baseButtonScale * EmphasizedControllerScaleMultiplier;
            return;
        }

        bool emphasize = ContainsAnyToken(normalizedName, KeyboardEmphasisTokens);

        image.rectTransform.localScale = emphasize ? Vector3.one * EmphasizedKeyScaleMultiplier : baseButtonScale;
    }

    private static bool ContainsAnyToken(string value, string[] tokens)
    {
        if (string.IsNullOrEmpty(value)) return false;

        for (int i = 0; i < tokens.Length; i++)
        {
            if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static string NormalizeForSpriteMatch(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var chars = new System.Text.StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if (char.IsLetterOrDigit(ch)) chars.Append(char.ToLowerInvariant(ch));
        }

        return chars.ToString();
    }

    private static bool IsRightTriggerSprite(string normalizedName)
    {
        return normalizedName.Contains("righttrigger", StringComparison.Ordinal)
            || normalizedName.Equals("rt", StringComparison.Ordinal);
    }

    private void HideUnusedImages(int usedCount)
    {
        for (int i = usedCount; i < imagePool.Count; i++)
        {
            var image = imagePool[i];
            if (image == null) continue;
            image.sprite = null;
            image.enabled = false;
        }
    }
}