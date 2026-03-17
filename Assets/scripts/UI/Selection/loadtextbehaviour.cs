using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(SelectionMenuLoading))]
public class loadtextbehaviour : MonoBehaviour
{
    [SerializeField] private RawImage loadingImage;

    public void SetSpecialLoadTextBehaviour(string key)
    {
        switch (key)
        {
            case "error":
                loadingImage.color = new Color(255, 0, 0);
                break;
            case "reallyspecial":
                Texture2D special = Resources.Load<Texture2D>("loading/reallyspecial");
                loadingImage.texture = special;
                break;
            default:
                break;
        }
    }
}
