using System;
using UnityEngine;

public class WalkingModule : ControlModule
{
    [SerializeField] private float WalkingSpeed = 5f;
    [SerializeField] private float Gravity = -9.8f;

    private CharacterController _controller;
    private Vector2 _inputVector = Vector2.zero;
    
    [SerializeField] private Player player;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _controller = player.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        PlayerInputManager.Instance.OnMoveInput += HandleMovement;
        player.GetComponent<CharacterController>().enabled = true;
    }

    private void OnDisable()
    {
        //if (PlayerInputManager.Instance != null) TODO ADD this check
        PlayerInputManager.Instance.OnMoveInput -= HandleMovement;
        player.GetComponent<CharacterController>().enabled = false;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        ExecuteMovement();
    }

    private void ExecuteMovement()
    {
        if (player.IsGrounded())
        {
            Vector3 horizontalMove = new Vector3(_inputVector.x, 0, _inputVector.y).normalized;
            _controller.Move(horizontalMove* WalkingSpeed * Time.fixedDeltaTime);
        }
    }
    
    private void HandleMovement(Vector2 input){
        _inputVector = input;
    }
}
