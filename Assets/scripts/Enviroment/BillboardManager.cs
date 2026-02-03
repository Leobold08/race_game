using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillboardManager : MonoBehaviour
{
    static readonly List<BillboardObject> objects = new();

    [Header("Global Billboard Settings")]
    public Camera billboardCamera;
    public float updateInterval = 0.2f;
    public float lenientAngle = 90f;

    float timer;

    void Awake()
    {
        if (billboardCamera == null)
            StartCoroutine(FindCameraDelayed());
    }

    IEnumerator FindCameraDelayed()
    {
        yield return new WaitForSeconds(0.05f); // Wait 50ms before retrying
        if (billboardCamera == null)
            billboardCamera = Camera.main;
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
                obj.transform.rotation = Quaternion.LookRotation(-lookDir);
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
