using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;



public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public static RacerScript racerscript;
    public GameObject CarUI;

    [Header("menut")]
    public bool isPaused => Time.timeScale == 0;

    [Header("car selection")]
    public GameObject CurrentCar { get; private set; }
    [SerializeField] private Transform playerSpawn;
    [SerializeField] private Transform reverse_playerSpawn;
    [SerializeField] private GameObject[] cars;
    public HashSet<BaseCarController> spawnedCars = new();

    [Header("scene asetukset")]
    public string sceneSelected => SceneManager.GetActiveScene().name;
    private string[] maps = new string[]
    {
        "haukipudas",
        "haukipudas_night",
        "ai_haukipudas",
        "ai_haukipudas_night",
        "tutorial",
        "canyon",
        "canyon_night",
        "ai_canyon",
        "ai_canyon_night"
    };
    
    [Header("auto")]
    public float carSpeed;
    void Awake()
    {
        instance = this;

        if (sceneSelected == "tutorial") CurrentCar = GameObject.Find("REALCAR");
        else if (maps.Contains(sceneSelected) && cars.Length > 0)
        {
            GameObject selectedCar = cars.FirstOrDefault(c => c.name == PlayerPrefs.GetString("SelectedCar"));
            if (selectedCar == null) selectedCar = cars[0];
            Transform spawn = PlayerPrefs.GetInt("Reverse") == 1 ? reverse_playerSpawn : playerSpawn;
            CurrentCar = Instantiate(selectedCar, spawn.position, spawn.rotation);
            spawnedCars.Add(CurrentCar.GetComponentInChildren<BaseCarController>());
        }
    }

    void OnEnable()
    {
        racerscript = FindAnyObjectByType<RacerScript>();
    }

#if UNITY_EDITOR
    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (racerscript.raceFinished) return;
            racerscript.EndRace();
        }
    }
#endif
}