using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class RacerScript : MonoBehaviour, IDataPersistence
{
    // Public variables
    public GameObject winMenu; 
    public GameObject Car1Hud;
    public GameObject Minimap;

    CarInputActions Controls;

    public float laptime;
    public float Rank;
    public float besttime;
    public bool racestarted = false; // <-- Add this
    private bool startTimer = false;

    // Other variables
    public Transform startFinishLine;
    public Transform[] checkpoints;
    public int CurrentLap => currentLap;

    private bool[] checkpointStates;

    private int currentLap = 1;
    private int totalLaps;
    public bool raceFinished = false;

    private Transform respawnPoint;
    private musicControl musicControl;
    private soundFXControl soundControl;

    public GameObject[] endButtons;
    public GameObject[] otherStuff;
    private GameObject finalLapImg;

    private PlayerCarController carController;

    public void LoadData(GameData data)
    {
        if (data != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            var sceneBestTime = data.bestTimesByMap
                .FirstOrDefault(scene => scene.sceneName.ToLower() == currentSceneName.ToLower());

            if (sceneBestTime != null)
            {
                besttime = sceneBestTime.bestTime;
                Debug.Log($"Loaded best time for scene {currentSceneName}: {besttime}");
            }
            else
            {
                besttime = 0;
            }
        }
    }

    public void SaveData(ref GameData data)
    {
        //outdated
        if (besttime > 0)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            DatapersistenceManager.instance.UpdateBestTime(currentSceneName, besttime);
            
        }
        else
        {
            Debug.LogWarning("Best time is 0. Nothing to save.");
        }
    }

    void Awake() //voi olla ongelmallinen!!!
    {
        Controls = new CarInputActions();
        Controls.Enable();
        musicControl = FindAnyObjectByType<musicControl>();
        soundControl = FindAnyObjectByType<soundFXControl>();
        carController = GetComponent<PlayerCarController>();
    }

    private void OnEnable()
    {
        Controls.Enable();
        
        GameObject[] checkpointObjects = GameObject.FindGameObjectsWithTag("checkpointTag");
        List<Transform> checkpointsToMove = new();

        foreach (GameObject checkpoint in checkpointObjects)
        {
            Transform checkpointTransform;
            checkpointTransform = checkpoint.GetComponent<Transform>();

            checkpointsToMove.Add(checkpointTransform);
        }
        checkpoints = checkpointsToMove.ToArray();

        finalLapImg = GameObject.Find("UIcanvas/finalLap");

        winMenu = GameObject.Find("WinMenu").GetComponentInChildren<Canvas>(true).gameObject;
        Minimap = GameObject.Find("Minimap");
        Car1Hud = GameObject.Find("UIcanvas");
        if (Car1Hud == null)
        {
            Debug.LogWarning("UIcanvas EI OLE VITTU OLEMASSA");
        }
        startFinishLine = GameObject.Find("StartFinish").transform;

        totalLaps = PlayerPrefs.GetInt("Laps");
        Debug.Log(totalLaps);
    }

    private void OnDisable()
    {
        Controls.Disable();
    }

    private void OnDestroy()
    {
        Controls.Disable();
        Controls.Dispose();
    }

    void Start()
    {
        InitializeRace();
        racestarted = false; // Ensure race doesn't start until countdown is done
    }

    void Update()
    {
        if (!racestarted || raceFinished) return; // Only run race logic if started

        HandleReset();
        Inactivity();
        Ranking(); // Continuously update the rank
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.name == "StartFinish")
        {
            HandleStart();
        }
        else if (other.gameObject.CompareTag("RespawnTrigger")) // Check for the respawn trigger
        {
            RespawnAtLastCheckpoint();
        }
        else
        {
            HandleCheck(other);
        }
    }

    public void RespawnAtLastCheckpoint()
    {
        Debug.Log("Respawning at the last checkpoint...");
        transform.SetPositionAndRotation(respawnPoint != null ? respawnPoint.position : startFinishLine.position,
        respawnPoint != null ? respawnPoint.rotation : startFinishLine.rotation);
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

    }

    void InitializeRace()
    {
        respawnPoint = startFinishLine;

        checkpointStates = new bool[checkpoints.Length];
    }

    void HandleReset()
    {
        if (Controls.CarControls.respawn.triggered)
        {
            ResetPosition();
            carController.ClearWheelTrails();
        }

        if (transform.position.y < -1)
        {
            ResetPosition();
            carController.ClearWheelTrails();
            ResetCarState();
        }
    }

    void ResetPosition()
    {
        transform.position = respawnPoint != null ? respawnPoint.position : startFinishLine.position;
        transform.rotation = respawnPoint != null ? respawnPoint.rotation : startFinishLine.rotation;

        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;//stop the car
        rb.angularVelocity = Vector3.zero;
        
        //StartCoroutine(TurnDownCarsValues());

    }


    void Inactivity()
    {
        if (startTimer)
        {
            laptime += Time.deltaTime;
        }
    }

    void HandleStart()
    {
        if (!startTimer)
        {
            StartNewLap();
        }

        bool allCheckpointsPassed = true;
        for (int i = 0; i < checkpointStates.Length; i++)
        {
            if (!checkpointStates[i])
            {
                allCheckpointsPassed = false;
                break;
            }
        }

        if (allCheckpointsPassed)
        {
            currentLap++;

            //FINAL LAP CHECK
            if (currentLap == totalLaps)
            {
                //musicControl.StartFinalLapTrack();
                LeanTween.value(finalLapImg,
                finalLapImg.GetComponent<RectTransform>().anchoredPosition.x, 0.0f, 0.6f)
                .setOnUpdate((float val) =>
                {
                    finalLapImg.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                    val, finalLapImg.GetComponent<RectTransform>().anchoredPosition.y);
                })
                .setEaseInOutCirc()
                .setOnComplete(() =>
                    LeanTween.value(finalLapImg,
                    finalLapImg.GetComponent<RectTransform>().anchoredPosition.x, -530.0f, 2.4f)
                    .setOnUpdate((float val) =>
                    {
                        finalLapImg.GetComponent<RectTransform>().anchoredPosition = new Vector2(
                        val, finalLapImg.GetComponent<RectTransform>().anchoredPosition.y);
                    })
                    .setEaseInExpo()
                );
            }

            if (currentLap > totalLaps)
            {
                raceFinished = true;
                startTimer = false;

                if (besttime == 0 || laptime < besttime)
                {
                    besttime = laptime;
                }

                EndRace();
            }
            else
            {
                for (int i = 0; i < checkpointStates.Length; i++)
                {
                    checkpointStates[i] = false;
                }
                respawnPoint = startFinishLine;
            }
        }
    }

    void HandleCheck(Collider other)
    {
        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (other.transform == checkpoints[i])
            {
                checkpointStates[i] = true;
                respawnPoint = checkpoints[i];
                break;
            }
        }
    }

    void ResetCarState()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void StartNewLap()
    {
        startTimer = true;
        laptime = 0;
        for (int i = 0; i < checkpointStates.Length; i++)
        {
            checkpointStates[i] = false;
        }
        respawnPoint = startFinishLine;
    }

    //KUTSU TÄÄ FUNKTIO AINOASTAA SILLO KU KISA LOPPUU!!!
    public void EndRace()
    {
        respawnPoint = startFinishLine;

        for (int i = 0; i < checkpointStates.Length; i++)
        {
            checkpointStates[i] = false;
        }

        musicControl.StopMusicTracks(true);

        if (startFinishLine != null)
            startFinishLine.gameObject.SetActive(false);
        if (Car1Hud != null)
            Car1Hud.SetActive(false);
        if (Minimap != null)
            Minimap.SetActive(false);
        winMenu.SetActive(true);
        raceFinished = true;
        startTimer = false;

        endButtons = GameObject.FindGameObjectsWithTag("winmenubuttons")
            .OrderBy(r => r.name)
            .ToArray();
        otherStuff = GameObject.FindGameObjectsWithTag("winmenuother")
            .OrderBy(r => r.name)
            .ToArray();
        TMP_InputField playerInput = otherStuff[1].GetComponent<TMP_InputField>();
        playerInput.Select();
    }

    //pitää kutsua sillo ku haluaa tallentaa
    public void FinalizeRaceAndSaveData()
    {
        if (winMenu != null)
        {
            Button returnButton = endButtons[0].GetComponent<Button>();

            DatapersistenceManager.instance.SaveGame();
            print("data saved");
            currentLap = 1;
            laptime = 0;

            foreach (GameObject go in endButtons)
            {
                LeanTween.value(go, go.GetComponent<RectTransform>().anchoredPosition.x, -20.0f, 1.2f)
                .setOnUpdate((float val) =>
                {
                    go.GetComponent<RectTransform>().anchoredPosition = new Vector2(val, go.GetComponent<RectTransform>().anchoredPosition.y);
                })
                .setEaseOutBack();
            }
            foreach (GameObject go in otherStuff)
            {
                LeanTween.value(go, go.GetComponent<RectTransform>().anchoredPosition.y, -110.0f, 0.4f)
                .setOnUpdate((float val) =>
                {
                    go.GetComponent<RectTransform>().anchoredPosition = new Vector2(go.GetComponent<RectTransform>().anchoredPosition.x, val);
                })
                .setEaseInOutQuart();
            }

            GameObject finishedImg, resultsImg;
            finishedImg = GameObject.Find("Race Finished");
            resultsImg = GameObject.Find("Race Results");

            LeanTween.value(finishedImg, finishedImg.GetComponent<RectTransform>().anchoredPosition.y, 150.0f, 0.6f)
            .setOnUpdate((float val) =>
            {
                finishedImg.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(finishedImg.GetComponent<RectTransform>().anchoredPosition.x, val);
            })
            .setEaseInOutQuart();
            //eri kesto tän siirtymiselle, jotta ne ei vaikuta overlappaavan
            LeanTween.value(resultsImg, resultsImg.GetComponent<RectTransform>().anchoredPosition.y, 0.0f, 0.9f)
            .setOnUpdate((float val) =>
            {
                resultsImg.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(resultsImg.GetComponent<RectTransform>().anchoredPosition.x, val);
            })
            .setEaseInOutQuart();

            GameObject leaderboard = GameObject.Find("leaderboardholder");
            LeanTween.value(leaderboard, leaderboard.GetComponent<RectTransform>().anchoredPosition.y, 0.0f, 2f)
            .setOnUpdate((float val) =>
            {
                leaderboard.GetComponent<RectTransform>().anchoredPosition
                = new Vector2(leaderboard.GetComponent<RectTransform>().anchoredPosition.x, val);
            })
            .setEaseInOutQuart();

            returnButton.Select();
        }
    } 

    public void RestartRace()
    {
        if (winMenu != null)
            winMenu.SetActive(false);

        if (startFinishLine != null)
            startFinishLine.gameObject.SetActive(true);

        if (Car1Hud != null)
            Car1Hud.SetActive(true);

        InitializeRace();
        Ranking();
    }

    public void Ranking()
    {
        if (GameManager.instance != null && laptime > 0)
        {
            float score = GameManager.instance.score;
            Rank = score / laptime;
        }
    }

    public void StartRace() // <-- Call this from Waitbeforestart
    {
        racestarted = true;
        if (GameManager.instance.sceneSelected != "tutorial")
            musicControl.StartMusicTracks();
        startTimer = true;
    }
}