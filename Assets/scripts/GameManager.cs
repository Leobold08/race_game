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

    [Header("score systeemi")]
    public int score;

    public float scoreAddWT = 0.01f; //WT = wait time

    public bool isAddingPoints = false;

    public float scoreamount = 0;

    [Header("menut")]
    public bool isPaused = false;

    public int chosenMap = 1;

    [Header("car selection")]
    public GameObject currentCar;
    public GameObject[] cars;
    public int carIndex;

    [Header("scene asetukset")]
    public string sceneSelected;
    private string[] maps = new string[]
    {
        "test_mountain",
        "test_mountain_night",
        "haukipudas",
        "AItest",
        "ai_haukipudas",
        "night_ai_haukipudas",
        "tutorial",
        "haukipudas_night"
    };
    
    [Header("auto")]
    public float carSpeed;
    public bool turbeActive = false;

    void OnEnable()
    {
        if (instance == null)
        {
            //Debug.Log("Pasia, olet tehnyt sen!");
            instance = this;
            // DontDestroyOnLoad(gameObject); //poistin koska "DontDestroyOnLoad only works for root GameObjects or components on root GameObjects."
        }
        else
        {
            Destroy(gameObject);
        }

        sceneSelected = SceneManager.GetActiveScene().name;

        // If PurrNet NetworkManager is active, we let its PlayerSpawner
        // handle spawning player cars from prefabs and do not manage
        // scene car instances here.
        if (NetworkManager.main == null)
        {
            //eksi autot järjestyksessä (pitäs olla aika ilmiselvää)
            cars = new GameObject[]
            {
                GameObject.Find("REALCAR_x"),
                GameObject.Find("REALCAR"),
                GameObject.Find("REALCAR_y"),
                GameObject.Find("Lada")
            };

            carIndex = PlayerPrefs.GetInt("CarIndex");
            if (sceneSelected == "tutorial")
            {
                currentCar = GameObject.Find("REALCAR");
            }
            else
            {
                currentCar = carIndex >= 0 && carIndex < cars.Length ? cars[carIndex] : cars[0];
            }
            chosenMap = PlayerPrefs.GetInt("chosenMap");

            if (maps.Contains(sceneSelected))
            {
                if (sceneSelected != "tutorial")
                {
                    foreach (GameObject car in cars)
                    {
                        if (car != null)
                            car.SetActive(false);
                    }

                    if (carIndex >= 0 && carIndex < cars.Length && cars[carIndex] != null)
                    {
                        cars[carIndex].SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("Car index out of range or car missing: " + carIndex);
                    }

                    foreach (GameObject car in cars)
                    {
                        if (car != null && car.activeInHierarchy)
                        {
                            Debug.Log("onnittelut, voitit paketin hiivaa!: " + car.name);
                        }
                    }
                }
            }
        }

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

    // When using PurrNet's PlayerSpawner, player cars are spawned at
    // runtime from prefabs. Here we lazily find and cache the local
    // player's car as currentCar so existing UI / AI code keeps working.
    void Update()
    {
        if (currentCar != null)
            return;

        var controllers = FindObjectsByType<CarController>(FindObjectsSortMode.None);
        if (controllers == null || controllers.Length == 0)
            return;

        CarController chosen = null;

        if (NetworkManager.main != null)
        {
            // Prefer the car owned by this client.
            foreach (var c in controllers)
            {
                if (c != null && c.isOwner)
                {
                    chosen = c;
                    break;
                }
            }
        }

        if (chosen == null)
            chosen = controllers[0];

        if (chosen != null)
            currentCar = chosen.gameObject;
    }

    //temp ja ota se pois sit
/*     public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (racerscript.winMenu.activeSelf) return;
            racerscript.EndRace();
        }
    } */



    // public void AddPoints()
    // {
    //     RacerScript racerScript = FindAnyObjectByType<RacerScript>();
    //     if (racerScript != null && racerScript.raceFinished)
    //     {
    //         return; 
    //     }

    //     if (!isAddingPoints && currentCar.activeSelf && instance != null
    //     && isPaused)
    //     {
    //         StartCoroutine(IncrementScoreWithDelay());
    //     }
    // }

    // private IEnumerator IncrementScoreWithDelay()
    // {
    //     isAddingPoints = true;
    //     float timer = 0f;

    //     while (isAddingPoints)
    //     {
    //         timer += Time.deltaTime;
    //         if (timer >= scoreAddWT)
    //         {
    //             score = Mathf.Max(0, score + (scoreAddWT > 0 ? 1 : -1));
    //             foreach (Text scoreText in ScoreTexts)
    //             {
    //                 scoreText.text = "Score: " + score.ToString();
    //             }
    //             timer -= scoreAddWT;
    //         }

    //         yield return null;
    //     }
    // }

    public void StopAddingPoints()
    {
        isAddingPoints = false;
    }
}