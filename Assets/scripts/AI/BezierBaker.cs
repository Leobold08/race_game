using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Splines;

// I love baking beziers they taste so good

[ExecuteAlways]
[RequireComponent(typeof(AiCarManager))]
public class BezierBaker : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("Parent transform containing cachedPoints for the AI path.")]
    public Transform path;
    [Range(1, 100)]
    [SerializeField] private int bezierCurveResolution = 10;
    [Tooltip("How many points to sample for each bezier curve")]
    [Range(3, 10)]
    [SerializeField] private int sampleSize = 5;
    [Tooltip("Amount of time in seconds for when to time out on baking.")]
    [Range(1, 100)]
    [SerializeField] private int timeOut = 10;
    [SerializeField] private SplineContainer SplineContainer;
    [SerializeField] private BakedPoint[] bakedPoints;
    [Serializable]
    public struct BakedPoint
    {
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
    }
    [SerializeField] private float[] curveRadi;

    [ContextMenu("Bake using preset path (Dont use)")]
    void Bake()
    {
        if (path == null) return;

        // Linq didnt wanna work so you get a foreach
        List<BakedPoint> tempPoints = new();
        foreach (Vector3 p in BezierMath.ComputeBezierPoints(
            bezierCurveResolution, 
            sampleSize, 
            timeOut, 
            path
            .GetComponentsInChildren<Transform>()
            .Where(t => t != path).Select(t => t.position)
            .ToArray()
        ))
        {
            tempPoints.Add(new()
            {
                position = p,
                rotation = Quaternion.identity
            });
        }
        bakedPoints = tempPoints.ToArray();
    }

    [ContextMenu("Use Road Spline as path")]
    void BakeSpline()
    {
        Transform splineTransform = SplineContainer.transform;
        List<BakedPoint> tempPoints = new();
        foreach (BezierKnot knot in SplineContainer[0])
        {
            tempPoints.Add(new()
            {
                position = splineTransform.rotation * knot.Position + splineTransform.position,
                rotation = knot.Rotation
            });
        }
        bakedPoints = tempPoints.ToArray();
    }

    [ContextMenu("Bake radi for curves")]
    void BakeRadi()
    {
        if (bakedPoints == null || bakedPoints.Count() == 0)
        {
            Debug.Log("Please bake the points first.");
            return;
        }

        List<float> radi = new();
        for (int i = 0; i < bakedPoints.Count(); i++)
        {
            radi.Add(BezierMath.GetRadius(bakedPoints[i].position, bakedPoints[(i + 1) % bakedPoints.Count()].position, bakedPoints[(i + 2) % bakedPoints.Count()].position));
        }
        curveRadi = radi.ToArray();
    }

    public BakedPoint[] GetCachedPoints()
    {
        if (bakedPoints.Count() == 0 || bakedPoints[0].position == Vector3.zero)
        {
            Debug.Log("Baked points are empty");
        }
        return bakedPoints.ToArray();
    }

    public float[] GetPointRadi()
    {
        if (curveRadi.Length == 0)
        {
            Debug.Log("Radi are empty");
        }
        return curveRadi;
    }

    #if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (bakedPoints.Count() <= 1) return;

        for (int i = 0; i < bakedPoints.Count(); i++)
        {
            Gizmos.DrawSphere(bakedPoints[i].position, 0.2f);
            Gizmos.DrawLine(bakedPoints[i].position, bakedPoints[(i+1) % bakedPoints.Count()].position);
        }
    }

#endif
}

