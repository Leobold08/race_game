using System.Collections.Generic;
using UnityEngine;

public class MainMenuCarRotation : MonoBehaviour
{
    [SerializeField] private List<Rigidbody> car;

    void Start()
    {
        Vector3 startRotation = car[0].transform.eulerAngles;
        foreach (var i in car) LeanTween.value(gameObject, (Vector3 val) => { i.transform.eulerAngles = val; }, startRotation, new(0.0f, startRotation.y + 360f, 0.0f), 8.0f).setLoopClamp();
    }
}