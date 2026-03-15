using UnityEngine;
using System.IO;

[RequireComponent(typeof(LeaderBoard))]
public class leaderboardClearer : MonoBehaviour
{
    private string filePath;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        filePath = Path.Combine(Application.persistentDataPath, "race_result.json");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.P))
            {
                ClearLeaderboardData();
            }
        }
    }

    private void ClearLeaderboardData()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            var LeaderboardManager = GetComponent<LeaderBoard>();
            LeaderboardManager.ClearLeaderboard();
            Debug.Log("Leaderboard data cleared!");
        }
        else
        {
            Debug.LogWarning("No leaderboard data file found to delete.");
        }
    }
}
