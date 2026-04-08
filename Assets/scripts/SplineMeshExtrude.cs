using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Splines;

[ExecuteAlways]
public class SplineMeshExtrude : MonoBehaviour
{
    private enum Axis
    {
        X, Y, Z,
        NegativeX, NegativeY, NegativeZ
    }

    [System.Serializable]
    public struct SegmentPreset
    {
        public Mesh templateMesh;
        public Material material;
    }

    [SerializeField]
    private List<SegmentPreset> segmentPresets = new List<SegmentPreset>();
    [SerializeField]
    private Axis extrusionAxis;
    [SerializeField]
    private Vector3 localMeshScale = Vector3.one;
    [SerializeField]
    private float extrusionInterval = 10f;
    [SerializeField]
    private bool smoothFaces = true;
    [SerializeField]
    private bool useWorldUp = true;

    [SerializeField]
    private Vector3 meshRotationOffset = Vector3.zero; // Allows rotating the mesh to face the spline

    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private SplineContainer splineContainer;
    private Spline spline;

    private Vector3[] templateVertices;

    private void OnEnable()
    {
        InitializeComponents();
        Spline.Changed += OnSplineChanged;
        RebuildMesh();
    }

    private void OnDisable()
    {
        Spline.Changed -= OnSplineChanged;
    }

    private void OnValidate()
    {
        // OnValidate can trigger before OnEnable in some cases, so ensure components are ready
        if (meshFilter == null || splineContainer == null) 
            InitializeComponents();

        RebuildMesh();
    }

    private void OnSplineChanged(Spline spline, int knotIndex, SplineModification modificationType)
    {
        // Rebuild when the spline points are moved
        if (this.spline == spline)
        {
            RebuildMesh();
        }
    }

    private void InitializeComponents()
    {
        meshCollider = gameObject.GetComponent<MeshCollider>();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (!meshFilter)
            Debug.LogError($"SplineMeshExtrude: Gameobject {gameObject.name} does not have an attached mesh filter.");

        splineContainer = gameObject.GetComponent<SplineContainer>();
        spline = splineContainer?.Spline;
    }

    private void RebuildMesh()
    {
        if (spline == null || segmentPresets == null || segmentPresets.Count == 0 || meshFilter == null) return;

        Mesh generatedMesh = GenerateMesh();
        meshFilter.sharedMesh = generatedMesh;

        MeshRenderer renderer = gameObject.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material[] mats = new Material[segmentPresets.Count];
            for(int i=0; i<segmentPresets.Count; i++)
                mats[i] = segmentPresets[i].material;
            renderer.sharedMaterials = mats;
        }

