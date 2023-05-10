using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// This script acts as a single point for all other scripts to get
// the current input from. It uses Unity's new Input System and
// functions should be mapped to their corresponding controls
// using a PlayerInput component with Unity Events.

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    private InputAction submit;
    private bool submitPressed;

    public Controls playerControls;

    private bool submitReleased = true;

    private void OnEnable(){
        submit = playerControls.Game.Submit;
        submit.Enable();
        submit.performed += Submit;
    }

    private void onDisable(){
        submit.Disable();
    }

    private void Awake(){
        playerControls = new Controls();  
    }
    private void Submit(InputAction.CallbackContext context){
        if (context.performed){
            submitPressed = true;
        }
        else if (context.canceled){
            submitPressed = false;
        }
    }
    
    public bool GetSubmitPressed(){
        Debug.Log("pRESSED" + submitPressed);
        Debug.Log("relseaed" + submitReleased);
        if (!submitPressed){
            submitReleased = true;
        } 

        if (submitReleased && submitPressed){
            submitReleased = false;
            return true;
        } 
        
        return false;
    }
}