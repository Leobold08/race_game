using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class optionScript : MonoBehaviour
{
    public Material pixelCount;
    private const float DefaultPixelValue = 320f;
    private const float PixelMultiplier = 64f;
    private List<OptionComponent> OptionsList;

    void Awake()
    {
        if (!PlayerPrefs.HasKey("pixel_value"))
        {
            pixelCount.SetFloat("_pixelcount", DefaultPixelValue);
            Debug.Log("pixel_value not found; set to default: " + DefaultPixelValue);
        }
        OptionsList = GetComponentsInChildren<OptionComponent>().ToList();
        InitializeOptions();
    }

    public void InitializeOptions()
    {
        foreach (var Option in OptionsList)
        {
            if (Option.gameObject.TryGetComponent(out Toggle toggle)) InitSpecificToggleValue(toggle);
            else if (Option.gameObject.TryGetComponent(out Slider slider)) InitSpecificSliderValue(slider);
        }
        foreach (var colorChanger in FindObjectsByType<PlayerCarColors>(FindObjectsSortMode.None))colorChanger.LightsState(3, true);
    }

    private void InitSpecificToggleValue(Toggle toggle)
    {
        var valueName = $"{toggle.name}_value";
        toggle.isOn = PlayerPrefs.GetInt(valueName) == 1;
        Debug.Log($"toggle {toggle} init; value: {toggle.isOn}");

        toggle.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetInt(valueName, value ? 1 : 0);
            Debug.Log($"changed: {toggle.name}, with value of {toggle.isOn}");

            //Long before time had a name, the first Gitjutsu master created "hacking the fuck out of this script"
            if (toggle.name == "optionTest") foreach (var colorChanger in FindObjectsByType<PlayerCarColors>(FindObjectsSortMode.None)) colorChanger.LightsState(3, true);
        });
    }
    private void InitSpecificSliderValue(Slider slider)
    {
        var valueName = $"{slider.name}_value";
        slider.value = PlayerPrefs.GetFloat(valueName);
        Debug.Log($"toggle {slider} init; value: {slider.value}");
        
        slider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(valueName, value);
            Debug.Log($"changed: {slider.name}, with value of {slider.value}");

            if (slider.name == "pixel") pixelCount.SetFloat("_pixelcount", slider.value * PixelMultiplier);
        });
    }

    public void SavePlayerPrefs()
    {
        //yep
        PlayerPrefs.Save();
        Debug.Log("settings saved!");
    }

    public void MenuOpenTween()
    {
        Vector3 preTweenScale = new(1.0f, 0.0f, 1.0f);
        gameObject.transform.localScale = preTweenScale;

        LeanTween.scaleY(gameObject, 1.0f, 0.5f).setIgnoreTimeScale(true).setEaseOutQuart();
    }
}