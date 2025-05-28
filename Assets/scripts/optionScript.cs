using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class optionScript : MonoBehaviour
{
    public Material pixelCount;
    private Text pixelCountLabel;
    private Text framerateLabel;
    private Dictionary<string, Slider> sliders = new Dictionary<string, Slider>();
    private Dictionary<string, Toggle> toggles = new Dictionary<string, Toggle>();

    private const float DefaultPixelValue = 256f;
    private const float PixelMultiplier = 64f;
    private const float DefaultVolume = 0.5f;
    private const int DefaultFramerate = 60;

    void OnEnable()
    {
        if (!PlayerPrefs.HasKey("pixel_value"))
        {
            pixelCount.SetFloat("_pixelcount", DefaultPixelValue);
            Debug.Log("pixel_value not found; set to default: " + DefaultPixelValue);
        }

        if (!PlayerPrefs.HasKey("volume"))
        {
            PlayerPrefs.SetFloat("volume", DefaultVolume);
            Debug.Log("volume not found; set to default: " + DefaultVolume);
        }

        if (!PlayerPrefs.HasKey("framerate"))
        {
            PlayerPrefs.SetInt("framerate", DefaultFramerate);
            Debug.Log("framerate not found; set to default: " + DefaultFramerate);
        }
    }

    void Start()
    {
        foreach (var colorChanger in FindObjectsByType<ColorChanger>(FindObjectsSortMode.None))
        {
            colorChanger.CheckLightState();
        }

        CacheUIElements();
        InitializeSliderValues();
        InitializeToggleValues();
        UpdateLabels();
    }

    private void CacheUIElements()
    {
        toggles["optionTest"] = GameObject.Find("optionTest").GetComponent<Toggle>();
        sliders["pixel"] = GameObject.Find("pixel").GetComponent<Slider>();
        sliders["framerate"] = GameObject.Find("framerate").GetComponent<Slider>();
        pixelCountLabel = GameObject.Find("LabelPA").GetComponent<Text>();
        framerateLabel = GameObject.Find("LabelFR").GetComponent<Text>();
    }

    private void InitializeSliderValues()
    {
        foreach (var slider in sliders)
        {
            if (PlayerPrefs.HasKey(slider.Key + "_value"))
            {
                slider.Value.value = PlayerPrefs.GetFloat(slider.Key + "_value");
            }
        }
    }
    
    private void InitializeToggleValues()
    {
        foreach (var toggle in toggles)
        {
            if (PlayerPrefs.HasKey(toggle.Key + "_value"))
            {
                toggle.Value.isOn = PlayerPrefs.GetInt(toggle.Key + "_value") == 1;
            }
        }
    }

    public void UpdateTogglePreference(string toggleName)
    {
        if (toggles.TryGetValue(toggleName, out var toggle))
        {
            PlayerPrefs.SetInt(toggleName + "_value", toggle.isOn ? 1 : 0);
            if (toggleName == "optionTest")
            {
                foreach (var colorChanger in FindObjectsByType<ColorChanger>(FindObjectsSortMode.None))
                {
                    colorChanger.CheckLightState();
                }
            }
            PlayerPrefs.Save();
        }
    }

    public void UpdateSliderPreference(string sliderName)
    {
        if (sliders.TryGetValue(sliderName, out var slider))
        {
            PlayerPrefs.SetFloat(sliderName + "_value", slider.value);
            if (sliderName == "pixel")
            {
                pixelCount.SetFloat("_pixelcount", slider.value * PixelMultiplier);
                UpdateLabels();
            }
            if (sliderName == "framerate")
            {
                Application.targetFrameRate = (int)PlayerPrefs.GetFloat("framerate_value") * 10;
                UpdateLabels();
            }
            PlayerPrefs.Save();
        }
    }

    private void UpdateLabels()
    {
        pixelCountLabel.text = (PlayerPrefs.GetFloat("pixel_value") * PixelMultiplier).ToString();
        framerateLabel.text = ((int)PlayerPrefs.GetFloat("framerate_value") * 10) .ToString();
    }
}