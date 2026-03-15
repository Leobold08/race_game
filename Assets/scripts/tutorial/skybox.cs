using UnityEngine;

public class skybox : MonoBehaviour
{
    void OnEnable()
    {
        float duration = 250f;
        LeanTween.value(gameObject, 0f, 360f, duration).setOnUpdate((float val) => { RenderSettings.skybox.SetFloat("_Rotation", val); }).setLoopClamp();
    }
}