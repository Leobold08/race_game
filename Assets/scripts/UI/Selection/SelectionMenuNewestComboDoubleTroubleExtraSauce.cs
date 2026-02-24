using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System;

[Serializable]
public class CarStats
{
    public string carName;
    public int speed;
    public int acceleration;
    public int handling;
    public float scoreMult;
    public int turbeBoost;
    public int turbeAmount;
}
[Serializable]
public class CarBase
{
    public string baseName;
    public List<GameObject> cars;
    public List<CarStats> carStats;
}
public class SelectionMenuNewestComboDoubleTroubleExtraSauce : MonoBehaviour
{
    CarInputActions Controls;

    private AudioSource loadingLoop;
    private AudioSource menuMusic;

    public enum Gamemode {Single, AI, Multi};
    private Gamemode selectedGamemode = Gamemode.Single;
    private float schizophrenia;
    private GameObject current;
    [SerializeField] private GameObject loadingImg;

    [Header("player data")]
    private string savedMapBaseName;

    [Header("general selection data")]
    private TextAsset selectionDetails;
    [SerializeField] private TMP_Dropdown lapCountDropdown;
    [SerializeField] private TMP_Dropdown AICarsAmountDropdown; 
    [SerializeField] private TMP_Dropdown AIDifficultyDropdown; 
    [SerializeField] private GameObject detailsPanel;
    [SerializeField] private GameObject startButton;
    [SerializeField] private GameObject nextButton;
    [SerializeField] private GameObject backButton;
    private Dictionary<string, Dictionary<string, string>> details;
    [SerializeField] private TextMeshProUGUI detailsPanelText;
    private GameObject carStatsContainer;
    public int selectionIndex = 0;
    private List<GameObject> selectionMenus;
    private List<GameObject> availableSelectionMenus;

    [Header("car selection")]
    //debt of 16 cars
    public List<CarBase> carBases;
    [SerializeField] private List<GameObject> availableCars;
    [SerializeField] private List<CarStats> availableCarStats;
    private int baseIndex;
    private int index;
    public Text carNameText,
    speedText, accelerationText, handlingText,
    scoreMultText, turbeBoostText, turbeAmountText;

    RaceResultHandler handler;
    RaceResultCollection collection;

    

    //3.1. data handling playerprefs kautta (esim. tällä hetkellä valittu map)
    //4. setuppaa map selectionin kuva juttu [ehkä]
    //5. score tai aika per auto: miten? mihin?
    //6. lisää tweenaukset kaikkeen tarpeelliseen

    void Awake()
    {
        Controls = new CarInputActions();
        
        selectionDetails = Resources.Load<TextAsset>("selectionDetails");
        //i'm dictionarying my dictionary
        details = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(selectionDetails.text);
        handler = new RaceResultHandler(Application.persistentDataPath, "race_result.json");
        collection = handler.Load();
        availableCars = carBases[baseIndex].cars;
        availableCarStats = carBases[baseIndex].carStats;
        
        carStatsContainer = GameObject.Find("carStatsContainer");
        carStatsContainer.SetActive(false);
        selectionMenus = GameObject.FindGameObjectsWithTag("selectionMenu")
        .OrderBy(go => go.name).ToList();
        availableSelectionMenus = selectionMenus;
        foreach (var menu in availableSelectionMenus.Skip(1))
        {
            menu.SetActive(false);
        }

        menuMusic = GameObject.Find("menuLoop").GetComponent<AudioSource>();
        loadingLoop = GameObject.Find("loadingLoop").GetComponent<AudioSource>();
    }
    void OnEnable()
    {
        Controls.Enable();
        Controls.CarControls.carskinright.performed += ctx => RightButton();
        Controls.CarControls.carskinleft.performed += ctx => LeftButton();
        Controls.CarControls.menucancel.performed += ctx => Back();
    }
    void OnDisable()
    {
        Controls.Disable();
        Controls.CarControls.carskinright.performed -= ctx => RightButton();
        Controls.CarControls.carskinleft.performed -= ctx => LeftButton();
        Controls.CarControls.menucancel.performed -= ctx => Back();
    }

    void Start()
    {
        foreach (CarBase carBase in carBases)
            foreach (GameObject car in carBase.cars)
                car.SetActive(false);

        menuMusic.Play();
    }
    public void SaveDropdownValues()
    {
        PlayerPrefs.SetInt(lapCountDropdown.name, lapCountDropdown.value + 1);
        PlayerPrefs.SetInt(AICarsAmountDropdown.name, AICarsAmountDropdown.value + 1);
        PlayerPrefs.SetInt(AIDifficultyDropdown.name, AIDifficultyDropdown.value);
        PlayerPrefs.Save();
    }

