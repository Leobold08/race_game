using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ImageCarousel : MonoBehaviour
{
    [SerializeField] private List<Sprite> images;
    [SerializeField] private float timeBetweenChanges;
    [Min(0)]
    [SerializeField] private int loopStartingIndex;
    [Min(1)]
    [SerializeField] private int loopLengthInImages;
    
    private Image source;
    private int index;
    private Coroutine ImageSwap;

    private void Awake() => source = GetComponent<Image>();
    /* private void OnEnable() => ResetImageSwapper(true);
    private void OnDisable() => ResetImageSwapper(false); */
    private void OnDestroy() => ResetImageSwapper(false);

    private IEnumerator ImageSwapper()
    {
        source.sprite = images[loopStartingIndex];
        while (true)
        {
            yield return new WaitForSeconds(timeBetweenChanges);
            index = index >= loopLengthInImages + loopStartingIndex - 1 ? loopStartingIndex : index += 1;
            source.sprite = images[index];
        }
    }
    public void ChangeStartingIndex(int newStartingIndex)
    {
        loopStartingIndex = newStartingIndex;
        ResetImageSwapper(true);
    }
    public void ResetImageSwapper(bool restart = false)
    {
        if (ImageSwap != null)
        {
            StopCoroutine(ImageSwap);
            ImageSwap = null;
        }
        index = loopStartingIndex;
        if (restart) ImageSwap = StartCoroutine(ImageSwapper());
    }
}