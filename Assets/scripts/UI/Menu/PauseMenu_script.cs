using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private GameObject Optionspanel;
    public GameObject[] pauseMenuObjects;
    private Selectable firstSelected;

    private bool optionsOpen => Optionspanel != null && Optionspanel.activeSelf;
    private CarInputActions Controls;
    public RacerScript racerScript;
    public GameObject fullMenu;

    void Awake()
    {
        Controls = new CarInputActions();
        Controls.Enable();
        Controls.CarControls.pausemenu.performed += PauseMenuCheck;

        fullMenu = transform.Find("menuCanvas").gameObject;
        Optionspanel = GetComponentInChildren<optionScript>().gameObject;
        firstSelected = EventSystem.current.firstSelectedGameObject.GetComponent<Selectable>();
    }

    private void OnEnable() => Controls.Enable();
    private void OnDisable()
    {
        Controls.CarControls.pausemenu.performed -= PauseMenuCheck;
        Controls.Disable();
    }
    private void OnDestroy()
    {
        Controls.CarControls.pausemenu.performed -= PauseMenuCheck;
        Controls.Disable();
    }

    void Start()
    {
        fullMenu.SetActive(false);
        Optionspanel.SetActive(false);
        racerScript = FindFirstObjectByType<RacerScript>();
    }

    void PauseMenuCheck(InputAction.CallbackContext context)
    {
        if (!optionsOpen && !racerScript.raceFinished && racerScript.racestarted) TogglePauseMenu();
    }

    private void TogglePauseMenu()
    {
        if (pauseMenuObjects == null || pauseMenuObjects.Length == 0) return;
        LeanTween.cancel(pauseMenuObjects[0]);
        bool isActive = pauseMenuObjects[0].activeSelf;

        SetPausedState(!isActive);
        foreach (GameObject obj in pauseMenuObjects) obj.SetActive(!isActive);
        if (!isActive)
        {
            pauseMenuObjects[0].transform.localPosition = new Vector3(0.0f, 470.0f, 0.0f);
            LeanTween.moveLocalY(pauseMenuObjects[0], 0.0f, 0.4f).setEaseInOutCirc().setIgnoreTimeScale(true);
            firstSelected.Select();
        }
    }

    //ERITTÄIN ronnyinen funktio tääl näi
    //ja nyt se on kaks kertaa ronnyisempi
    public void SetPausedState(bool paused)
    {
        Time.timeScale = paused ? 0 : 1;
        GameManager.instance.isPaused = paused;

        musicControl musicCtrl = FindFirstObjectByType<musicControl>();
        if (musicCtrl != null) musicCtrl.PausedMusicHandler();
        soundFXControl soundFXctrl = FindFirstObjectByType<soundFXControl>();
        if (soundFXctrl != null && racerScript.racestarted) soundFXctrl.PauseStateHandler();
        musicControlTutorial musicCtrlTutorial = FindFirstObjectByType<musicControlTutorial>();
        if (musicCtrlTutorial != null && racerScript.racestarted) musicCtrlTutorial.PausedMusicHandler();
    }

    public void ContinueGame()
    {
        foreach (GameObject obj in pauseMenuObjects) obj.SetActive(false);
        SetPausedState(false);
    }
    public void QuitGame()
    {
        SetPausedState(false);
        SceneManager.LoadSceneAsync("MainMenu");
    }
    public void RestartGame()
    {
        SetPausedState(false);
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
}
