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
    [SerializeField] private Vector3[] bakedPoints;
    [SerializeField] private float[] curveRadi;

    [ContextMenu("Bake using preset path")]
    void Bake()
    {
        if (path == null) return;
        bakedPoints = BezierMath.ComputeBezierPoints(
            bezierCurveResolution, 
            sampleSize, 
            timeOut, 
            path
            .GetComponentsInChildren<Transform>()
            .Where(t => t != path).Select(t => t.position)
            .ToArray()
        );
    }
    [ContextMenu("Use Road Spline as path")]
    void BakeSpline()
    {
        Transform splineTransform = SplineContainer.GetComponent<Transform>();
        bakedPoints = SplineContainer[0].Select(point => 
                splineTransform.rotation * new Vector3(point.Position.x, point.Position.y, point.Position.z) + splineTransform.position
                ).ToArray();
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
        for (int i = 0; i < bakedPoints.Length; i++)
        {
            radi.Add(BezierMath.GetRadius(bakedPoints[i], bakedPoints[(i + 1) % bakedPoints.Length], bakedPoints[(i + 2) % bakedPoints.Length]));
        }
        curveRadi = radi.ToArray();
    }

    public Vector3[] GetCachedPoints()
    {
        if (bakedPoints.Length == 0 || bakedPoints[0] == Vector3.zero)
        {
            Debug.Log("Baked points are empty");
        }
        return bakedPoints;
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
            Gizmos.DrawSphere(bakedPoints[i], 0.2f);
            Gizmos.DrawLine(bakedPoints[i], bakedPoints[(i+1) % bakedPoints.Count()]);
        }
    }

#endif
}

