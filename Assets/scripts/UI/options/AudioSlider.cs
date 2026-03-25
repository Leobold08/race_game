using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class AudioSlider : MonoBehaviour
{
    public Slider volumeSlider;

    void Awake()
    {
        volumeSlider = GetComponent<Slider>();
    }
}