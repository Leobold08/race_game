using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioSlider : MonoBehaviour
{
    private Slider volumeSlider;
    private const float DefaultVolume = 0.6f;

    void Start()
    {
        volumeSlider = GetComponent<Slider>();
        volumeSlider.value = PlayerPrefs.GetFloat("audio_value");
        volumeSlider.onValueChanged.AddListener((value) => { AudioListener.volume = volumeSlider.value; });
        if (!PlayerPrefs.HasKey("audio_value"))
        {
            PlayerPrefs.SetFloat("audio_value", DefaultVolume);
            Debug.Log("audio_value not found; set to default: " + DefaultVolume);
        }
    }
}