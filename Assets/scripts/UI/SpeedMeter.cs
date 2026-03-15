using UnityEngine;
using UnityEngine.UI;

public class SpeedMeter : MonoBehaviour
{
    public Rigidbody target;

    [Header("float valuet")]
    public float maxSpeed = 240.0f;
    public float minSpeedArrowAngle = 20.0f;
    public float maxSpeedArrowAngle = -200.0f;

    private float speed;

    [Header("UI")] //kiitos leo's leikkimaa, very cool
    public RectTransform arrow;

    private void Start()
    {
        target = GameManager.instance.CurrentCar.GetComponentInChildren<Rigidbody>();
    }

    private void Update()
    {
        speed = target.linearVelocity.magnitude * 3.6f;
        if (arrow != null) arrow.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, speed / maxSpeed));
    }
}
