using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Waitbeforestart : MonoBehaviour
{
    [SerializeField] private List<GameObject> countGraphics = new(4);
    [SerializeField] private List<AudioSource> countSounds = new(4);
    private RacerScript racerScript;

    void OnEnable()
    {
        racerScript = FindAnyObjectByType<RacerScript>();
    }
    void Start()
    {
        LogitechLedController.Clear();
        SetupCountdown();
        StartCoroutine(ShowS1AfterDelay());
        Time.timeScale = 0f;
    }

    void SetupCountdown()
    {
        for (int val = 3; val >= 1; val--)
        {
            countGraphics.Add(GameManager.instance.CarUI.transform.Find($"s{val}").gameObject);
            countSounds.Add(GameManager.instance.CarUI.transform.Find($"s{val}").GetComponent<AudioSource>());
        }
        countGraphics.Add(GameManager.instance.CarUI.transform.Find("go").gameObject);
        countSounds.Add(GameManager.instance.CarUI.transform.Find("go").GetComponent<AudioSource>());
        
        foreach (GameObject img in countGraphics) img.SetActive(false);
    }

    IEnumerator ShowS1AfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        for (int val = 0; val <= 2; val++)
        {
            countGraphics[val].SetActive(true);
            countSounds[val].Play();

            LogitechLedController.SetNormalized((val + 1) / 3);
            LeanTween.value(countGraphics[val].GetComponent<RawImage>().color.a, 0.0f, 0.7f)
            .setOnUpdate((float alpha) =>
            {
                var img = countGraphics[val].GetComponent<RawImage>();
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }).setIgnoreTimeScale(true).setEaseLinear();
            yield return new WaitForSecondsRealtime(1f);
        }
        countGraphics[3].SetActive(true);
        countSounds[3].Play();
        LogitechLedController.Clear();
        Time.timeScale = 1f;
        racerScript.StartRace();

        LeanTween.value(countGraphics[3].GetComponent<RawImage>().color.a, 0.0f, 2f)
        .setOnUpdate((float alpha) =>
        {
            var img = countGraphics[3].GetComponent<RawImage>();
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }).setIgnoreTimeScale(true).setEaseLinear().setOnComplete(() => { Destroy(this); });
    }
}