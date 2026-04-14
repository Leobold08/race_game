using System;
using UnityEngine;
using UnityEngine.UI;

public class LapCounter : MonoBehaviour
{
    public Sprite[] numberSprites;
    public Sprite[] finalLapNumberSprites;
    public GameObject digitPrefab;

    private const int lapNumberCount = 1;
    private Image[] lapNumberImages = new Image[lapNumberCount];
    private string lastLapString = "";
    private int laps;
    private RacerScript racer;
    private int CurrentLap => racer != null ? racer.currentLap : 0;
    private string LapString => CurrentLap.ToString().PadLeft(lapNumberCount, '0');

    //TODO: if lauseitten poisto ja muutama pieni parannus

    void Start()
    {
        if (GameManager.instance.CurrentCar != null) racer = GameManager.instance.CurrentCar.GetComponentInChildren<RacerScript>();
        laps = PlayerPrefs.GetInt("Laps");
        if (laps == 1) numberSprites = finalLapNumberSprites;

        for (int i = 0; i < lapNumberCount; i++)
        {
            GameObject digitGO = Instantiate(digitPrefab, transform);
            lapNumberImages[i] = digitGO.GetComponent<Image>();
            if (lapNumberImages[i] == null) throw new NullReferenceException($"index of {i} was null in lapNumberImages");
        }
    }

    void Update()
    {
        if (LapString != lastLapString) UpdateLapUI(LapString, lastLapString);
        lastLapString = LapString;
    }

    void UpdateLapUI(string lapString, string prevLapString)
    {
        if (CurrentLap == laps) numberSprites = finalLapNumberSprites;
        if (numberSprites == null) return;
        for (int i = 0; i < lapNumberCount; i++)
        {
            char digitChar = lapString[i];
            int digit = digitChar - '0';

            if (prevLapString.Length != lapNumberCount || prevLapString[i] != digitChar)
            {
                SFXManager SFXMngr = FindFirstObjectByType<SFXManager>(FindObjectsInactive.Exclude);
                if (digit >= 0 && digit <= 9 && numberSprites != null && numberSprites.Length > digit)
                {
                    lapNumberImages[i].sprite = numberSprites[digit];
                    if (digit != 1) SFXMngr.nextLap.Play();
                }
            }
        }
    }
}
