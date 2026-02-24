using UnityEngine;
using PurrNet;

[RequireComponent(typeof(Camera))]
public class CameraFollow : NetworkBehaviour
{
    public float moveSmoothness;
    public float rotSmoothness;


    public Vector3 moveOffset; 
    public Vector3 rotOffset;

    public Transform carTarget;
    
    private Camera Cam;
    private PlayerCarController carController;
    float normalFOV = 60;
    float ZoomFOV = 70;
    public float smoothTime = 0.3f;
    public bool setTutorialValues = false;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        enabled = isOwner;

        if (Cam == null)
            Cam = GetComponent<Camera>();

        if (Cam != null)
            Cam.enabled = isOwner;

        var listener = GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = isOwner;
    }


    private void Start()
    {
        Cam = GetComponent<Camera>();

        if (carTarget == null)
            carTarget = transform.root;

        if (carTarget != null)
            carController = carTarget.GetComponentInChildren<PlayerCarController>();

        if (carController == null && GameManager.instance != null && GameManager.instance.CurrentCar != null)
            carController = GameManager.instance.CurrentCar.GetComponentInChildren<PlayerCarController>();
    }

    private void LateUpdate()
    {
        if (!isOwner) return;
        FollowTarget();
    }

    void FollowTarget()
    {
        if (carTarget == null || carController == null) return;

        HandleMovement();
        HandleRotation();
        Cam.fieldOfView = Mathf.Lerp(
            Cam.fieldOfView, 
            Mathf.Lerp(
                normalFOV, 
                ZoomFOV, 
                Mathf.Clamp01(carController.GetSpeed() / carController.GetMaxSpeed())
            ),
            Time.deltaTime * moveSmoothness
        );
    }

    void HandleMovement()
    {
        Vector3 targetPos = carTarget.TransformPoint(moveOffset); 
    
        transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * Time.deltaTime);

    }
    void HandleRotation()
    {
        var direction = carTarget.position - transform.position;
        var rotation = Quaternion.LookRotation(direction + rotOffset, Vector3.up);
        transform.rotation = rotation;
    }
}
