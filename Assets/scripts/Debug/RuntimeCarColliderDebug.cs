using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class RuntimeCarColliderDebug : MonoBehaviour
{
    [SerializeField] private bool showColliders = true;
    [SerializeField] private Color localOwnerColor = new Color(0.1f, 1f, 0.2f, 1f);
    [SerializeField] private Color remoteCarColor = new Color(1f, 0.35f, 0.15f, 1f);

    private readonly List<Collider> cachedColliders = new List<Collider>();
    private float refreshTimer;
    private BaseCarController baseController;

    private void Awake()
    {
        baseController = GetComponent<BaseCarController>();
        RefreshColliders();
    }

    private void LateUpdate()
    {
        if (!showColliders) return;

        refreshTimer -= Time.unscaledDeltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshColliders();
            refreshTimer = 1.0f;
        }

        DrawCachedColliders();
    }

    private void RefreshColliders()
    {
        cachedColliders.Clear();
        GetComponentsInChildren(true, cachedColliders);
    }

    private void DrawCachedColliders()
    {
        Color drawColor = GetDrawColor();

        for (int i = 0; i < cachedColliders.Count; i++)
        {
            var col = cachedColliders[i];
            if (col == null || !col.enabled || !col.gameObject.activeInHierarchy) continue;

            if (col is BoxCollider box)
            {
                DrawBoxCollider(box, drawColor);
            }
            else if (col is SphereCollider sphere)
            {
                DrawSphereCollider(sphere, drawColor);
            }
            else if (col is CapsuleCollider capsule)
            {
                DrawCapsuleCollider(capsule, drawColor);
            }
            else if (col is WheelCollider wheel)
            {
                DrawWheelCollider(wheel, drawColor);
            }
            else
            {
                DrawBounds(col.bounds, drawColor);
            }
        }
    }

    private Color GetDrawColor()
    {
        if (baseController != null)
        {
            return baseController.isOwner ? localOwnerColor : remoteCarColor;
        }

        var networkBehaviour = GetComponent<NetworkBehaviour>();
        if (networkBehaviour != null)
        {
            return networkBehaviour.isOwner ? localOwnerColor : remoteCarColor;
        }

        return localOwnerColor;
    }

    private static void DrawBoxCollider(BoxCollider box, Color color)
    {
        Transform t = box.transform;
        Vector3 c = box.center;
        Vector3 e = box.size * 0.5f;

        Vector3[] pts = new Vector3[8];
        pts[0] = t.TransformPoint(c + new Vector3(-e.x, -e.y, -e.z));
        pts[1] = t.TransformPoint(c + new Vector3(e.x, -e.y, -e.z));
        pts[2] = t.TransformPoint(c + new Vector3(e.x, -e.y, e.z));
        pts[3] = t.TransformPoint(c + new Vector3(-e.x, -e.y, e.z));
        pts[4] = t.TransformPoint(c + new Vector3(-e.x, e.y, -e.z));
        pts[5] = t.TransformPoint(c + new Vector3(e.x, e.y, -e.z));
        pts[6] = t.TransformPoint(c + new Vector3(e.x, e.y, e.z));
        pts[7] = t.TransformPoint(c + new Vector3(-e.x, e.y, e.z));

        DrawLine(pts[0], pts[1], color); DrawLine(pts[1], pts[2], color); DrawLine(pts[2], pts[3], color); DrawLine(pts[3], pts[0], color);
        DrawLine(pts[4], pts[5], color); DrawLine(pts[5], pts[6], color); DrawLine(pts[6], pts[7], color); DrawLine(pts[7], pts[4], color);
        DrawLine(pts[0], pts[4], color); DrawLine(pts[1], pts[5], color); DrawLine(pts[2], pts[6], color); DrawLine(pts[3], pts[7], color);
    }

    private static void DrawSphereCollider(SphereCollider sphere, Color color)
    {
        Transform t = sphere.transform;
        Vector3 center = t.TransformPoint(sphere.center);
        float maxScale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
        float radius = sphere.radius * maxScale;

        DrawCircle(center, t.right, t.up, radius, color);
        DrawCircle(center, t.right, t.forward, radius, color);
        DrawCircle(center, t.up, t.forward, radius, color);
    }

    private static void DrawCapsuleCollider(CapsuleCollider capsule, Color color)
    {
        Transform t = capsule.transform;

        Vector3 axis;
        Vector3 orthoA;
        Vector3 orthoB;
        float axisScale;
        float radiusScale;

        switch (capsule.direction)
        {
            case 0:
                axis = t.right;
                orthoA = t.up;
                orthoB = t.forward;
                axisScale = Mathf.Abs(t.lossyScale.x);
                radiusScale = Mathf.Max(Mathf.Abs(t.lossyScale.y), Mathf.Abs(t.lossyScale.z));
                break;
            case 1:
                axis = t.up;
                orthoA = t.right;
                orthoB = t.forward;
                axisScale = Mathf.Abs(t.lossyScale.y);
                radiusScale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
                break;
            default:
                axis = t.forward;
                orthoA = t.right;
                orthoB = t.up;
                axisScale = Mathf.Abs(t.lossyScale.z);
                radiusScale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.y));
                break;
        }

        Vector3 center = t.TransformPoint(capsule.center);
        float radius = capsule.radius * radiusScale;
        float height = capsule.height * axisScale;
        float halfSide = Mathf.Max(0f, (height * 0.5f) - radius);

        Vector3 top = center + axis * halfSide;
        Vector3 bottom = center - axis * halfSide;

        DrawCircle(top, orthoA, orthoB, radius, color);
        DrawCircle(bottom, orthoA, orthoB, radius, color);

        Vector3 rA = orthoA * radius;
        Vector3 rB = orthoB * radius;
        DrawLine(top + rA, bottom + rA, color);
        DrawLine(top - rA, bottom - rA, color);
        DrawLine(top + rB, bottom + rB, color);
        DrawLine(top - rB, bottom - rB, color);
    }

    private static void DrawWheelCollider(WheelCollider wheel, Color color)
    {
        Transform t = wheel.transform;
        Vector3 center = t.TransformPoint(wheel.center);
        float radiusScale = Mathf.Max(Mathf.Abs(t.lossyScale.x), Mathf.Abs(t.lossyScale.z));
        float radius = wheel.radius * radiusScale;

        DrawCircle(center, t.right, t.up, radius, color);
        DrawCircle(center, t.forward, t.up, radius, color);
        DrawCircle(center, t.right, t.forward, radius, color);

        float suspensionScale = Mathf.Abs(t.lossyScale.y);
        Vector3 suspensionStart = center + t.up * (wheel.suspensionDistance * 0.5f * suspensionScale);
        Vector3 suspensionEnd = center - t.up * (wheel.suspensionDistance * 0.5f * suspensionScale);
        DrawLine(suspensionStart, suspensionEnd, color);
    }

    private static void DrawBounds(Bounds bounds, Color color)
    {
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3[] pts = new Vector3[8];
        pts[0] = new Vector3(min.x, min.y, min.z);
        pts[1] = new Vector3(max.x, min.y, min.z);
        pts[2] = new Vector3(max.x, min.y, max.z);
        pts[3] = new Vector3(min.x, min.y, max.z);
        pts[4] = new Vector3(min.x, max.y, min.z);
        pts[5] = new Vector3(max.x, max.y, min.z);
        pts[6] = new Vector3(max.x, max.y, max.z);
        pts[7] = new Vector3(min.x, max.y, max.z);

        DrawLine(pts[0], pts[1], color); DrawLine(pts[1], pts[2], color); DrawLine(pts[2], pts[3], color); DrawLine(pts[3], pts[0], color);
        DrawLine(pts[4], pts[5], color); DrawLine(pts[5], pts[6], color); DrawLine(pts[6], pts[7], color); DrawLine(pts[7], pts[4], color);
        DrawLine(pts[0], pts[4], color); DrawLine(pts[1], pts[5], color); DrawLine(pts[2], pts[6], color); DrawLine(pts[3], pts[7], color);
    }

    private static void DrawCircle(Vector3 center, Vector3 axisA, Vector3 axisB, float radius, Color color)
    {
        const int segments = 24;
        Vector3 prev = center + axisA.normalized * radius;
        float step = (Mathf.PI * 2f) / segments;

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector3 next = center + (axisA.normalized * Mathf.Cos(angle) + axisB.normalized * Mathf.Sin(angle)) * radius;
            DrawLine(prev, next, color);
            prev = next;
        }
    }

    private static void DrawLine(Vector3 from, Vector3 to, Color color)
    {
        Debug.DrawLine(from, to, color, 0f, false);
    }
}