//miksi vitussa...
using UnityEngine;
using UnityEngine.EventSystems;

public class DropdownOpenSFX : MonoBehaviour, ISubmitHandler
{
    public AudioSource dropdownOpen;
    public void OnSubmit(BaseEventData eventData) => dropdownOpen.Play();
}