using UnityEngine;
using Fusion;

public class PilotCamera : NetworkBehaviour
{
    public float minX = -60f;
    public float maxX = 60f;

    [Networked]
    public float sensitivity { get; set; }
    public Camera cam;
    public GameObject cameraGameObject;

    public Vector2 look;

    private float rotY;

    private float rotX;

    PilotMovement move;

    public float sprintBobSpeed;
    public float runBobSpeed;
    public float sprintBobAmount;
    public float runBobAmount;
    float defaultY;
    private float timer;

    private void Awake()
    {
        move = GetComponentInParent<PilotMovement>();
    }

    void Start()
    {
        cameraGameObject = cam.gameObject;
        if (!HasInputAuthority)
        {
            cam.enabled = false;
            if (cameraGameObject.TryGetComponent(out AudioListener audioListener))
            {
                audioListener.enabled = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (transform == null)
            return;

        defaultY = cam.transform.localPosition.y;
    }
    
    void Update()
    {
        if (!HasStateAuthority)
            return;
        
        if (move.canMove == false)
            return;

        rotY += look.x;
        rotX += look.y;

        rotX = Mathf.Clamp(rotX, minX, maxX);

        transform.localEulerAngles = new Vector3(0, rotY, 0);
        cameraGameObject.transform.localEulerAngles = new Vector3(-rotX, 0, move.tilt);

        HandleHeadBob();
    }

    void HandleHeadBob()
    {
        if (move.isMoving && move.isGrounded && !move.isSliding)
        {
            timer += Time.deltaTime * (move.isSprinting ? sprintBobSpeed : runBobSpeed);
            Transform camTransform = cameraGameObject.transform;
            camTransform.localPosition = new Vector3(camTransform.localPosition.x, defaultY + Mathf.Sin(timer) * (move.isSprinting ? sprintBobAmount : runBobAmount), camTransform.localPosition.z);
        }
    }

}
