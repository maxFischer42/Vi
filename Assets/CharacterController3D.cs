using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController3D : MonoBehaviour
{
    CharacterController controller;

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float jumpSpeed = 1f;
    [SerializeField] private float gravityMultiplier = 1.75f;
    [SerializeField] private float jumpGracePeriod = 0.15f;

    private float ySpeed;
    private float originalStepOffset;
    private float? lastGroundedTime;
    private float? jumpButtonPressedTime;


    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        originalStepOffset = controller.stepOffset;
    }


    // Update is called once per frame
    void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 movementDirection = new Vector3(horizontalInput, 0, verticalInput);
        float magnitude = Mathf.Clamp01(movementDirection.magnitude) * moveSpeed;
        movementDirection.Normalize();

        ySpeed += Physics.gravity.y * Time.deltaTime * gravityMultiplier;

        if(controller.isGrounded)
        {
            lastGroundedTime= Time.time;
        }
        if(Input.GetButtonDown("Jump"))
        {
            jumpButtonPressedTime = Time.time;
        }

        if(Time.time - lastGroundedTime <= jumpGracePeriod)
        {
            controller.stepOffset = originalStepOffset;
            ySpeed = -0.5f;
            if(Time.time - jumpButtonPressedTime <= jumpGracePeriod)
            {
                ySpeed = jumpSpeed;
                jumpButtonPressedTime = null;
                lastGroundedTime = null;
            }
        } else
        {
            controller.stepOffset = 0;
        }
        Vector3 velocity = movementDirection * magnitude;
        velocity.y = ySpeed;
        controller.Move(velocity * Time.deltaTime);       
        if(movementDirection != Vector3.zero)
        {
            Quaternion toRot = Quaternion.LookRotation(movementDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, toRot, rotationSpeed * Time.deltaTime);
        }
    }
}
