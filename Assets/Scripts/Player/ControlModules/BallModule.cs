using System;
using UnityEngine;

public class BallModule : ControlModule
{
    
    [SerializeField] private float _jumpImpulseMagnitude;
    [SerializeField] private float _sprintImpulseMagnitude;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Player player;
    

    public void OnEnable()
    {
        Mock_InputController.OnJumpInput += Input_JumpImpulse;
        Mock_InputController.OnSprintInput += Input_SprintImpulse;
    }

    public void OnDisable()
    {
        Mock_InputController.OnJumpInput -= Input_JumpImpulse;
        Mock_InputController.OnSprintInput -= Input_SprintImpulse;
    }

    private void Input_JumpImpulse()
    {
        if(player.IsGrounded())
            rb.AddForce(Vector3.up * _jumpImpulseMagnitude, ForceMode.Impulse);
    }
    
    private void Input_SprintImpulse(Vector3 direction)
    {
        if (player.IsGrounded())
        {
            //Debug.Log("Firing sprint impulse");
            rb.AddForce(direction * _sprintImpulseMagnitude, ForceMode.Impulse);
        }
    }
}
