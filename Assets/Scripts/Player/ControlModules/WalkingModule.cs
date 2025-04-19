using System;
using System.Numerics;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class WalkingModule : ControlModule
{
    [SerializeField] private float WalkingSpeed = 5f;
    [SerializeField] private float Gravity = -9.8f;
    private float _verticalVelocity=0;

    private CharacterController _controller;
    private Vector2 _inputVector = Vector2.zero;
    
    [SerializeField] private Player player;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        name = "Walk";
        _controller = player.GetComponent<CharacterController>();
        
    }

    private void OnEnable()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.OnMoveInput += HandleMovement;
            player.GetComponent<CharacterController>().enabled = true;
        }
    }

    private void OnDisable()
    {
        if (PlayerInputManager.Instance != null)
        {
            PlayerInputManager.Instance.OnMoveInput -= HandleMovement;
            player.GetComponent<CharacterController>().enabled = false;
        }

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
            // Rotate the player according to normal
            var groundNormal = player.GetGroundNormal();
            Debug.DrawRay(transform.position, groundNormal, Color.red,3f);
            transform.rotation = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            
            // Calculate the movement
            var horizontalMove = Vector3.zero;
            var projectedMove = Vector3.zero;
            if (Math.Abs(groundNormal.y) < 0.01f){
                projectedMove = new Vector3(_inputVector.x, _inputVector.y, 0).normalized;
            }else{
                horizontalMove = new Vector3(_inputVector.x, 0, _inputVector.y).normalized;
                projectedMove = Vector3.ProjectOnPlane(horizontalMove, groundNormal).normalized;
            }
            var move = projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
            _controller.Move(move);
            
            // Apply the Gravity
            _controller.Move(-groundNormal * 0.1f);
        }else {
            // TODO to test this part
            _verticalVelocity += Gravity * Time.fixedDeltaTime;
            Vector3 fall = new Vector3(0f, _verticalVelocity, 0f);
            _controller.Move(fall * Time.fixedDeltaTime);
        }
    }
    
    private void HandleMovement(Vector2 input){
        _inputVector = input;
    }
}
