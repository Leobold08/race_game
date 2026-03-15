using UnityEngine;

public class PlayerCarColors : MonoBehaviour
{
    private Light[] carLights;
    private Light pointLight;
    private Light right;
    private Light left;
    public float duration = 1.0f;
    [SerializeField] private AudioSource lights;

    CarInputActions Controls;

    void Awake()
    {
        Controls = new CarInputActions();
        Controls.Enable();
    }

    private void OnEnable()
    {
        carLights = GetComponentsInChildren<Light>();
        foreach (Light child in carLights)
        {
            if (child.CompareTag("pl")) pointLight = child;
            else if (child.CompareTag("rl")) right = child;
            else if (child.CompareTag("ll")) left = child;
        }

        Controls.CarControls.lights.performed += ctx => LightsState(1, false);
        Controls.CarControls.underglow.performed += ctx => LightsState(2, false);
    }

    private void OnDisable()
    {
        Controls.Disable();
    }

    private void Update()
    {
        if (pointLight.enabled)
        {
            float t = Mathf.PingPong(Time.time / duration, 1.0f);
            pointLight.color = Color.Lerp(Color.red, Color.blue, t);
        }
    }
    
    /// <summary>
    /// tarkistaa, saako valoja vaihtaa. tavallisesti kutsutaan inputin kautta
    /// </summary>
    /// <returns></returns>
    /// <param name="shouldSet">jos funktio kutsutaan, pitäskö sen muuttaa valot asetuksen mukaiseksi?</param>
    public void LightsState(int lightSelected, bool shouldSet = false)
    {
        bool lightsOptionEnabled = PlayerPrefs.GetInt("optionTest_value") == 1;

        if (shouldSet)
        {
            left.enabled = lightsOptionEnabled;
            right.enabled = lightsOptionEnabled;
            pointLight.enabled = lightsOptionEnabled;
            return;
        }
        if (!lightsOptionEnabled || GameManager.instance.isPaused) return;

        switch (lightSelected)
        {
            case 1:
                left.enabled = !left.enabled;
                right.enabled = !right.enabled;
                break;
            case 2:
                pointLight.enabled = !pointLight.enabled;
                break;
            case 3:
                left.enabled = !left.enabled;
                right.enabled = !right.enabled;
                pointLight.enabled = !pointLight.enabled;
                break;
        }

        lights.Play();
    }
}