    //uus scene: lobby > map > car > options > gaming multiplayeris

    //tätä käytetää vain alussa
    public void SelectGamemode(int mode)
    {
        selectedGamemode = (Gamemode)mode;

        //what bullshit. ei enää nii paskanen kuitenkaa
        if (selectedGamemode != Gamemode.AI)
            availableSelectionMenus = selectionMenus.Where((a, i) => i != 1).ToList();
        else
            availableSelectionMenus = selectionMenus;
    }

    public void UpdateBase()
    {
        availableCars[index].SetActive(false);

        //Update() ei tykänny päivittää valittua oikein... miten vitussa
        GameObject selectedBase = EventSystem.current.currentSelectedGameObject;
        baseIndex = int.Parse(selectedBase.name[^1].ToString()) - 1; //-1 koska baset alkaa numerost 1
        index = 0;
        availableCars = carBases[baseIndex].cars;

        availableCars[index].SetActive(true);
        SaveSelectedCarPrefs();

        UpdateCarStats();
    }

    private void UpdateCarStats()
    {
        if (availableSelectionMenus[selectionIndex].name != "B_carSelection") return;

        index = -1;
        //laita activeCarIndex kuntoon
        foreach (GameObject car in availableCars)
        {
            if (car.activeInHierarchy)
            {
                index = availableCars.IndexOf(car);
                break;
            }
        }

        availableCarStats = carBases[baseIndex].carStats;
        if (index >= 0 && index < availableCarStats.Count)
        {
            CarStats activeCarStats = availableCarStats[index];

            carNameText.text = $"{activeCarStats.carName}";
            speedText.text = $"Speed: {activeCarStats.speed}";
            accelerationText.text = $"Acceleration: {activeCarStats.acceleration}";
            handlingText.text = $"Handling: {activeCarStats.handling}";
            scoreMultText.text = $"Score mult.: {activeCarStats.scoreMult}x";
            turbeBoostText.text = $"Turbo boost: {activeCarStats.turbeBoost}";
            turbeAmountText.text = $"Turbo amount: {activeCarStats.turbeAmount}";
        }
    }

    //todo: muuta score timeksi ja ota se per base map
    public void UpdateResultsPerMap()
    {
        /* CarStatsNew activeCarStats = carStats[index];

        string selectedMap = PlayerPrefs.GetString("SelectedMap");

        var bestResults = Array.Empty<RaceResultData>();
        if (collection != null && collection.results.Count != 0)
        {
            bestResults = collection.results
                .Where(r => string.Equals(r.map, selectedMap, StringComparison.OrdinalIgnoreCase))
                .Where(r => string.Equals(r.carName, activeCarStats.carName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.score)
                .ToArray();
        }
        else
        {
            Debug.Log("no race results exist; defaulting to empty");
            bestResults = Array.Empty<RaceResultData>();
        }

        int topResultsScore = 0;
        if (bestResults.Length != 0)
        {
            topResultsScore = bestResults[0].score;
            scoreText.text = $"Best score with {activeCarStats.carName}: {topResultsScore}";
        }
        else
            scoreText.text = $"No score yet with {activeCarStats.carName}"; */
    }
    
    public void RightButton()
    {
        if (availableSelectionMenus[selectionIndex].name != "B_carSelection") return;

        availableCars[index].SetActive(false);
        index = (index + 1) % availableCars.Count;
        availableCars[index].SetActive(true);
        if (index >= 0 && index < availableCars.Count)
        {
            UpdateCarStats(); 
        }

        SaveSelectedCarPrefs();
    }

    public void LeftButton()
    {
        if (availableSelectionMenus[selectionIndex].name != "B_carSelection") return;
        
        availableCars[index].SetActive(false);
        index = (index - 1 + availableCars.Count) % availableCars.Count;
        availableCars[index].SetActive(true);
        if (index >= 0 && index < availableCars.Count)
        {
            UpdateCarStats(); 
        }

        SaveSelectedCarPrefs();
    }

    private void SaveSelectedCarPrefs()
    {
        if (index < 0 || index >= availableCars.Count || availableCars[index] == null)
            return;

        string selectedCarName = availableCars[index].name;
        PlayerPrefs.SetString("SelectedCar", selectedCarName);

        PlayerPrefs.Save();
    }

