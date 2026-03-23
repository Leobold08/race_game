using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class soundFXControlcopy : MonoBehaviour
{
    CarInputActions Controls;
    //säilyttää KAIKKI äänet, paitsi ne, joita käytetään interactableiden kanssa
    public List<GameObject> soundList;
    //kaikki äänet, joita käytetään interactablea käyttäessä
    private List<GameObject> soundClickList;
    public List<GameObject> soundButtonsList;
    public List<GameObject> soundSlidersList;
    public List<GameObject> soundTogglesList;

    void Awake()
    {
        Controls = new CarInputActions();
    }

    private void OnEnable()
    {
        Controls.Enable();
    }
    private void OnDisable()
    {
        Controls.Disable();
    }
    private void OnDestroy()
    {
        Controls.Disable();
    }

    private void FindSoundGameObjects()
    {
        //eti äänet tässä
        soundList = GameObject.FindGameObjectsWithTag("soundFX").OrderBy(a => a.name).ToList();
        soundClickList = GameObject.FindGameObjectsWithTag("soundFXonClick").OrderBy(a => a.name).ToList();
    }

    void Start()
    {
        FindSoundGameObjects();

        //0 = button, 1 = toggle, 2 = slider
    }

    //ei halunnu toimia enää omassa scriptissään
    public void PauseStateHandler()
    {
        bool isPaused = GameManager.instance.isPaused;
        GameObject menu = GameObject.Find("menu");
        optionScript optionScript = menu.GetComponentInChildren<optionScript>(true);

        GameObject[] pausedSoundList = soundList
        .Where((s, i) => i != 4).ToArray();

        foreach (GameObject sound in pausedSoundList)
        {
            AudioSource audioSource = sound.GetComponent<AudioSource>();
            if (isPaused)
            {
                Debug.Log(sound + " pysäytetty");
                audioSource.Pause();
                soundList[4].GetComponent<AudioSource>().volume = 0.66f;
            }
            else
            {
                Debug.Log(sound + " ei pysäytetty");
                audioSource.UnPause();
                soundList[4].GetComponent<AudioSource>().volume = 0.0f;
            }
        }
        if (isPaused && !optionScript.gameObject.activeSelf)
            soundList[6].GetComponent<AudioSource>().Play();
    }
}