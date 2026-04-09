using UnityEngine;

[ExecuteAlways]
public class EditorDisableWaterInEditMode : MonoBehaviour
{
    private void OnEnable() => Water();
    private void OnValidate() => Water();

    private void Water()
    {
        if (Application.isPlaying) gameObject.SetActive(true);
        else gameObject.SetActive(false);
    }
}