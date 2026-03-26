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
    [SerializeField] private List<Tuple<Vector3, Quaternion>> bakedPoints;
    [SerializeField] private float[] curveRadi;

    [ContextMenu("Bake using preset path (Dont use)")]
    void Bake()
    {
        if (path == null) return;

        // Linq didnt wanna work so you get a foreach
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
            bakedPoints.Add(new(p, Quaternion.identity));
        }
    }

    [ContextMenu("Use Road Spline as path")]
    void BakeSpline()
    {
        Transform splineTransform = SplineContainer.GetComponent<Transform>();
        foreach (BezierKnot knot in SplineContainer[0])
        {
            bakedPoints.Add(new(splineTransform.rotation * knot.Position + splineTransform.position, knot.Rotation));
        }
    }

    [ContextMenu("Bake radi for curves")]
    void BakeRadi()
    {
        if (bakedPoints == null)
        {
            Debug.Log("Please bake the points first.");
            return;
        }

        List<float> radi = new();
        for (int i = 0; i < bakedPoints.Count(); i++)
        {
            radi.Add(BezierMath.GetRadius(bakedPoints[i].Item1, bakedPoints[(i + 1) % bakedPoints.Count()].Item1, bakedPoints[(i + 2) % bakedPoints.Count()].Item1));
        }
        curveRadi = radi.ToArray();
    }

    public Tuple<Vector3, Quaternion>[] GetCachedPoints()
    {
        if (bakedPoints.Count() == 0 || bakedPoints[0].Item1 == Vector3.zero)
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
            Gizmos.DrawSphere(bakedPoints[i].Item1, 0.2f);
            Gizmos.DrawLine(bakedPoints[i].Item1, bakedPoints[(i+1) % bakedPoints.Count()].Item1);
        }
    }

#endif
}

