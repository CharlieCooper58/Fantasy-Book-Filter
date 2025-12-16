using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
public class PlayerCamera : MonoBehaviour
{
    PlayerInputHandler playerInputHandler;
    [SerializeField] Transform cameraPivot;
    PlayerManager playerManager;

    [SerializeField] Transform cameraRotationHelper;
    float targetCameraXAxisRotation;

    float cameraXAngularVelocity;
    float cameraYAngularVelocity;

    [SerializeField] float cameraUpperRotationLimit;
    [SerializeField] float cameraLowerRotationLimit;

    public Vector2 cameraInput;

    public Vector3 cameraOffset;

    bool skipFirstframe = true;

    [SerializeField] float cameraSmoothTime;

    bool lockedcursor = true;
    private void Awake()
    {
        targetCameraXAxisRotation = 0;
    }
    public void Initialize()
    {
        playerManager = GetComponent<PlayerManager>();
        playerInputHandler = GetComponent<PlayerInputHandler>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cameraRotationHelper.localRotation = transform.rotation;
        
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            if (lockedcursor)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                lockedcursor = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                lockedcursor = true;
            }
        }
    }

    public void UpdateCameraOrientation()
    {

        if (skipFirstframe)
            {
                skipFirstframe = false;
                return;
            } 
        float lookX = (GameManager.instance.horizontalLookSensitivity * 100) * playerInputHandler.look.x * Time.deltaTime;
        float lookY = (GameManager.instance.verticalLookSensitivity * 100) * playerInputHandler.look.y * Time.deltaTime;

        Vector2 lookVector = new Vector2(lookX, lookY);

        // Adjust the target camera rotation based on input
        targetCameraXAxisRotation = Mathf.Clamp(targetCameraXAxisRotation - lookVector.y, cameraLowerRotationLimit, cameraUpperRotationLimit);
        cameraRotationHelper.Rotate(Vector3.up * lookVector.x);

        // Update the camera pivot and player's transform rotation
        transform.rotation = Quaternion.Euler(0, Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraRotationHelper.localEulerAngles.y, ref cameraYAngularVelocity, cameraSmoothTime), 0);
        cameraPivot.transform.localRotation = Quaternion.Euler(Mathf.SmoothDampAngle(cameraPivot.transform.localEulerAngles.x, targetCameraXAxisRotation, ref cameraXAngularVelocity, cameraSmoothTime), 0, 0);
    }
}