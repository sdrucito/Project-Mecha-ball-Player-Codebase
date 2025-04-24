using System;
using UnityEngine;

public class BallModule : ControlModule
{
    
    [SerializeField] private float _jumpImpulseMagnitude;
    [SerializeField] private float _sprintImpulseMagnitude;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Player player;

    private void Awake()
    {
        name = "Ball";
    }

    public void OnEnable()
    {
        PlayerInputManager.Instance.OnJumpInput += Input_JumpImpulse;
        rb.isKinematic = false;
    }

    public void OnDisable()
    {
        PlayerInputManager.Instance.OnJumpInput -= Input_JumpImpulse;
        rb.isKinematic = true;

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
            rb.AddForce(direction * _sprintImpulseMagnitude, ForceMode.Impulse);
        }
    }
}
