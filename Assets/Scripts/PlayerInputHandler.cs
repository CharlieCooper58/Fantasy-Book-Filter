using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


public class PlayerInputHandler : MonoBehaviour
{
    public PlayerControls playerControls;
    PlayerLocomotion playerLocomotion;
    //[Header("Character Input Values")]
    public Vector2 move { get; private set; }
    public Vector2 look { get; private set; }
    public bool jump { get; private set; }
    public bool sprint { get; private set; }
    public bool scan { get; private set; }

    [Header("Movement Settings")]
    public bool analogMovement;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    bool discardFirstLookInput = true;
    private void Awake()
    {
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }
    private void OnEnable()
    {
        SetCursorState(cursorLocked);
        if(playerControls == null)
        {
            playerControls = new PlayerControls();
            playerControls.Enable();
            playerControls.Player.Move.performed += x => move = x.ReadValue<Vector2>();
            playerControls.Player.Look.performed += x => Look() ;
            playerControls.Player.Jump.performed += x => Jump();
            playerControls.Player.Jump.canceled += x => Jump();
            playerControls.Player.Sprint.performed += x => Sprint();
        }
        else
        {
            playerControls.Enable();

        }
        look = Vector2.zero;
    }

    private void Jump()
    {
        jump = playerControls.Player.Jump.IsPressed();
    }
    private void Sprint()
    {
        sprint = playerControls.Player.Sprint.IsPressed();
    }

    private void Look()
    {
        if (discardFirstLookInput)
        {
            discardFirstLookInput = false;
            return;
        }
        look = playerControls.Player.Look.ReadValue<Vector2>();
    }


    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}

