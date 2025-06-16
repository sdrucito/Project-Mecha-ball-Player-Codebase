using System;
using Player.Animation;
using System.Collections;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    public enum PlayerState
    {
        Unoccupied,
        Hit,
        Dead
    }
    
    
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

        
        public PlayerState PlayerState { get; private set; }
        protected override void Awake()
        {
            base.Awake();
            _physicsModule = GetComponent<PhysicsModule>();
            PawnAttributes = GetComponent<PawnAttributes>();
            PlayerState = PlayerState.Unoccupied;
        }

        private void Start()
        {
            StartCoroutine(InitializePlayer());
        }
        
        private IEnumerator InitializePlayer()
        {
            PawnAttributes.InitAttributes();
            PlayerState = PlayerState.Unoccupied;
            // Reset Animator
            PlayerAnimator.Rebirth();
            // Switch to Ball mode
            if (ControlModuleManager.GetActiveModuleName() == "Walk")
            {
                yield return new WaitForSeconds(1f);
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
            PlayerAnimator.Initialize();
            // Here the player should play something like spawn animations, sounds ecc.
            StartCoroutine(InitializePlayer());
        }
        
        private void OnCollisionEnter(Collision other)
        {
            // Create collision data wrapper
            CollisionData collisionData = new CollisionData(other, other.gameObject.layer, other.gameObject.tag, Rigidbody.linearVelocity.magnitude);
            _physicsModule.OnEnterPhysicsUpdate(collisionData);
            /*if (collisionData.Tag == "Ground")
            {
                CameraShake.Instance.Shake("BallLanding");
            }
            else
            {
                CameraShake.Instance.Shake("BounceShake");
            }*/
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
            if (!PawnAttributes.IsDead)
            {
                PawnAttributes.TakeDamage(damage);
                if (PlayerState != PlayerState.Hit)
                {
                    PlayerState = PlayerState.Hit;
                    PlayerAnimator.TakeDamage();
                }
                PlayerSound.TakeDamage();
                PlayerVFX.TakeDamage();
                CameraShake.Instance.Shake("Damage");
                HapticsManager.Instance.Play("Damage");
            }
            else
            {
                PlayerState = PlayerState.Dead;
            }
        }

        public void SetPlayerState(PlayerState newState)
        {
            PlayerState = newState;
        }
        
    }
}
