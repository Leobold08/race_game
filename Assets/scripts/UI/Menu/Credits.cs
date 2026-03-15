using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class Credits : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    
    private int index = 0;

    [Header("UI")]
    [SerializeField] private Text thetext;

    [Header("Data")]
    private string[] tasks;

    [Header("Specific Info Popup")]
    private string[] whatHeDo;
    [SerializeField] private Text popupInfo;
    [SerializeField] private AudioSource creditsTrack;

    private int musictweenIDstart = -1;
    private int musictweenIDend = -1;

    //BEST VALUE!!
    private Coroutine inactivityCoroutine;

    private void Awake()
    {
        tasks = new string[]
        {
            "PixelatedGoose\nPROJECT LEAD\ngraphical design, map design, shaders",
            "Vizl87\nLEAD PROGRAMMER\ncar controller, game data handling",
            "ThatOneGuy\nCOMPOSER, PROGRAMMER\nmusic and sfx, selection menus",
            "Leobold\nPROGRAMMER\nracing mechanics",
            "lamelemon\nPROGRAMMER\nai cars, refactoring scripts",
            "rojp\nDESIGNER\nother help, car textures",
        };
        whatHeDo = new string[]
        {
            "- all pixel art graphics\n- all map design\n- all shaders\n- leaderboard system",
            "- car controller\n- save data system\n- wheel support\n- score system",
            "- all music and sound effects\n- selection menus\n- some graphics assets\n- bug fixing",
            "- lap and time system",
            "- AI car code\n- user input check system",
            "- car textures\n- ideas",
        };
    }

    public void UpdateIconSelection()
    {        
        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected.TryGetComponent(out Selectable target))
        {
            bool hasIndex = int.TryParse(target.name.Substring(0, 1), out index);
            if (hasIndex)
            {
                thetext.text = tasks[index];
                popupInfo.text = whatHeDo[index];
            }
            else
            {
                thetext.text = "";
                popupInfo.text = "";
            }
        }
    }

    public void CreditsMusic(bool active)
    {
        switch (active)
        {
            case true:
                if (musictweenIDend != -1)
                    LeanTween.cancel(musictweenIDend);
                creditsTrack.Stop();
                creditsTrack.Play();

                musictweenIDstart = LeanTween.value(creditsTrack.volume, 0.27f, 0.9f).setOnUpdate(val => creditsTrack.volume = val).id;
                break;
            case false:
                if (musictweenIDstart != -1)
                    LeanTween.cancel(musictweenIDstart);
                musictweenIDend = LeanTween.value(creditsTrack.volume, 0.0f, 0.9f).setOnUpdate(val => creditsTrack.volume = val).id;
                break;
        }
    }

    public void Inactive()
    {
        inactivityCoroutine = StartCoroutine(InactiveCoroutine());
    }
    private IEnumerator InactiveCoroutine()
    {
        //cacheen laittaminen paskois tän kaiken lol
        yield return new WaitForSecondsRealtime(30f);
        videoPlayer.Prepare();
        videoPlayer.Play();
        creditsTrack.volume = 0f;
    }
    public void StopInactive()
    {
        videoPlayer.Stop();
        creditsTrack.volume = 0.27f;
        StopCoroutine(inactivityCoroutine);
    }
}