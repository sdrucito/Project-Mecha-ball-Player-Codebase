using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    [RequireComponent(typeof(Rigidbody),typeof(PlayerAttributes))]
    public class Player : MonoBehaviour
    {

        public static Player Instance;

        private PhysicsModule _physicsModule;
        private PlayerAttributes _playerAttributes;
        [field: SerializeField] public ControlModuleManager ControlModuleManager { get; private set; }
        [field: SerializeField] public Rigidbody Rigidbody { get; private set; }
        [field: SerializeField] public RaycastManager RaycastManager { get; private set; }
        [field: SerializeField] public CharacterController CharacterController { get; private set; }

        [field: SerializeField] public PhysicsModule PhysicsModule { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
            _physicsModule = GetComponent<PhysicsModule>();
            _playerAttributes = GetComponent<PlayerAttributes>();
        }

        private void Start()
        {
        
            InitializePlayer();
        }

      

        private void InitializePlayer()
        {
            _playerAttributes.ResetMaxHealth();
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

        public void SetMovementEnabled(bool movementEnabled)
        {
            //ControlModuleManager.SetModuleEnabled(movementEnabled);
            ControlModuleManager.GetModule(ControlModuleManager.GetActiveModuleName()).IsActive = movementEnabled;
            PlayerInputManager.Instance.SetInputEnabled(movementEnabled);
            CharacterController.enabled = movementEnabled;
        }

        
    }
}
