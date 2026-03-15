using UnityEngine;

public class AICarColors : MonoBehaviour
{
    private Light[] carLights;
    private Light pointLight;
    public float duration = 1.0f;

    private void OnEnable()
    {
        carLights = GetComponentsInChildren<Light>();
        foreach (Light child in carLights) if (child.CompareTag("pl")) pointLight = child;
    }

    private void Update()
    {
        if (pointLight.enabled)
        {
            float t = Mathf.PingPong(Time.time / duration, 1.0f);
            pointLight.color = Color.Lerp(Color.red, Color.blue, t);
        }
    }
}