
using System.Collections.Generic;
using UnityEngine;

public class BillboardManager : MonoBehaviour
{
    static readonly List<BillboardObject> objects = new();

    [Header("Global Billboard Settings")]
    public Camera billboardCamera;
    public float updateInterval = 0.2f;
    public float lenientAngle = 90f;

    [Header("Camera Resolve")]
    [Tooltip("How often to retry resolving the local camera when none is assigned.")]
    public float cameraRetryInterval = 0.5f;

    float timer;
    float cameraRetryTimer;

    void Awake()
    {
        TryResolveCamera(true);
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        if (!TryResolveCamera(false))
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
                obj.transform.rotation = Quaternion.LookRotation(-lookDir);
        }
    }

    bool TryResolveCamera(bool force)
    {
        if (billboardCamera != null && billboardCamera.enabled && billboardCamera.gameObject.activeInHierarchy)
            return true;

        if (!force)
        {
            cameraRetryTimer += updateInterval;
            if (cameraRetryTimer < cameraRetryInterval)
                return false;
        }

        cameraRetryTimer = 0f;

        var mainCam = Camera.main;
        if (mainCam != null && mainCam.enabled && mainCam.gameObject.activeInHierarchy)
        {
            billboardCamera = mainCam;
            return true;
        }

        var cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; i++)
        {
            var cam = cameras[i];
            if (cam == null || !cam.enabled || !cam.gameObject.activeInHierarchy)
                continue;

            var listener = cam.GetComponent<AudioListener>();
            if (listener != null && listener.enabled)
            {
                billboardCamera = cam;
                return true;
            }
        }

        if (cameras.Length > 0)
        {
            for (int i = 0; i < cameras.Length; i++)
            {
                var cam = cameras[i];
                if (cam != null && cam.enabled && cam.gameObject.activeInHierarchy)
                {
                    billboardCamera = cam;
                    return true;
                }
            }
        }

        billboardCamera = null;
        return false;
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
