using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(SplineContainer))]
[ExecuteInEditMode]
public class SplineMeshDeformer : MonoBehaviour
{
    [Tooltip("The pre-existing mesh you want to extrude/bend (e.g., a straight tunnel segment)")]
    public Mesh SourceMesh;

    [Tooltip("Repeat the mesh multiple times along the spline?")]
    public bool LoopMesh = true;

    private SplineContainer m_SplineContainer;
    private Mesh m_DeformedMesh;

    void OnEnable()
    {
        m_SplineContainer = GetComponent<SplineContainer>();
        DeformMesh();
    }

    public void DeformMesh()
    {
        if (m_SplineContainer == null || m_SplineContainer.Splines.Count == 0 || SourceMesh == null) 
            return;

        if (m_DeformedMesh == null)
        {
            m_DeformedMesh = new Mesh();
            m_DeformedMesh.name = "Deformed Spline Mesh";
            GetComponent<MeshFilter>().sharedMesh = m_DeformedMesh;
        }

        m_DeformedMesh.Clear();

        Spline spline = m_SplineContainer.Splines[0];
        float splineLength = m_SplineContainer.CalculateLength();
        float meshLength = SourceMesh.bounds.size.z;

        if (meshLength <= 0) return;

        // Calculate how many times the mesh should repeat to fill the spline
        int repeatCount = LoopMesh ? Mathf.FloorToInt(splineLength / meshLength) : 1;
        if (repeatCount < 1) repeatCount = 1;

        Vector3[] sourceVerts = SourceMesh.vertices;
        Vector3[] sourceNormals = SourceMesh.normals;
        Vector2[] sourceUvs = SourceMesh.uv;
        int[] sourceTriangles = SourceMesh.triangles;
        
        int vertsPerMesh = sourceVerts.Length;
        int trisPerMesh = sourceTriangles.Length;

        // Prepare arrays for the final massive mesh
        Vector3[] finalVerts = new Vector3[vertsPerMesh * repeatCount];
        Vector3[] finalNormals = new Vector3[vertsPerMesh * repeatCount];
        Vector2[] finalUvs = new Vector2[vertsPerMesh * repeatCount];
        int[] finalTriangles = new int[trisPerMesh * repeatCount];

        for (int i = 0; i < repeatCount; i++)
        {
            float zOffset = i * meshLength; // Where this segment starts along the spline
            
            // 1. Map Vertices and Normals
            for (int v = 0; v < vertsPerMesh; v++)
            {
                int finalIndex = (i * vertsPerMesh) + v;
                Vector3 origVert = sourceVerts[v];
                
                // Shift the vertex Z by the mesh bounds minimum so it starts at 0, then add the segment offset
                float distanceAlongSpline = (origVert.z - SourceMesh.bounds.min.z) + zOffset; 
                
                // Calculate percentage along the spline (t)
                float t = distanceAlongSpline / splineLength;
                t = Mathf.Clamp01(t);

                // Evaluate the spline at t
                m_SplineContainer.Evaluate(0, t, out float3 pos, out float3 forward, out float3 up);
                
                // Ensure proper vectors
                forward = math.normalize(forward);
                up = math.normalize(up);
                float3 right = math.cross(up, forward);

                // Position the vertex relatively using the spline's right/up vectors and the vertex's X/Y
                float3 bentPosition = pos + (right * origVert.x) + (up * origVert.y);
                
                finalVerts[finalIndex] = bentPosition;
                finalUvs[finalIndex] = sourceUvs[v]; // Copy UVs directly

                // (Optional) Map Normals to properly react to lighting
                float3 origNormal = sourceNormals[v];
                float3 bentNormal = (right * origNormal.x) + (up * origNormal.y) + (forward * origNormal.z);
                finalNormals[finalIndex] = bentNormal;
            }

            // 2. Map Triangles
            for (int t = 0; t < trisPerMesh; t++)
            {
                finalTriangles[(i * trisPerMesh) + t] = sourceTriangles[t] + (i * vertsPerMesh);
            }
        }

        m_DeformedMesh.vertices = finalVerts;
        m_DeformedMesh.normals = finalNormals;
        m_DeformedMesh.uv = finalUvs;
        m_DeformedMesh.triangles = finalTriangles;
        
        m_DeformedMesh.RecalculateBounds();
    }
}