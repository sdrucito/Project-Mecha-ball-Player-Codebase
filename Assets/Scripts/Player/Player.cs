using System;
using Player.Animation;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [RequireComponent(typeof(Rigidbody),typeof(PawnAttributes))]
    public class Player : Singleton<Player>, IDamageable
    {
        
        public Action OnPlayerDeath;
        
        private PhysicsModule _physicsModule;

        public PawnAttributes PawnAttributes { get; private set; }

        [field: SerializeField] public ControlModuleManager ControlModuleManager { get; private set; }
        [field: SerializeField] public Rigidbody Rigidbody { get; private set; }
        [field: SerializeField] public RaycastManager RaycastManager { get; private set; }

        [field: SerializeField] public PhysicsModule PhysicsModule { get; private set; }

        [field: SerializeField] public PlayerSound PlayerSound { get; private set; }
        [field: SerializeField] public PlayerAnimator PlayerAnimator { get; private set; }
        [field: SerializeField] public PlayerVFX PlayerVFX { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            _physicsModule = GetComponent<PhysicsModule>();
            PawnAttributes = GetComponent<PawnAttributes>();
        }

        private void Start()
        {
            InitializePlayer();
        }

      

        private void InitializePlayer()
        {
            PawnAttributes.ResetMaxHealth();
        }
        private void OnCollisionEnter(Collision other)
        {
            // Create collision data wrapper
            CollisionData collisionData = new CollisionData(other, other.gameObject.layer, other.gameObject.tag, Rigidbody.linearVelocity.magnitude);
            _physicsModule.OnEnterPhysicsUpdate(collisionData);
        }

        private void OnCollisionExit(Collision other)
        {
            // Create collision data wrapper
            CollisionData collisionData = new CollisionData(other, other.gameObject.layer, other.gameObject.tag,  Rigidbody.linearVelocity.magnitude);
            _physicsModule.OnExitPhysicsUpdate(collisionData);    
        }

        public bool IsGrounded()
        {
            return _physicsModule.IsGrounded();
        }

        public bool CanMove(Vector3 movement)
        {
            return _physicsModule.CanMove(movement);
        }
        
        public Vector3 GetGroundNormal()
        {
            return _physicsModule.GetGroundNormal();
        }

        public void UpdateWhitenScaleForLegs(Vector2 whitenScale)
        {
            _physicsModule.UpdateWhitenScale(whitenScale);
        }

        public void SetMovementEnabled(bool movementEnabled)
        {
            //ControlModuleManager.SetModuleEnabled(movementEnabled);
            ControlModuleManager.GetModule(ControlModuleManager.GetActiveModuleName()).IsActive = movementEnabled;
            PlayerInputManager.Instance.SetInputEnabled(movementEnabled);
        }

        public void TakeDamage(float damage)
        {
            if (!PawnAttributes.IsDead)
            {
                PawnAttributes.TakeDamage(damage);
                PlayerAnimator.TakeDamage();
            }
        }
        
    }
}
