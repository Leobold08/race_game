using PurrNet;
using UnityEngine;
using UnityEngine.UI;

public class SpeedMeter : NetworkBehaviour
{
    public Rigidbody target;

    [Header("float valuet")]
    public float maxSpeed = 0.0f;

    public float minSpeedArrowAngle;
    public float maxSpeedArrowAngle;

    private float speed;

    [Header("UI")] //kiitos leo's leikkimaa, very cool
    public Text speedLabel;//Leo our favourite gremlin
    public RectTransform arrow;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        enabled = isOwner;
    }

    private void Start()
    {
        if (!isOwner) return;
        
        if (speedLabel == null)
        {
            Debug.LogWarning("speedLabel EI OLE VITTU OLEMASSA");
        }

        target = GameManager.instance.CurrentCar.GetComponentInChildren<Rigidbody>();
    }

    private void Update()
    {
        speed = target.linearVelocity.magnitude * 3.6f;

        if (speedLabel != null)
        {
            speedLabel.text = ((int)speed) + " km/h";
        }

        if (arrow != null)
        {
            arrow.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, speed / maxSpeed));
        }
    }
}
