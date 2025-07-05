using System;
using System.Collections;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.ControlModules
{
    public class BallModule : ControlModule, IUpdateObserver
    {
    
        [SerializeField] private float jumpImpulseMagnitude;
        [SerializeField] private float sprintImpulseMagnitude;
        [SerializeField] private float sprintCooldownTime;
        [SerializeField] private float maxSpeed = 15f;
        
        private Rigidbody _rigidbody;
        private PhysicsModule _physicsModule;
        [SerializeField] private float OverrideLinearDrag;
        [SerializeField] private float OverrideAngularDrag;
            
        [Header("Jump Crosshair")]
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private float maxDistance = 100f;
        private LineRenderer lineRenderer;
        [SerializeField] private GameObject jumpCrosshair;
        private GameObject activeJumpCrossahair;
        [SerializeField] private float minJumpCrosshairHeight = 2.0f;
        
        [Header("Debug")] public bool CanJumpInfinite = false;
        [Header("Debug")] public bool CanSprintInfinite = false;
        private bool _canSprint = true;
        private float _runningSprintCooldown = 0.0f;
        
        public int UpdatePriority { get; set; }
        
        private void Awake()
        {
            name = "Ball";
            UpdatePriority = 0;
        }

        private void Start()
        {
            _rigidbody = Player.Instance.Rigidbody;
            _physicsModule = Player.Instance.PhysicsModule;
        }

        public void ObservedUpdate()
        {
            Player.Instance.PlayerVFX.UpdateTrailRenders(_physicsModule.GetVelocity().magnitude);
            JumpCrosshairLogic();        
        }

        public void OnEnable()
        {
            UpdateManager.Instance.Register(this);
            PlayerInputManager.Instance.OnJumpInput += Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput += Input_SprintImpulse;
            PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            if (!_rigidbody) _rigidbody = Player.Instance.Rigidbody;
            _rigidbody.linearDamping = OverrideLinearDrag;
            _rigidbody.angularDamping = OverrideAngularDrag;
            _rigidbody.WakeUp();
            Player.Instance.PhysicsModule.InjectGroundLayer();
            
            if (lineRenderer == null)
                lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.enabled = false;
        }

        public void OnDisable()
        {
            UpdateManager.Instance?.Unregister(this);
            if (PlayerInputManager.TryGetInstance() == null) return;
            PlayerInputManager.Instance.OnJumpInput -= Input_JumpImpulse;
            PlayerInputManager.Instance.OnSprintImpulseInput -= Input_SprintImpulse;

        }

        public void OnDestroy()
        {
            if (PlayerInputManager.TryGetInstance() == null) return;
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

        private void JumpCrosshairLogic()
        {
            bool isJumping = !Player.Instance.IsGrounded();

            if (isJumping)
            {
                Ray ray = new Ray(transform.position, Vector3.down);

                if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask) && hit.distance > minJumpCrosshairHeight)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.SetPosition(0, transform.position);
                    lineRenderer.SetPosition(1, hit.point);
                    
                    if (activeJumpCrossahair == null)
                    {
                        activeJumpCrossahair = Instantiate(jumpCrosshair);
                    }
                    Vector3 circlePos = hit.point + Vector3.up * 0.01f;
                    activeJumpCrossahair.transform.position = circlePos;
                    activeJumpCrossahair.transform.rotation = Quaternion.LookRotation(hit.normal);
                }
                else
                {
                    lineRenderer.enabled = false;
                    if (activeJumpCrossahair != null)
                    {
                        Destroy(activeJumpCrossahair);
                        activeJumpCrossahair = null;
                    }
                }
            }
            else
            {
                lineRenderer.enabled = false;
                if (activeJumpCrossahair != null)
                {
                    Destroy(activeJumpCrossahair);
                    activeJumpCrossahair = null;
                }
            }
            lineRenderer.material.mainTextureOffset += new Vector2(Time.deltaTime * 2, Time.deltaTime * 2);
            
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
