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
            Player player = Player.Instance;
            if (player.IsGrounded())
            {
                player.Rigidbody.AddForce(Vector3.up * jumpImpulseMagnitude, ForceMode.Impulse);
                player.PlayerSound.Jump();
            }
        }
    
        private void Input_SprintImpulse(Vector2 direction)
        {
            Player player = Player.Instance;
            if (player.IsGrounded() && _canSprint)
            {
                //Debug.Log("Firing sprint impulse");
                player.Rigidbody.AddForce(new Vector3(direction.x,0,direction.y) * sprintImpulseMagnitude, ForceMode.Impulse);
                StartCoroutine(SprintCoroutine());
                player.PlayerSound.Sprint();
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
