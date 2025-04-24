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
        PlayerInputManager.Instance.OnSprintImpulseInput += Input_SprintImpulse;
        rb.isKinematic = false;
    }

    public void OnDisable()
    {
        PlayerInputManager.Instance.OnJumpInput -= Input_JumpImpulse;
        PlayerInputManager.Instance.OnSprintImpulseInput -= Input_SprintImpulse;
        rb.isKinematic = true;

    }

    private void Input_JumpImpulse()
    {
        if(player.IsGrounded())
            rb.AddForce(Vector3.up * _jumpImpulseMagnitude, ForceMode.Impulse);
    }
    
    private void Input_SprintImpulse(Vector2 direction)
    {
        if (player.IsGrounded())
        {
            //Debug.Log("Firing sprint impulse");
            rb.AddForce(new Vector3(direction.x,0,direction.y) * _sprintImpulseMagnitude, ForceMode.Impulse);
        }
    }
}
