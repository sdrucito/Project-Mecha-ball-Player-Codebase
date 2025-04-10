using System;
using UnityEngine;

public class BallModule : ControlModule
{
    
    [SerializeField] private float _jumpImpulseMagnitude;
    [SerializeField] private float _sprintImpulseMagnitude;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

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
        rb.AddForce(Vector3.up * _jumpImpulseMagnitude, ForceMode.Impulse);
    }
    
    private void Input_SprintImpulse(Vector3 direction)
    {
        rb.AddForce(Vector3.forward * _sprintImpulseMagnitude, ForceMode.Impulse);
    }
}
