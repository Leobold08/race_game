using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class RandomizeAudioStartPoint : MonoBehaviour
{
    void Awake()
    {
        AudioSource audio = GetComponent<AudioSource>();
        audio.time = Random.Range(0.00f, audio.clip.length);
    }
}