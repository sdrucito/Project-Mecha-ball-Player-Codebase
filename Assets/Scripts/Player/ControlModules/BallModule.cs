using System;
using System.Collections;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.ControlModules
{
    public class BallModule : ControlModule, IFixedUpdateObserver
    {
    
        [SerializeField] private float jumpImpulseMagnitude;
        [SerializeField] private float sprintImpulseMagnitude;
        [SerializeField] private float sprintCooldownTime;
        [SerializeField] private float maxSpeed = 15f;
        
        private Rigidbody _rigidbody;
        private PhysicsModule _physicsModule;
        [SerializeField] private float OverrideLinearDrag;
        [SerializeField] private float OverrideAngularDrag;

        [Header("Debug")] public bool CanJumpInfinite = false;
        [Header("Debug")] public bool CanSprintInfinite = false;
        private bool _canSprint = true;
        private float _runningSprintCooldown = 0.0f;
        
        public int FixedUpdatePriority { get; set; }

        
        private void Awake()
        {
            name = "Ball";
        }

        private void Start()
        {
            _rigidbody = Player.Instance.Rigidbody;
            _physicsModule = Player.Instance.PhysicsModule;
        }

        public void ObservedFixedUpdate()
        {
            if (_physicsModule == null)
            {
                _physicsModule = Player.Instance.PhysicsModule;
            }
            _physicsModule.CastGroundRollback();
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
            if (player.IsGrounded() || CanJumpInfinite)
            {
                player.Rigidbody.AddForce(Vector3.up * jumpImpulseMagnitude, ForceMode.Impulse);
                player.PlayerSound.Jump();
            }
        }
    
        private void Input_SprintImpulse(Vector2 direction)
        {
            Player player = Player.Instance;
            
            if (CanSprint(direction, player) || CanSprintInfinite)
            {
                //Debug.Log("Firing sprint impulse"+direction);
                if (Player.Instance.PhysicsModule.GetVelocity().magnitude < maxSpeed)
                    player.Rigidbody.AddForce(new Vector3(direction.x,0,direction.y) * sprintImpulseMagnitude, ForceMode.Impulse);
                StartCoroutine(SprintCoroutine());
                player.PlayerSound.Sprint();
            }
        }

        private bool CanSprint(Vector2 direction, Player player)
        {
            return player.IsGrounded() && _canSprint && player.PlayerState != PlayerState.Dead && direction.magnitude > 0.05f;
        }

        private IEnumerator SprintCoroutine()
        {
            _canSprint = false;
            while (_runningSprintCooldown - sprintCooldownTime < Single.Epsilon)
            {
                yield return null;
                GameManager.Instance.UIManager.HudUI.SetImpulseCharge(_runningSprintCooldown/sprintCooldownTime);
                _runningSprintCooldown += Time.deltaTime;
            }
            GameManager.Instance.UIManager.HudUI.SetImpulseCharge(1.0f);

            _runningSprintCooldown = 0.0f;
            _canSprint = true;
        }
    }
}
