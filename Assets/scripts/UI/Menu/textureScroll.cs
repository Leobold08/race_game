using UnityEngine;
using UnityEngine.UI;

public class ScrollingImageEffect : MonoBehaviour
{
    [SerializeField] private RawImage targetImage;
    [SerializeField] private float scrollSpeed = 0.17f;
    private int buttontween;

    private void Start()
    {
        buttontween = LeanTween.value(0.0f, 0.0f + 1.0f, scrollSpeed).setOnUpdate((float val) => {
            Rect rect = targetImage.uvRect;
            rect.x = val;
            targetImage.uvRect = rect; }).setLoopClamp().id;
    }
    private void OnDestroy()
    {
        LeanTween.cancel(buttontween);
    }
}