using System;
using System.Collections;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.ControlModules
{
    public class BallModule : ControlModule
    {
    
        [SerializeField] private float jumpImpulseMagnitude;
        [SerializeField] private float sprintImpulseMagnitude;

        private void Awake()
        {
            name = "Ball";
        }
        

        public void OnEnable()
        {
            PlayerInputManager.Instance.OnJumpInput += Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput += Input_SprintImpulse;
            if (Player.Instance)
            {
                Player.Instance.Rigidbody.isKinematic = false;
            }
        }

        public void OnDisable()
        {
            PlayerInputManager.Instance.OnJumpInput -= Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput -= Input_SprintImpulse;

        }

        private void Input_JumpImpulse()
        {
            if(Player.Instance.IsGrounded())
                Player.Instance.Rigidbody.AddForce(Vector3.up * jumpImpulseMagnitude, ForceMode.Impulse);
        }
    
        private void Input_SprintImpulse(Vector2 direction)
        {
            if (Player.Instance.IsGrounded())
            {
                //Debug.Log("Firing sprint impulse");
                Player.Instance.Rigidbody.AddForce(new Vector3(direction.x,0,direction.y) * sprintImpulseMagnitude, ForceMode.Impulse);
            }
        }
    }
}
