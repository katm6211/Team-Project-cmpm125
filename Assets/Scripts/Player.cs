using UnityEngine;

public class Player : MonoBehaviour
{
    public Transform cameraTransform;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    public float gravity = -20f;
    public float mouseSensitivity = 2f;

    CharacterController controller;
    float verticalVelocity;
    float cameraPitch;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        //cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        cameraPitch = Mathf.Clamp(cameraPitch - mouseY, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // Movement
        Vector3 move = transform.right * Input.GetAxis("Horizontal")
                     + transform.forward * Input.GetAxis("Vertical");
        move = Vector3.ClampMagnitude(move, 1f) * moveSpeed;

        // Jump and gravity
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (controller.isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
        move.y = verticalVelocity;

        controller.Move(move * Time.deltaTime);
    }
}
