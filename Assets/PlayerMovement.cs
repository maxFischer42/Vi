using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Reference")]
    public ThirdPersonCamera cameraController;
    public ProjectileController projectileController;
    
    [Header("Movement")]
    private float moveSpeed;
    public float groundDrag;
    public float sneakSpeed;
    public float walkSpeed;
    public float sprintSpeed;

    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sneakKey = KeyCode.C;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public KeyCode aimKey = KeyCode.Mouse4;
    public KeyCode spinKey = KeyCode.Mouse0;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask groundLayers;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Spin")]
    public float spinCooldown;
    private bool canSpin;


    float horizontalInput;
    float verticalInput;

    bool isAiming = false;
    bool canAirSpin = false;


    public Transform orientation;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    private CapsuleCollider mainCollider;

    public enum MovementState
    {
        walking,
        crouching,
        sneaking,
        sprinting,
        aim,
        air
    }


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;
        canSpin = true;
        mainCollider = GetComponent<CapsuleCollider>();
        playerHeight = mainCollider.height;
        startYScale = playerHeight;
    }

    private void Update()
    {
        HandleGroundCheck();
        MyInput();
        SpeedControl();
        MovementStateHandler();
        HandleGroundDrag();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(aimKey) && grounded)
        {
            isAiming = true;
            cameraController.SwitchCameraStyle(ThirdPersonCamera.CameraStyle.Combat);
        }

        if (Input.GetKeyUp(aimKey))
        {
            isAiming = false;
            cameraController.SwitchCameraStyle(ThirdPersonCamera.CameraStyle.Basic);
        }



        // When to jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }

        // Start Crouch
        if(Input.GetKeyDown(crouchKey))
        {
            mainCollider.height = crouchYScale;
            mainCollider.center = new Vector3(0, crouchYScale / 2f, 0);
            rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);            
        }

        // Stop Crouch
        if(Input.GetKeyUp(crouchKey)) {
            mainCollider.height = startYScale;
            mainCollider.center = Vector3.zero;
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
        }

        // Spin
        if(Input.GetKeyDown(spinKey) && (state != MovementState.aim && state != MovementState.crouching) && canAirSpin)
        {
            DoSpin();
            Invoke(nameof(ResetSpin), spinCooldown);
        }


        
    }

    private void MovePlayer()
    {
        if (isAiming) return;
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;


        bool isOnSlope = OnSlope();
    
        // On Slope
        if(isOnSlope && !exitingSlope)
        {
            rb.AddForce(GetSlopeMoveDirection() * moveSpeed * 20f, ForceMode.Force);
            if(rb.velocity.y > 0)
            {
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
            }
        }

        // If grounded
        if (grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }
        else if (!grounded)
        {
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        // Turn gravity off when on slope
        rb.useGravity = !isOnSlope;

    }

    private void HandleGroundCheck()
    {
        // Ground Check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayers);
    }

    private void HandleGroundDrag()
    {
        if(grounded)
        {
            rb.drag = groundDrag;
        } else
        {
            rb.drag = 0f;
        }
    }

    private void SpeedControl()
    {
        // limiting speed on a slope
        if (OnSlope() && !exitingSlope)
        {
            if(rb.velocity.magnitude > moveSpeed)
            {
                rb.velocity = rb.velocity.normalized * moveSpeed;
            }
        }
        else
        {
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // Limit velocity if needed
            if (flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        if (isAiming) return;
        exitingSlope = true;
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.y);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void SpinJump()
    {
        if (isAiming) return;
        exitingSlope = true;
        rb.velocity = new Vector3(0f, 0f, 0f);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        exitingSlope = false;
        readyToJump = true;
        canAirSpin = true;
    }

    private void MovementStateHandler()
    {
        // Mode - Crouching
        if(grounded && Input.GetKey(crouchKey)) {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
        }
        // Mode - Sprinting
        else if(grounded && Input.GetKey(sprintKey))
        {
            state = MovementState.sprinting;
            moveSpeed = sprintSpeed;
        }
        // Mode - Sneaking
        else if(grounded && Input.GetKey(sneakKey))
        {
            state = MovementState.sneaking;
            moveSpeed = sneakSpeed;
        }
        // Mode - Walking
        else if(grounded)
        {
            state = MovementState.walking;
            moveSpeed = walkSpeed;
        }
        // Mode - Aim
        else if(grounded && Input.GetKey(aimKey)) {
            state = MovementState.aim;
            moveSpeed = 0f;
        }
        // Mode - Air
        else
        {
            state = MovementState.air;
        }
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, mainCollider.height * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
    }

    private void DoSpin()
    {
        canSpin = false;
        Debug.Log("Spin!");

        if(!grounded)
        {
            SpinJump();
            canAirSpin = false;
        }
    }

    private void ResetSpin()
    {
        canSpin = true;
    }
}
