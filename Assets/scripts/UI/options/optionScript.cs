using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class optionScript : MonoBehaviour
{
    public Material pixelCount;
    private Text pixelCountLabel;
    private Dictionary<string, Slider> sliders = new();
    private Dictionary<string, Toggle> toggles = new();

    private const float DefaultPixelValue = 320f;
    private const float PixelMultiplier = 64f;
    private const float DefaultVolume = 0.6f;

    [SerializeField] private Toggle optionTestRef;
    [SerializeField] private Slider pixelRef, audioRef;
    [SerializeField] private Text LabelPARef;

    void OnEnable()
    {
        if (!PlayerPrefs.HasKey("pixel_value"))
        {
            pixelCount.SetFloat("_pixelcount", DefaultPixelValue);
            Debug.Log("pixel_value not found; set to default: " + DefaultPixelValue);
        }

        if (!PlayerPrefs.HasKey("audio_value"))
        {
            PlayerPrefs.SetFloat("audio_value", DefaultVolume);
            Debug.Log("audio_value not found; set to default: " + DefaultVolume);
        }
    }

    void Start()
    {
        CacheUIElements();
        InitializeSliderValues();
        InitializeToggleValues();

        foreach (var colorChanger in FindObjectsByType<PlayerCarColors>(FindObjectsSortMode.None))
        {
            colorChanger.LightsState(3, true);
        }

        //vaikka meillä ei pitäs tehä näin...
        UpdateLabels(PlayerPrefs.GetFloat("pixel_value"));
    }

    public void CacheUIElements()
    {
        toggles["optionTest"] = optionTestRef;
        sliders["pixel"] = pixelRef;
        sliders["audio"] = audioRef;
        pixelCountLabel = LabelPARef;
    }

    public void InitializeSliderValues()
    {
        foreach (var entry in sliders)
        {
            //key on pelkkä nimi, value on viittaus ite objektiin
            //mahollisesti vois tehä rewriten dictionaryjen poistamista varten
            if (PlayerPrefs.HasKey(entry.Key + "_value"))
            {
                entry.Value.value = PlayerPrefs.GetFloat(entry.Key + "_value");
                Debug.Log($"slider {entry.Key} init; value: {entry.Value.value}");
            }
        }
    }
    
    public void InitializeToggleValues()
    {
        foreach (var entry in toggles)
        {
            if (PlayerPrefs.HasKey(entry.Key + "_value"))
            {
                entry.Value.isOn = PlayerPrefs.GetInt(entry.Key + "_value") == 1;
                Debug.Log($"toggle {entry.Key} init; value: {entry.Value.isOn}");
            }
        }
    }

    public void UpdateTogglePreference(string toggleName)
    {
        if (toggles.TryGetValue(toggleName, out Toggle toggle))
        {
            PlayerPrefs.SetInt(toggleName + "_value", toggle.isOn ? 1 : 0);
            if (toggleName == "optionTest")
            {
                foreach (var colorChanger in FindObjectsByType<PlayerCarColors>(FindObjectsSortMode.None))
                {
                    colorChanger.LightsState(3, true);
                }
            }
            Debug.Log($"changed: {toggleName}, with value of {toggle.isOn}");
        }
    }

    public void UpdateSliderPreference(string sliderName)
    {
        if (sliders.TryGetValue(sliderName, out Slider slider))
        {
            PlayerPrefs.SetFloat(sliderName + "_value", slider.value);
            if (sliderName == "pixel")
            {
                pixelCount.SetFloat("_pixelcount", slider.value * PixelMultiplier);
                UpdateLabels(slider.value);
            }
            Debug.Log($"changed: {sliderName}, with value of {slider.value}");
        }
    }

    private void UpdateLabels(float pixelvalue)
    {
        pixelCountLabel.text = (pixelvalue * PixelMultiplier).ToString();
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