using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using System;
using PurrNet;



public class GameManager : MonoBehaviour, IDataPersistence
{
    public static GameManager instance;
    public static RacerScript racerscript;
    public static int SelectedCarIndex { get; private set; }

    [Header("score systeemi")]
    public int score;

    public float scoreAddWT = 0.01f; //WT = wait time

    public bool isAddingPoints = false;

    public float scoreamount = 0;

    [Header("menut")]
    public bool isPaused = false;

    [Header("car selection")]
    public GameObject CurrentCar { get; private set; }
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private GameObject[] cars;

    [Header("scene asetukset")]
    public string sceneSelected;
    private string[] maps = new string[]
    {
        "haukipudas",
        "haukipudas_night",
        "ai_haukipudas",
        "ai_haukipudas_night",
        "tutorial",
        "canyon"
    };
    
    [Header("auto")]
    public float carSpeed;
    public bool turbeActive = false;
    void Awake()
    {
        instance = this;

        sceneSelected = SceneManager.GetActiveScene().name;

        SelectedCarIndex = ResolveSelectedCarIndex();
        if (sceneSelected == "tutorial") CurrentCar = GameObject.Find("REALCAR");
        else CurrentCar = SelectedCarIndex >= 0 && SelectedCarIndex < cars.Length ? cars[SelectedCarIndex] : cars[0];

        if (maps.Contains(sceneSelected) && (NetworkManager.main == null || NetworkManager.main.isOffline))
            CurrentCar = Instantiate(CurrentCar, playerSpawn.position, playerSpawn.rotation);
    }

    private int ResolveSelectedCarIndex()
    {
        if (cars == null || cars.Length == 0)
            return 0;

        int maxIndex = Mathf.Max(0, cars.Length - 1);

        string selectedCarName = PlayerPrefs.GetString("SelectedCar", string.Empty);
        if (!string.IsNullOrWhiteSpace(selectedCarName))
        {
            for (int i = 0; i < cars.Length; i++)
            {
                if (cars[i] != null && string.Equals(cars[i].name, selectedCarName, StringComparison.OrdinalIgnoreCase))
                {
                    PlayerPrefs.SetInt("CarIndex", i);
                    PlayerPrefs.Save();
                    return i;
                }
            }
        }

        if (PlayerPrefs.HasKey("CarIndex"))
            return Mathf.Clamp(PlayerPrefs.GetInt("CarIndex", 0), 0, maxIndex);

        return 0;
    }

    void OnEnable()
    {
        //instance = this;
        // if (instance == null)
        // {
        //     //Debug.Log("Pasia, olet tehnyt sen!");
        //     // DontDestroyOnLoad(gameObject); //poistin koska "DontDestroyOnLoad only works for root GameObjects or components on root GameObjects."
        // }
        // else
        // {
        //     Destroy(gameObject);
        // }

        //etsi autot järjestyksessä (pitäs olla aika ilmiselvää)
        /*cars = new GameObject[] 
        { 
            GameObject.Find("REALCAR_x"), 
            GameObject.Find("REALCAR"), 
            GameObject.Find("REALCAR_y"),
            GameObject.Find("Lada")
        };*/

        

        racerscript = FindAnyObjectByType<RacerScript>();
    }

    public void LoadData(GameData data)
    {
        if (data != null)
        {
            return;
        }
    }

    public void SaveData(ref GameData data)
    {
        if (data != null)
        {
            data.scored += this.score;
        }       
    }

    //temp ja ota se pois sit
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (racerscript.winMenu.activeSelf) return;
            racerscript.EndRace();
        }
    }

    public void StopAddingPoints()
    {
        isAddingPoints = false;
    }
}