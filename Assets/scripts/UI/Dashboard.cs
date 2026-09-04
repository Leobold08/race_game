using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Dashboard : MonoBehaviour
{
    private CarInputActions Controls;
    private MusicManager musicManager;
    private CarColors colors;

    private bool ShouldDashboardOpen = false;
    private RectTransform rect;
    private List<Selectable> dashboardButtons;
    private Button firstSelectedButton;
    [SerializeField] private Button firstSelectedDashboardButton;
    [SerializeField] private Toggle shuffleToggle;
    [SerializeField] private Toggle loopToggle;

    [SerializeField] private RectTransform rected;
    private Image rectedImage;
    private GameObject selected => EventSystem.current.currentSelectedGameObject;
    private GameObject oldSelected;
    private RectTransform selectedRected => selected.GetComponent<RectTransform>();

    private void Awake()
    {
        firstSelectedButton = EventSystem.current.firstSelectedGameObject.GetComponent<Button>();
        rect = GetComponent<RectTransform>();
        rectedImage = rected.GetComponent<Image>();
        dashboardButtons = GetComponentsInChildren<Selectable>().ToList();
        musicManager = FindAnyObjectByType<MusicManager>();
        colors = GameManager.CurrentCar.GetComponentInChildren<CarColors>();
        foreach (var b in dashboardButtons) b.interactable = false;

        Controls = new();
        Controls.CarControls.OpenDashboard.performed += ctx => ToggleDashboard();
    }
    private void OnEnable() => Controls.Enable();
    private void OnDisable()
    {
        Controls.CarControls.OpenDashboard.performed -= ctx => ToggleDashboard();
        Controls.Disable();
    }
    private void OnDestroy()
    {
        Controls.CarControls.OpenDashboard.performed -= ctx => ToggleDashboard();
        Controls.Disable();
    }

    //hell has been established
    //hell has been established twice
    private void Update()
    {
        if (oldSelected == selected || !ShouldDashboardOpen) return;
        //TODO: pitäs päästä eroon LINQistä tässä ja yksinkertastaa tätä if/else-if paskiaista
        //hack fixattu toistaseksi
        else if (!dashboardButtons.Any(b => b.name == selected.name))
        {
            rectedImage.color = Color.clear;
            return;
        }

        oldSelected = selected;
        SetSelectionRectPosition();
    }

    private void ToggleDashboard()
    {
        if (GameManager.racerscript.raceFinished || !GameManager.racerscript.racestarted) return;
        LeanTween.cancel(rect);
        ShouldDashboardOpen = !ShouldDashboardOpen;
        if (ShouldDashboardOpen)
        {
            foreach (var b in dashboardButtons) b.interactable = true;
            firstSelectedDashboardButton.Select();
            rectedImage.color = Color.white;

            LeanTween.value(rect.anchoredPosition.x, -5.0f, 0.4f).setOnUpdate((float val) => { rect.anchoredPosition = new Vector2(val, rect.anchoredPosition.y); }).setEaseInOutCirc().setIgnoreTimeScale(true);
        }
        else
        {
            foreach (var b in dashboardButtons) b.interactable = false;
            firstSelectedButton.Select();
            rectedImage.color = Color.clear;

            LeanTween.value(rect.anchoredPosition.x, 320.0f, 0.4f).setOnUpdate((float val) => { rect.anchoredPosition = new Vector2(val, rect.anchoredPosition.y); }).setEaseInOutCirc().setIgnoreTimeScale(true);
        }
    }

    public void PlayAndPause()
    {
        musicManager.PauseSong();
    }
    public void PreviousTrack()
    {
        musicManager.PreviousSong();
    }
    public void NextTrack()
    {
        musicManager.NextSong();
    }
    public void Shuffle()
    {
        musicManager.shuffleSong = shuffleToggle.isOn;
        shuffleToggle.image.color = shuffleToggle.isOn ? Color.clear : Color.white;
    }
    public void LoopSingular()
    {
        musicManager.SetLoop(loopToggle.isOn);
        loopToggle.image.color = loopToggle.isOn ? Color.clear : Color.white;
    }
    public void Respawn()
    {
        GameManager.racerscript.FadeGameViewAndRespawn();
    }
    public void Lights()
    {
        colors.ToggleLights();
    }

    private void SetSelectionRectPosition()
    {
        rected.sizeDelta = selectedRected.sizeDelta;
        rected.anchoredPosition = selectedRected.anchoredPosition;
    }
}