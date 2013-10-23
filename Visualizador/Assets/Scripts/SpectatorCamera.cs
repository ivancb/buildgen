using UnityEngine;
using System.Collections;

/// <summary>
/// Represents a flight camera with no constraints or acceleration motions
/// </summary>
public class SpectatorCamera : MonoBehaviour
{
    private Vector2 MouseMovement;
    private Vector2 TargetDirection;

    public Material AltMaterial = null;

    public Vector2 Sensitivity = new Vector2(5, 5);
    public float MinimumYAngle = -80f;
    public float MaximumYAngle = 80f;
    public float RotationSpeed = 5.0f;
    public float MovementSpeed = 0.4f;


    void Start()
    {
        Screen.lockCursor = true;
        TargetDirection = this.transform.rotation.eulerAngles;
    }

    void Update()
    {
        // Handle mouse look
        if (Screen.lockCursor)
        {
            Quaternion targetOrientation = Quaternion.Euler(TargetDirection);
            Vector2 mouseDelta = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

            mouseDelta.x *= Sensitivity.x;
            mouseDelta.y *= Sensitivity.y;
            MouseMovement += mouseDelta;

            // Apply rotation along the Y axis
            var xRotation = Quaternion.AngleAxis(-MouseMovement.y, targetOrientation * Vector3.right);
            this.transform.localRotation = xRotation;

            // Clamp X rotation so the camera doesn't flip over itself
            MouseMovement.y = Mathf.Clamp(MouseMovement.y, MinimumYAngle, MaximumYAngle);

            // Apply rotation along the X axis
            var yRotation = Quaternion.AngleAxis(MouseMovement.x, this.transform.InverseTransformDirection(Vector3.up));
            this.transform.localRotation *= yRotation;
            this.transform.rotation *= targetOrientation;
        }

        CharacterController controller = GetComponent<CharacterController>();

        // Apply key movement
        if (Input.GetAxis("Forward") > 0f)
            controller.Move(this.transform.TransformDirection(new Vector3(0, 0, 1)) * MovementSpeed);
        if (Input.GetAxis("Back") > 0f)
            controller.Move(this.transform.TransformDirection(new Vector3(0, 0, -1)) * MovementSpeed);
        if (Input.GetAxis("Strafe Left") > 0f)
            controller.Move(this.transform.TransformDirection(new Vector3(-1, 0, 0)) * MovementSpeed);
        if (Input.GetAxis("Strafe Right") > 0f)
            controller.Move(this.transform.TransformDirection(new Vector3(1, 0, 0)) * MovementSpeed);

        // Other key handling
        if (Input.GetButtonUp("Toggle Mouselook"))
        {
            Screen.lockCursor = !Screen.lockCursor;
        }
        if (Input.GetKeyUp("1"))
        {
            this.transform.position = new Vector3(0, 82, -186);
            MouseMovement = new Vector2(0, 0);
        }
        if (Input.GetKeyUp("2"))
        {
            this.transform.position = new Vector3(89, 33, -42);
            MouseMovement = new Vector2(300, 0);
        }
        if (Input.GetKeyUp("r"))
        {
            var buildingObj = GameObject.Find("Building");

            if (AltMaterial != null)
            {
                Material tmp = buildingObj.renderer.material;
                buildingObj.renderer.material = AltMaterial;
                AltMaterial = tmp;
            }
        }
    }
}
