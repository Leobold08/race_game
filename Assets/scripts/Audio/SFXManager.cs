using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SFXManager : MonoBehaviour
{
    //säilyttää KAIKKI äänet, paitsi ne, joita käytetään interactableiden kanssa
    [SerializeField] private List<AudioSource> soundList;
    //kaikki äänet, joita käytetään interactablea käyttäessä
    [SerializeField] private List<AudioSource> interactableSounds;
    [SerializeField] private List<GameObject> interactables;
    [SerializeField] private AudioSource gamePaused;
    [SerializeField] private AudioSource pausedTrack;
    public AudioSource nextLap;
    [ContextMenu("Assign SFX")]
    void FindSounds()
    {
        interactableSounds = GetComponentsInChildren<AudioSource>().Where(a => a.CompareTag("soundFXonClick")).OrderBy(a => a.name).ToList();
    }
    [ContextMenu("Assign interactables")]
    void FindInteractables()
    {
        //HUOM!!! context menun tekemät muutokset EI AIHEUTA muutoksia tiedostoon; laita esim. joku gameobject päälle ja pois ja sitten tallenna nii se tallentaa lmao
        interactables = GameObject.FindGameObjectsWithTag("SFXInteractable").ToList();
    }

    void Start()
    {
        soundList = GetComponentsInChildren<AudioSource>().Where(a => a.CompareTag("soundFX")).ToList();
        //TODO: muuttaa hiukan paremmaks, mutta tarpeeks hyvä atm
        foreach (var i in interactables)
        {
            if (i.TryGetComponent(out Button button)) button.onClick.AddListener(() => { interactableSounds[0].Play(); });
            else if (i.TryGetComponent(out Toggle toggle)) toggle.onValueChanged.AddListener((value) => { interactableSounds[1].Play(); });
            else if (i.TryGetComponent(out Slider slider)) slider.onValueChanged.AddListener((value) => { interactableSounds[2].Play(); });
        }
    }

    public void PauseStateHandler()
    {
        bool isPaused = GameManager.instance.isPaused;

        pausedTrack.volume = isPaused ? 0.66f : 0f;
        foreach (AudioSource sound in soundList)
        {
            if (isPaused) sound.Pause();
            else sound.UnPause();
        }
        if (!isPaused) return;
        gamePaused.Play();
    }
}