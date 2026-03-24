using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
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



    private void Start()
    {
        Cam = GetComponent<Camera>();
        carController = GameManager.instance.CurrentCar.GetComponentInChildren<PlayerCarController>();
    }

    private void Update()
    {
        FollowTarget();
    }

    void FollowTarget()
    {
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