    private void Update()
    {
        current = EventSystem.current.currentSelectedGameObject;

        if (current != null)
        {
            //TODO: setuppaa todennäkösesti variable tolle ja sen onchanged paskiainen tänne,
            //jotta voi yksinkertastaa koodia

            //vuoden indeksoinnit siitä
            if (details[availableSelectionMenus[selectionIndex].name].ContainsKey(current.name))
                detailsPanelText.text = details[availableSelectionMenus[selectionIndex].name][current.name];
            else if (details[availableSelectionMenus[selectionIndex].name].ContainsKey(availableCars[index].name))
                detailsPanelText.text
                = details[availableSelectionMenus[selectionIndex].name][availableCars[index].name];
            //säilytä edellinen teksti details ruudus jos dropdown on avattuna
            else if (current.name.StartsWith("Item"))
                return;
            else
                detailsPanelText.text = "";
        }
    }

    public void Next()
    {
        selectionIndex++;
        availableSelectionMenus[selectionIndex].SetActive(true);
        availableSelectionMenus[selectionIndex - 1].SetActive(false);
        ThePanelThing();

        carStatsContainer.SetActive(false);
        GameObject firstSelected = GameObject.FindGameObjectWithTag("firstSelectable");
        firstSelected.GetComponent<Selectable>().Select();

        if (availableSelectionMenus[selectionIndex].name == "B_carSelection")
        {
            carStatsContainer.SetActive(true);
            if (index >= 0 && index < availableCars.Count)
            {
                availableCars[index].SetActive(true);
            }
            else
            {
                Debug.LogError("Car index out of range: " + index);
                index = 0;
                availableCars[index].SetActive(true);
            }

            UpdateCarStats();
        }
    }
    public void Back()
    {
        //normal back
        if (selectionIndex != 0)
        {
            selectionIndex--;
            availableSelectionMenus[selectionIndex].SetActive(true);
            availableSelectionMenus[selectionIndex + 1].SetActive(false);
            ThePanelThing();
            
            carStatsContainer.SetActive(false);
            GameObject firstSelected = GameObject.FindGameObjectWithTag("firstSelectable");
            firstSelected.GetComponent<Selectable>().Select();

            if (availableSelectionMenus[selectionIndex].name == "A_mapSelection")
            {
                foreach (CarBase carBase in carBases)
                    foreach (GameObject car in carBase.cars)
                        car.SetActive(false);
            }
            else if (availableSelectionMenus[selectionIndex].name == "B_carSelection")
            {
                carStatsContainer.SetActive(true);
            }
        }
        else
        {
            SceneManager.LoadSceneAsync("MainMenu");
        }
    }

    //helper
    //TODO: päivitä ottamaan huomioon autot (pieni juttu)
    private void ThePanelThing()
    {
        nextButton.SetActive(false);
        backButton.SetActive(false);
        startButton.SetActive(false);
        if (selectionIndex == 0) detailsPanel.SetActive(false);

        if (availableSelectionMenus[selectionIndex].name == "C_optionSelection")
        {
            startButton.SetActive(true);
            backButton.SetActive(true);
        }
        else if (availableSelectionMenus[selectionIndex].name == "1_AIoptionSelection")
        {
            nextButton.SetActive(true);
        }
    }

    private void SetMapToLoad()
    {
        string selectedMap = savedMapBaseName;
        TMP_Dropdown dayOrNight = GameObject.Find("Time").GetComponent<TMP_Dropdown>();

        if (selectedGamemode == Gamemode.AI)
            selectedMap = $"ai_{savedMapBaseName}";
        if (dayOrNight.value == 1)
            selectedMap += $"_night";
        PlayerPrefs.SetString("SelectedMap", selectedMap);
        PlayerPrefs.Save();

        Debug.Log($"onnittelut, voitat lomamatkan kohteeseen: {selectedMap}");
    }

    public void SaveBaseMapName(string selecta)
    {
        savedMapBaseName = selecta;
    }

    //tarkistan myöhemmin voiko tätä välttää... vitun coroutinet
    public void StartGame()
    {
        SetMapToLoad();
        StartCoroutine(LoadSelectedMap());
    }
    private IEnumerator LoadSelectedMap()
    {
        loadingLoop.Play();
        schizophrenia = UnityEngine.Random.Range(3.5f, 6.5f);

        LeanTween.value(loadingImg, loadingImg.GetComponent<RectTransform>().anchoredPosition.y, 0.0f, 1f)
        .setOnUpdate((float val) =>
        {
            loadingImg.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(loadingImg.GetComponent<RectTransform>().anchoredPosition.x, val);
        })
        .setEaseInOutCubic();

        Controls.Disable();
        Debug.Log("you will now wait for: " + schizophrenia + " seconds");
        yield return new WaitForSeconds(schizophrenia);
        
        SceneManager.LoadSceneAsync(PlayerPrefs.GetString("SelectedMap"));
    }
}