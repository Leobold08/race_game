using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarColors : MonoBehaviour
{
    private Light pointLight;
    [SerializeField] private List<Light> leftLights;
    [SerializeField] private List<Light> rightLights;
    //[SerializeField] private float duration = 3f;
    public AudioSource lights;

    private void Start()
    {
        //LeanTween.value(pointLight.gameObject, new Color(1f, 0f, 0f), new Color(0f, 0f, 1f), duration).setOnUpdate((Color val) => { pointLight.color = val; }).setLoopPingPong();
    }
    [ContextMenu("Assign car lights")]
    void AssignLights()
    {
        leftLights = GetComponentsInChildren<Light>().Where(d => d.CompareTag("ll")).ToList();
        rightLights = GetComponentsInChildren<Light>().Where(d => d.CompareTag("rl")).ToList();
    }

    public void ToggleLights()
    {
        if (GameManager.IsPaused) return;

        foreach (var lightt in leftLights) lightt.enabled = !lightt.enabled;
        foreach (var lightt in rightLights) lightt.enabled = !lightt.enabled;
        lights.Play();
    }
}