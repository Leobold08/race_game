using UnityEngine;

public class MainMenuCarLights : MonoBehaviour
{
    private Light[] carLights;
    private Light pointLight;
    private Light right;
    private Light left;

    private void Start()
    {
        carLights = GetComponentsInChildren<Light>();
        foreach (Light child in carLights)
        {
            if (child.CompareTag("pl")) pointLight = child;
            else if (child.CompareTag("rl")) right = child;
            else if (child.CompareTag("ll")) left = child;
        }

        Color amisPink = new(0.945f, 0.318f, 0.831f, 1.0f);
        Color amisGreen = new(0.129f, 0.933f, 0.541f, 1.0f);
        float tweenDuration = 1.7f;

        LeanTween.value(left.gameObject, amisGreen, amisPink, tweenDuration).setOnUpdate((Color val) => { left.color = val; }).setLoopPingPong();
        LeanTween.value(right.gameObject, amisPink, amisGreen, tweenDuration).setOnUpdate((Color val) => { right.color = val; }).setLoopPingPong();
    }

    public void CheckLightState()
    {
        bool lightsOptionEnabled = PlayerPrefs.GetInt("optionTest_value") == 1;

        pointLight.enabled = lightsOptionEnabled;
        left.enabled = lightsOptionEnabled;
        right.enabled = lightsOptionEnabled;
    }
}