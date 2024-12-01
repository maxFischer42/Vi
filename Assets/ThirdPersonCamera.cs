using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform playerObj;

    public Rigidbody rb;

    public float rotationSpeed;

    [Header("Camera")]
    public ProjectileController projectileController;
    public PlayerMovement playerMovement;
    public GameObject primaryCamera;
    public GameObject combatCamera;
    public GameObject topDownCamera;

    public CameraStyle cameraStyle;
    public Transform aimLookAt;
    public enum CameraStyle
    {
        Basic,
        Combat,
        TopDown
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible= false;
    }

    private void Update()
    {
        // rotate orientation
        Vector3 viewDir = player.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
        orientation.forward = viewDir.normalized;

        // rotate player object
        if (cameraStyle == CameraStyle.Basic)
        {
            if (playerMovement.isGrounded())
            {
                float horizontalInput = Input.GetAxis("Horizontal");
                float verticalInput = Input.GetAxis("Vertical");
                Vector3 inputDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

                if (inputDirection != Vector3.zero)
                {
                    playerObj.forward = Vector3.Slerp(playerObj.forward, inputDirection.normalized, Time.deltaTime * rotationSpeed);
                }
            }
        }
        else if (cameraStyle == CameraStyle.Combat)
        {
            Vector3 dirToCombatLookAt = aimLookAt.position - new Vector3(transform.position.x, player.position.y, transform.position.z);
            orientation.forward = dirToCombatLookAt.normalized;
            playerObj.forward = dirToCombatLookAt.normalized;
        }
    }

    public void SwitchCameraStyle(CameraStyle newStyle)
    {
        combatCamera.SetActive(false);
        primaryCamera.SetActive(false);
        topDownCamera.SetActive(false);


        switch(newStyle)
        {
            case CameraStyle.Basic:
                primaryCamera.SetActive(true);
                break;
            case CameraStyle.Combat:
                combatCamera.SetActive(true);
                break;
            case CameraStyle.TopDown:
                topDownCamera.SetActive(true);
                break;
        }
        cameraStyle = newStyle;
    } 
}
