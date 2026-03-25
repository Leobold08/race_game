using UnityEngine;

public class FollowCarNoRotation : MonoBehaviour
{
    [SerializeField] private Transform target;
    public bool offsetInTargetLocalSpace = false;
    public Vector3 localOffset = new(0f, 3f, -6f);

    void Awake()
    {
        target = GameManager.instance.CurrentCar.transform.Find("car");
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = offsetInTargetLocalSpace ? target.TransformPoint(localOffset) : target.position + localOffset;
        transform.position = desired;
    }
}