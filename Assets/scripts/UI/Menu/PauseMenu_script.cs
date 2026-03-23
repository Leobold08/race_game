using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    private GameObject Optionspanel;
    private Selectable firstSelected;

    private CarInputActions Controls;
    private RacerScript racerScript;
    private GameObject fullMenu;

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
        if (!Optionspanel.activeSelf && !racerScript.raceFinished && racerScript.racestarted) TogglePauseMenu();
    }

    public void TogglePauseMenu()
    {
        fullMenu.SetActive(!fullMenu.activeSelf);
        LeanTween.cancel(fullMenu);
        musicControl musicCtrl = FindFirstObjectByType<musicControl>();
        soundFXControl soundFXctrl = FindFirstObjectByType<soundFXControl>();
        Time.timeScale = fullMenu.activeSelf ? 0 : 1;
        if (musicCtrl != null) musicCtrl.PausedMusicHandler();
        if (soundFXctrl != null && racerScript.racestarted) soundFXctrl.PauseStateHandler();

        if (!fullMenu.activeSelf) return;

        fullMenu.transform.localPosition = new Vector3(0.0f, 470.0f, 0.0f);
        LeanTween.moveLocalY(fullMenu, 0.0f, 0.4f).setEaseInOutCirc().setIgnoreTimeScale(true);
        firstSelected.Select();
    }

    public void QuitGame()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }
    public void RestartGame()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
}
