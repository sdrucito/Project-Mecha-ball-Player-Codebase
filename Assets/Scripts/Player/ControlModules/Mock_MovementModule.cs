using System;
using UnityEngine;

public class Mock_MovementModule : ControlModule
{
    
    [SerializeField] private float movementSpeed = 5f;
    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        Mock_InputController.OnMovementInput += Input_Apply2DMovement;
    }

    private void OnDisable()
    {
        Mock_InputController.OnMovementInput -= Input_Apply2DMovement;
    }

    void Input_Apply2DMovement(Vector3 direction)
    {
        direction.y = 0;
        rb.AddForce(direction*movementSpeed, ForceMode.VelocityChange);
    }
}
