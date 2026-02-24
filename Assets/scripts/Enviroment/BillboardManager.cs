using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PurrNet;

public class BillboardManager : MonoBehaviour
{
    static readonly List<BillboardObject> objects = new();

    [Header("Global Billboard Settings")]
    public Camera billboardCamera;
    public float updateInterval = 0.2f;
    public float lenientAngle = 90f;

    float timer;
    bool cameraSearchActive;

    void Awake()
    {
        if (billboardCamera == null)
        {
            cameraSearchActive = true;
            StartCoroutine(FindLocalPlayerCamera());
        }
    }

    IEnumerator FindLocalPlayerCamera()
    {
        // Keep searching until we find the local player's camera
        while (billboardCamera == null)
        {
            yield return new WaitForSeconds(0.1f);

            // Try Camera.main first
            if (Camera.main != null)
            {
                billboardCamera = Camera.main;
                cameraSearchActive = false;
                yield break;
            }

            // Look for a camera attached to a NetworkBehaviour that isOwner
            var cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                var networkBehaviour = cam.GetComponentInParent<NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.isOwner)
                {
                    billboardCamera = cam;
                    cameraSearchActive = false;
                    yield break;
                }
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return; 
        timer = 0f;

        if (billboardCamera == null)
            return;

        Vector3 camPos = billboardCamera.transform.position;
        Vector3 camForward = billboardCamera.transform.forward;

        for (int i = 0; i < objects.Count; i++)
        {
            var obj = objects[i];
            if (obj == null) continue;

            Vector3 toObj = (obj.transform.position - camPos).normalized;

            if (Vector3.Angle(camForward, toObj) > lenientAngle)
                continue;

            Vector3 lookDir = camPos - obj.transform.position;
            lookDir.y = 0f;

            if (lookDir.sqrMagnitude > 0.001f)
            {
                Quaternion baseRotation = Quaternion.LookRotation(-lookDir);
                obj.transform.rotation = baseRotation * obj.RotationOffset;
            }
        }
    }

    public static void Register(BillboardObject obj)
    {
        if (!objects.Contains(obj))
            objects.Add(obj);
    }

    public static void Unregister(BillboardObject obj)
    {
        objects.Remove(obj);
    }
}
