using System;
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
        private PawnAttributes _pawnAttributes;

 
        [field: SerializeField] public ControlModuleManager ControlModuleManager { get; private set; }
        [field: SerializeField] public Rigidbody Rigidbody { get; private set; }
        [field: SerializeField] public RaycastManager RaycastManager { get; private set; }

        [field: SerializeField] public PhysicsModule PhysicsModule { get; private set; }

        [field: SerializeField] public PlayerSound PlayerSound { get; private set; }
        protected override void Awake()
        {
            base.Awake();
            _physicsModule = GetComponent<PhysicsModule>();
            _pawnAttributes = GetComponent<PawnAttributes>();
        }

        private void Start()
        {
            InitializePlayer();
        }

      

        private void InitializePlayer()
        {
            _pawnAttributes.ResetMaxHealth();
        }
        private void OnCollisionEnter(Collision other)
        {
            // Create collision data wrapper
            CollisionData collisionData = new CollisionData(other, other.gameObject.layer, other.gameObject.tag);
            _physicsModule.OnEnterPhysicsUpdate(collisionData);
        }

        private void OnCollisionExit(Collision other)
        {
            // Create collision data wrapper
            CollisionData collisionData = new CollisionData(other, other.gameObject.layer, other.gameObject.tag);
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
            // TODO: Call here SFX and VFX for damage taken
            _pawnAttributes.TakeDamage(damage);
            PlayerSound.TakeDamage();
        }
        
    }
}
