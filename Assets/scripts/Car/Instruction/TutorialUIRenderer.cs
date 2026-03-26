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
    private float fadeSpeed = 5f;
    private float comboImageSpacing = 56f;

    public TutorialUIRenderer(TextMeshProUGUI instruction, TextMeshProUGUI progress, Image buttonImage)
    {
        this.instruction = instruction;
        this.progress = progress;
        this.buttonImage = buttonImage;

        canvasGroup = instruction.GetComponentInParent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = instruction.gameObject.AddComponent<CanvasGroup>();

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

        EnsureImagePoolSize(sprites.Count);

        Vector2 origin = imagePool[0].rectTransform.anchoredPosition;
        for (int i = 0; i < sprites.Count; i++)
        {
            var image = imagePool[i];
            image.sprite = sprites[i];
            image.enabled = true;
            image.rectTransform.anchoredPosition = origin + new Vector2(i * comboImageSpacing, 0f);
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