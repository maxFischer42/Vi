using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

    [Header("Reference")]
    public ThirdPersonCamera cameraController;
    public ProjectileController projectileController;
    public ParticleSystem tailSwingParticles;
    public ParticleSystem dustParticles;

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

    public float coyoteTimeLength = 0.2f;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Header("Spin")]
    public float spinCooldown;
    private bool canSpin;

    [Header("Model Handling")]

    float horizontalInput;
    float verticalInput;

    bool isAiming = false;
    bool canAirSpin = false;

    bool coyoteTime = false;


    public Transform orientation;

    Vector3 moveDirection;

    Rigidbody rb;

    public MovementState state;
    private CapsuleCollider mainCollider;

    public Animator _anim;
    public Animator _shadowAnim;
    public Transform modelParent;
    public float verticalCrouchOffset = 1f;

    private float animator_velocity;
    private bool animator_isMoving;

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

        _anim.SetFloat("velocity", animator_velocity);
        _anim.SetBool("movingAxis", (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0));

        _shadowAnim.SetFloat("velocity", animator_velocity);
        _shadowAnim.SetBool("movingAxis", (Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0));
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
        if (Input.GetKey(jumpKey) && readyToJump && (grounded || coyoteTime))
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
            //modelParent.transform.localPosition = Vector3.Lerp(modelParent.transform.position, Vector3.up * verticalCrouchOffset, Time.timeScale);
            modelParent.transform.localPosition = Vector3.up * verticalCrouchOffset;
        }

        // Stop Crouch
        if(Input.GetKeyUp(crouchKey)) {
            mainCollider.height = startYScale;
            mainCollider.center = Vector3.zero;
            rb.AddForce(Vector3.up * 5f, ForceMode.Impulse);
            //modelParent.transform.localPosition = Vector3.Lerp(Vector3.up * verticalCrouchOffset, modelParent.transform.position, Time.timeScale);
            modelParent.transform.localPosition = Vector3.down;
        }

        // Spin
        if(Input.GetKeyDown(spinKey) && (state != MovementState.aim && state != MovementState.crouching) && (grounded == true || canAirSpin))
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

        animator_isMoving = false;
        float velocity = Mathf.Max(Mathf.Abs(rb.velocity.x), Mathf.Abs(rb.velocity.z));
        //switch(state)
        //{
        //    case MovementState.walking:
        //        animator_velocity = 1f;
        //        animator_isMoving = true;
        //        break;
        //    case MovementState.sprinting:
        //        animator_velocity = 2f;
        //        animator_isMoving = true;
        //        break;
        //    case MovementState.sneaking:
        //        animator_velocity = 0.15f;
        //        animator_isMoving = true;
        //        break;
        //    default:
        //        animator_velocity = 0f;
        //        if(grounded)
        //        {

        //        } else
        //        {

        //        }
        //        break;
        //}
        animator_velocity = velocity;
        // Turn gravity off when on slope
        rb.useGravity = !isOnSlope;

    }

    private void HandleGroundCheck()
    {
        bool previousGroundState = grounded;
        // Ground Check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, groundLayers);
        _anim.SetBool("grounded", grounded);
        _shadowAnim.SetBool("grounded", grounded);
        if(previousGroundState && !grounded) {
            coyoteTime = true;
            Invoke(nameof(HandleCoyoteTime), coyoteTimeLength);
        }
    }

    private void HandleCoyoteTime()
    {
        coyoteTime = false;
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
        if (grounded)
        {
            _anim.SetTrigger("Jump");
            _shadowAnim.SetTrigger("Jump");
        }
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
        _anim.SetBool("isCrouch", false);
        _shadowAnim.SetBool("isCrouch", false);
        // Mode - Crouching
        if (grounded && Input.GetKey(crouchKey)) {
            state = MovementState.crouching;
            moveSpeed = crouchSpeed;
            _anim.SetBool("isCrouch", true);
            _shadowAnim.SetBool("isCrouch", true);
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
        _anim.SetTrigger("Spin");
        _shadowAnim.SetTrigger("Spin");
        tailSwingParticles.Play();
        tailSwingParticles.GetComponentInChildren<ParticleSystem>().Play();
        if (!grounded)
        {
            SpinJump();
            canAirSpin = false;
        }
    }

    private void ResetSpin()
    {
        canSpin = true;
    }

    public void AnimAction_PlayDust()
    {
        dustParticles.Play();
    }

    public bool isGrounded()
    {
        return grounded;
    }

}
