using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class winmenu : MonoBehaviour
{
    private GameObject[] endButtons;
    private GameObject[] otherStuff;
    public void OnRaceEnd()
    {
        TMP_InputField playerInput = GetComponentInChildren<TMP_InputField>();
        playerInput.Select();
        playerInput.ActivateInputField();
        gameObject.SetActive(true);
    }
    public void FinalizeRaceAndSaveData()
    {
        endButtons = GameObject.FindGameObjectsWithTag("winmenubuttons").OrderBy(r => r.name).ToArray();
        otherStuff = GameObject.FindGameObjectsWithTag("winmenuother").OrderBy(r => r.name).ToArray();

        foreach (GameObject go in endButtons) LeanTween.value(go, go.GetComponent<RectTransform>().anchoredPosition.x, -20.0f, 1.2f).setOnUpdate((float val) => { go.GetComponent<RectTransform>().anchoredPosition = new Vector2(val, go.GetComponent<RectTransform>().anchoredPosition.y); }).setEaseOutBack();
        foreach (GameObject go in otherStuff) LeanTween.value(go, go.GetComponent<RectTransform>().anchoredPosition.y, -110.0f, 0.4f).setOnUpdate((float val) => { go.GetComponent<RectTransform>().anchoredPosition = new Vector2(go.GetComponent<RectTransform>().anchoredPosition.x, val); }).setEaseInOutQuart();

        GameObject finishedImg, resultsImg;
        finishedImg = transform.Find("Race Finished").gameObject;
        resultsImg = transform.Find("Race Results").gameObject;

        LeanTween.value(finishedImg, finishedImg.GetComponent<RectTransform>().anchoredPosition.y, 150.0f, 0.6f).setOnUpdate((float val) => { finishedImg.GetComponent<RectTransform>().anchoredPosition = new Vector2(finishedImg.GetComponent<RectTransform>().anchoredPosition.x, val); }).setEaseInOutQuart();
        LeanTween.value(resultsImg, resultsImg.GetComponent<RectTransform>().anchoredPosition.y, 0.0f, 0.9f).setOnUpdate((float val) => { resultsImg.GetComponent<RectTransform>().anchoredPosition = new Vector2(resultsImg.GetComponent<RectTransform>().anchoredPosition.x, val); }).setEaseInOutQuart();

        GameObject leaderboard = transform.Find("leaderboardholder").gameObject;
        LeanTween.value(leaderboard, leaderboard.GetComponent<RectTransform>().anchoredPosition.y, 0.0f, 2f).setOnUpdate((float val) => { leaderboard.GetComponent<RectTransform>().anchoredPosition = new Vector2(leaderboard.GetComponent<RectTransform>().anchoredPosition.x, val); }).setEaseInOutQuart();
    }

    public void RestartGame()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
    }
    public void MainMenu()
    {
        SceneManager.LoadSceneAsync("MainMenu");
    }
}