        if (meshCollider)
            meshCollider.sharedMesh = generatedMesh;
    }

    private Mesh GenerateMesh()
    {
        Mesh mesh = new Mesh();
        bool success = SplineUtil.SampleSplineInterval(spline, transform, extrusionInterval, 
                                                       out Vector3[] positions, out Vector3[] tangents, out Vector3[] upVectors);
        if (!success)
        {
            Debug.LogError("SplineMeshExtrude: GenerateMesh: Error encountered when sampling spline. Aborting");
            return mesh;
        }

        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();

        List<List<int>> subMeshTriangles = new List<List<int>>();
        for (int i = 0; i < segmentPresets.Count; i++)
            subMeshTriangles.Add(new List<int>());

        int curveCount = spline.GetCurveCount();

        for (int i = 0; i < positions.Length - 1; i++)
        {
            float tValue = (float)i / (positions.Length - 1);
            int segmentIndex = Mathf.FloorToInt(tValue * curveCount);
            if (segmentIndex >= curveCount) segmentIndex = curveCount - 1;

            int presetIndex = segmentIndex < segmentPresets.Count ? segmentIndex : segmentPresets.Count - 1;
            SegmentPreset currentPreset = segmentPresets[presetIndex];
            
            Mesh currentTemplateMesh = currentPreset.templateMesh;
            if (currentTemplateMesh == null)
            {
                currentPreset = segmentPresets[0];
                currentTemplateMesh = currentPreset.templateMesh;
                presetIndex = 0;
                if (currentTemplateMesh == null) continue;
            }

            // distinguish verticies from first and second edges
            (int[] firstEdge, int[] secondEdge) = GetEdgeIndicies(currentTemplateMesh.vertices, extrusionAxis);

            templateVertices = CollapsePointsOnAxis(currentTemplateMesh.vertices, extrusionAxis);
            templateVertices = templateVertices.Select(position => Vector3.Scale(position, localMeshScale)).ToArray();

            AppendMeshSegment(vertices, subMeshTriangles[presetIndex], normals, uvs,
                              positions[i], tangents[i], upVectors[i], positions[i + 1], tangents[i + 1], upVectors[i + 1],
                              firstEdge, secondEdge, currentTemplateMesh);
        }

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.subMeshCount = segmentPresets.Count;
        for (int i = 0; i < segmentPresets.Count; i++)
        {
            mesh.SetTriangles(subMeshTriangles[i].ToArray(), i);
        }

        return mesh;
    }

    private void AppendMeshSegment(List<Vector3> vertices, List<int> triangles, List<Vector3> normals, List<Vector2> uvs,
        Vector3 firstPos, Vector3 firstTangent, Vector3 firstUp, Vector3 secondPos, Vector3 secondTangent, Vector3 secondUp, 
        int[] firstEdgeIndicies, int[] secondEdgeIndicies, Mesh extrusionTemplateMesh)
    {
        Vector3[] newVertices = new Vector3[templateVertices.Length];
        Vector3[] newNormals = new Vector3[extrusionTemplateMesh.normals.Length];

        // 1. Calculate offset rotation from the inspector
        Quaternion offsetRotation = Quaternion.Euler(meshRotationOffset);

        Quaternion rotation = useWorldUp ? Quaternion.LookRotation(new Vector3(firstTangent.x, 0, firstTangent.z), Vector3.up) : 
                                           Quaternion.LookRotation(firstTangent, firstUp);
        Quaternion flatRotation = Quaternion.identity;
        if (!smoothFaces)
        {
            if (useWorldUp)
            {
                Vector3 avgTangentDir = new Vector3(firstTangent.x + secondTangent.x, 0, firstTangent.z + secondTangent.z);
                flatRotation = Quaternion.LookRotation(avgTangentDir, Vector3.up);
            }
            else
            {
                Vector3 avgTangentDir = firstTangent + secondTangent;
                Vector3 avgUpDir = firstUp + secondUp;
                flatRotation = Quaternion.LookRotation(avgTangentDir, avgUpDir);
            }
        }
        
        Quaternion normalRotation = smoothFaces ? rotation : flatRotation;

        // Apply offset to the first edge
        Quaternion finalRotationFirst = rotation * offsetRotation;
        Quaternion finalNormalRotationFirst = normalRotation * offsetRotation;

        foreach (int index in firstEdgeIndicies)
        {
            newVertices[index] = (finalRotationFirst * templateVertices[index]) + firstPos;
            newNormals[index] = finalNormalRotationFirst * extrusionTemplateMesh.normals[index];
        }

        rotation = useWorldUp ? Quaternion.LookRotation(new Vector3(secondTangent.x, 0, secondTangent.z), Vector3.up) :
                                Quaternion.LookRotation(secondTangent, secondUp);
        normalRotation = smoothFaces ? rotation : flatRotation;

        // Apply offset to the second edge
        Quaternion finalRotationSecond = rotation * offsetRotation;
        Quaternion finalNormalRotationSecond = normalRotation * offsetRotation;

        foreach (int index in secondEdgeIndicies)
        {
            newVertices[index] = (finalRotationSecond * templateVertices[index]) + secondPos;
            newNormals[index] = finalNormalRotationSecond * extrusionTemplateMesh.normals[index];
        }

        int prevVerticiesLength = vertices.Count;

        vertices.AddRange(newVertices);
        triangles.AddRange(extrusionTemplateMesh.triangles.Select(index => index + prevVerticiesLength));
        normals.AddRange(newNormals);
        uvs.AddRange(extrusionTemplateMesh.uv);
    }

    private (int[] first, int[] second) GetEdgeIndicies(Vector3[] templateVertices, Axis axis)
    {
        List<int> firstIndicies = new List<int>();
        List<int> secondIndicies = new List<int>();

        // Find the correct index (0 for X, 1 for Y, 2 for Z)
        int vectorIndex = (axis == Axis.X || axis == Axis.NegativeX) ? 0 : 
                          (axis == Axis.Y || axis == Axis.NegativeY) ? 1 : 2;

        // Determine if we need to flip the logic for negative axes
        bool isNegative = axis == Axis.NegativeX || axis == Axis.NegativeY || axis == Axis.NegativeZ;

        for (int i = 0; i < templateVertices.Length; i++)
        {
            bool condition = isNegative ? templateVertices[i][vectorIndex] > 0 : templateVertices[i][vectorIndex] < 0;
            
            if (condition)
                firstIndicies.Add(i);
            else
                secondIndicies.Add(i);
        }

        return (firstIndicies.ToArray(), secondIndicies.ToArray());
    }

    // set the specified axis to zero for each point
    // returns a new array, and does not modify the input array
    private Vector3[] CollapsePointsOnAxis(Vector3[] points, Axis axis)
    {
        Vector3[] collapsedPoints = new Vector3[points.Length];
        Vector3 axisCollapseVector = (axis == Axis.X || axis == Axis.NegativeX) ? new Vector3(0, 1, 1) :
                                     (axis == Axis.Y || axis == Axis.NegativeY) ? new Vector3(1, 0, 1) : 
                                                                                  new Vector3(1, 1, 0);

        for (int i = 0; i < points.Length; i++)
        {
            // element wise multiplication
            collapsedPoints[i] = Vector3.Scale(points[i], axisCollapseVector);
        }
        return collapsedPoints;
    }
}
