using UnityEngine;
using UnityEngine.UI;

public class OptionComponent : MonoBehaviour
{
    //Statics are decent choice when you're making library and you want to simplify API, so people can use it easier
    //Using examples from C# libraries - instead of new Math().Abs(-1) you can use Math.Abs(-1).
    //T. random jätkä redditistä
    private void Start()
    {
        Debug.Log($"hi! i am {GetComponent<Selectable>()}");
    }
}