using System;
using System.Collections;
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

      

        private IEnumerator InitializePlayer()
        {
            _pawnAttributes.ResetMaxHealth();
            // Switch to Ball mode
            if (ControlModuleManager.GetActiveModuleName() == "Walk")
            {
                yield return new WaitForSeconds(0.2f);
                ControlModuleManager.SwitchMode();
                yield return new WaitForSeconds(3f);
                ControlModuleManager.SwitchMode();
                PlayerInputManager.Instance.SetInputEnabled(true);
            }
            else
            {
                ControlModuleManager.SwitchMode();
                PlayerInputManager.Instance.SetInputEnabled(true);
            }
            
        }

        public void SpawnPlayer(Transform newPosition)
        {
            // Block player input during spawn
            PlayerInputManager.Instance.SetInputEnabled(false);
            
            // Use here the function on the other branch for player repositioning
            PhysicsModule.Reposition(newPosition.position, newPosition.rotation);
            // Here the player should play something like spawn animations, sounds ecc.
            StartCoroutine(InitializePlayer());
            // TODO: Play spawn animation
            // TODO: Play spawn SFX and/or VFX
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

        public void Die()
        {
            OnPlayerDeath?.Invoke();
        } 
        public void TakeDamage(float damage)
        {
            // TODO: Call here SFX and VFX for damage taken
            _pawnAttributes.TakeDamage(damage);
            PlayerSound.TakeDamage();
        }
        
    }
}
