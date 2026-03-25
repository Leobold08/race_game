using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OptionCategories : MonoBehaviour
{
    public List<Transform> CategoryContents;
    public List<Button> CategoryButtonList;
    int index = 0;
    CarInputActions Controls;

    void Awake()
    {
        //TODO: joku parempi tapa tälle sillä tää on täynnä conditioneita ja paskaa
        //HUOM. tiedän että tälle vois tehä .Substring ^1 mutta se ei kirjaimellisesti toimi
        CategoryContents = GetComponentsInChildren<Transform>().Where(i => char.IsDigit(i.name[i.name.Length - 1])).OrderBy(a => a.name).Reverse().ToList();
        CategoryButtonList = GetComponentsInChildren<Button>().OrderBy(a => a.name).Reverse().ToList();
        Controls = new CarInputActions();
    }
    void OnEnable()
    {
        Controls.Enable();
        Controls.CarControls.carskinright.performed += ctx => ChangeCategoryManual(true);
        Controls.CarControls.carskinleft.performed += ctx => ChangeCategoryManual(false);
    }
    void OnDestroy()
    {
        Controls.Disable();
        Controls.CarControls.carskinright.performed -= ctx => ChangeCategoryManual(true);
        Controls.CarControls.carskinleft.performed -= ctx => ChangeCategoryManual(false);
    }
    //TODO: tiedät jo kyl mikä tän ongelma on
    private void ChangeCategoryManual(bool change)
    {
        if (index > CategoryButtonList.Count - 1 || index < 0) return;
        if (change) CategoryButtonList[index + 1].Select();
        else CategoryButtonList[index - 1].Select();
    }
    
    public void ChangeCategory()
    {
        //THINK FAST CHUCKLENUTS
        int previousButtonIndex = index;
        int currentButtonIndex = CategoryButtonList.IndexOf(EventSystem.current.currentSelectedGameObject.GetComponent<Button>());
        if (previousButtonIndex == currentButtonIndex) return;
        index = previousButtonIndex > currentButtonIndex ? index -= 1 : index += 1 ;
        Debug.Log($"index: {index}, prev: {previousButtonIndex}, cur: {currentButtonIndex}");

        CategoryContents[previousButtonIndex].gameObject.SetActive(false);
        CategoryContents[currentButtonIndex].gameObject.SetActive(true);
    }
}