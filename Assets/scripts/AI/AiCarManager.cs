using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;

// Add AiSpawnPosition prefabs as children to this manager to set spawn positions for AI

[RequireComponent(typeof(BezierBaker))]
public class AiCarManager : MonoBehaviour
{
    [Header("AI Car Settings")]
    [Tooltip("Number of AI cars to spawn. 0 = no AI cars.")]
    [Range(0, 100)]
    [SerializeField] private byte spawnedAiCarCount = 0;
    [SerializeField] private AiCarController[] AiCarPrefabs;
    private AIDifficulty difficulty;
    public BezierBaker.BakedPoint[] Waypoints { get; private set; }
    public float[] PointRadi { get; private set; }
    public enum AIDifficulty { Beginner, Intermediate, Hard } 
 
    public struct DifficultyStats
    {
        public float minSpeed, maxSpeed, minAcceleration, maxAcceleration, avoidance;
        public DifficultyStats(float minSpeed, float maxSpeed, float minAcceleration, float maxAcceleration, float avoidance)
        {
            this.minSpeed = minSpeed; 
            this.maxSpeed = maxSpeed; 
            this.minAcceleration = minAcceleration; 
            this.maxAcceleration = maxAcceleration;
            this.avoidance = avoidance;
        }
    }

    private readonly Dictionary<AIDifficulty, DifficultyStats> difficultyRanges = new()
    {
        { AIDifficulty.Beginner,     new DifficultyStats(105f, 115f, 240f, 290f, 1f) },
        { AIDifficulty.Intermediate, new DifficultyStats(120f, 130f, 270f, 290f, 0.7f) },
        { AIDifficulty.Hard,         new DifficultyStats(130f, 140f, 280f, 300f, 0.3f) }
    };

    void Start()
    {
        BezierBaker bezierBaker = GetComponent<BezierBaker>();
        Waypoints = bezierBaker.GetCachedPoints();
        PointRadi = bezierBaker.GetPointRadi();
        spawnedAiCarCount = 1;//(byte)PlayerPrefs.GetInt("AIAmount");
        difficulty = (AIDifficulty)PlayerPrefs.GetInt("AILevel");

        // Spawn AI
        if (spawnedAiCarCount > 0)
        {
            // Find Spawn points
            Transform[] spawnPoints = GetComponentsInChildren<Transform>().Where(t => t != transform).ToArray();
            
            for (int i = 0; i < spawnedAiCarCount; i++)
            {
                // Get a random prefab from the list
                AiCarController prefab = AiCarPrefabs[UnityEngine.Random.Range(0, AiCarPrefabs.Length)];
                
                GameObject newAI = Instantiate(prefab.gameObject, spawnPoints[i % spawnPoints.Length].position, transform.rotation);

                // Initialize the controller
                AiCarController controller = newAI.GetComponent<AiCarController>();
                controller.Initialize(this, difficultyRanges[difficulty]);
                
                GameManager.instance.spawnedCars.Add(controller);
            }
        }
    }
}
