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
        [SerializeField] private float sprintCooldownTime;
        
        private Rigidbody _rigidbody;
        [SerializeField] private float OverrideLinearDrag;
        [SerializeField] private float OverrideAngularDrag;
        
        private bool _canSprint = true;
        private void Awake()
        {
            name = "Ball";
        }

        private void Start()
        {
            _rigidbody = Player.Instance.Rigidbody;
        }

        public void OnEnable()
        {
            PlayerInputManager.Instance.OnJumpInput += Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput += Input_SprintImpulse;
            PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            if (!_rigidbody) _rigidbody = Player.Instance.Rigidbody;
            _rigidbody.linearDamping = OverrideLinearDrag;
            _rigidbody.angularDamping = OverrideAngularDrag;
            _rigidbody.WakeUp();
            Player.Instance.PhysicsModule.InjectGroundLayer();
        }

        public void OnDisable()
        {
            PlayerInputManager.Instance.OnJumpInput -= Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput -= Input_SprintImpulse;
        }

        private void Input_JumpImpulse()
        {
            if(Player.Instance.IsGrounded() && Player.Instance.PlayerState != PlayerState.Dead)
                Player.Instance.Rigidbody.AddForce(Vector3.up * jumpImpulseMagnitude, ForceMode.Impulse);
        }
    
        private void Input_SprintImpulse(Vector2 direction)
        {
            if (Player.Instance.IsGrounded() && _canSprint && Player.Instance.PlayerState != PlayerState.Dead)
            {
                //Debug.Log("Firing sprint impulse");
                Player.Instance.Rigidbody.AddForce(new Vector3(direction.x,0,direction.y) * sprintImpulseMagnitude, ForceMode.Impulse);
                StartCoroutine(SprintCoroutine());
            }
        }

        private IEnumerator SprintCoroutine()
        {
            _canSprint = false;
            yield return new WaitForSeconds(sprintCooldownTime);
            _canSprint = true;
        }
    }
}
