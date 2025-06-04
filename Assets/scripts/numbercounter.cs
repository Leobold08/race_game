using UnityEngine;
using UnityEngine.UI;

public class numbercounter : MonoBehaviour
{
    public Sprite[] numberSprites; // Array to hold sprites for digits 0-9
    public GameObject scoreContainer; // Parent GameObject for the score UI
    public GameObject digitPrefab; // Prefab for a single digit (with an Image component)

    private const int digitCount = 7; // Always 7 digits
    private Image[] digitImages;
    private string lastScoreString = "";

    void Start()
    {
        // Instantiate digit objects once and cache their Image components
        digitImages = new Image[digitCount];
        for (int i = 0; i < digitCount; i++)
        {
            GameObject digitGO = Instantiate(digitPrefab, scoreContainer.transform);
            digitImages[i] = digitGO.GetComponent<Image>();
        }
    }

    void Update()
    {
        int score = GameManager.instance != null ? GameManager.instance.score : 0;
        string scoreString = score.ToString().PadLeft(digitCount, '0');

        // Only update UI if the score string has changed
        if (scoreString != lastScoreString)
        {
            UpdateScoreUI(scoreString, lastScoreString);
            lastScoreString = scoreString;
        }
    }

    void UpdateScoreUI(string scoreString, string prevScoreString)
    {
        for (int i = 0; i < digitCount; i++)
        {
            if (digitImages[i] == null)
                continue;

            char digitChar = scoreString[i];
            int digit = digitChar - '0';

            // Only update if this digit has changed
            if (prevScoreString.Length != digitCount || prevScoreString[i] != digitChar)
            {
                if (digit >= 0 && digit <= 9 && numberSprites != null && numberSprites.Length > digit)
                    digitImages[i].sprite = numberSprites[digit];
            }
        }
    }
}
