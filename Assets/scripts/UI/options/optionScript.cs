using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Audio;
using TMPro;

public class OptionScript : MonoBehaviour
{
    public Material pixelCount;
    private List<OptionComponent> OptionsList;
    [SerializeField] private AudioMixer main;
    public AudioMixerGroup[] AllMixerGroups { get { return main.FindMatchingGroups(string.Empty); } }

    void Awake()
    {
        OptionsList = GetComponentsInChildren<OptionComponent>().ToList();
        if (OptionsList.Count != 0) InitializeOptions();
    }

    void Start()
    {
        //vois koittaa välttää tämmöstä awake > start juttua. hauska juttu myös VOLUME EI PIDÄ AWAKEN KÄYTÖSTÄ
        InitializeVolumeSliders();
        gameObject.SetActive(false);
    }

    public void InitializeOptions()
    {
        foreach (var Option in OptionsList)
        {
            if (Option.gameObject.TryGetComponent(out Toggle toggle)) InitSpecificOptionValue(toggle);
            else if (Option.gameObject.TryGetComponent(out Slider slider)) InitSpecificOptionValue(slider);
            else if (Option.gameObject.TryGetComponent(out TMP_Dropdown dropdown)) InitSpecificOptionValue(dropdown);
        }
    }
    private void InitializeVolumeSliders()
    {
        //MISTER BARBER DID I NOT TELL YOU TO REMOVE EVERYTHING???
        foreach (var i in AllMixerGroups) main.SetFloat($"{i}_value", Mathf.Log10(PlayerPrefs.GetFloat($"{i}_value_value")) * 20);

        List<AudioSlider> audioSliders = GetComponentsInChildren<AudioSlider>().ToList();
        foreach (var i in audioSliders) { i.volumeSlider.onValueChanged.AddListener((value) => { main.SetFloat(i.volumeSlider.name, Mathf.Log10(i.volumeSlider.value) * 20); }); }
        if (audioSliders.Count == 0) return;

        GameObject audioCategory = transform.Find("Container/Audio").gameObject;
        if (audioCategory != null) audioCategory.SetActive(false);
    }

    private void InitSpecificOptionValue(Toggle toggle)
    {
        var valueName = $"{toggle.name}_value";
        toggle.isOn = PlayerPrefs.GetInt(valueName) == 1;
        Debug.Log($"toggle {toggle} init; value: {toggle.isOn}");

        toggle.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetInt(valueName, value ? 1 : 0);
            Debug.Log($"changed: {toggle.name}, with value of {toggle.isOn}");
        });
    }
    private void InitSpecificOptionValue(Slider slider)
    {
        var valueName = $"{slider.name}_value";
        slider.value = PlayerPrefs.GetFloat(valueName);
        Debug.Log($"toggle {slider} init; value: {slider.value}");
        
        slider.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetFloat(valueName, value);
            Debug.Log($"changed: {slider.name}, with value of {slider.value}");

            if (slider.name == "pixel") pixelCount.SetFloat("_pixelcount", slider.value * 64f);
        });
    }
    private void InitSpecificOptionValue(TMP_Dropdown dropdown)
    {
        var valueName = $"{dropdown.name}_value";
        dropdown.value = PlayerPrefs.GetInt(valueName);
        Debug.Log($"toggle {dropdown} init; value: {dropdown.value}");
        
        dropdown.onValueChanged.AddListener((value) =>
        {
            PlayerPrefs.SetInt(valueName, value);
            Debug.Log($"changed: {dropdown.name}, with value of {dropdown.value}");
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