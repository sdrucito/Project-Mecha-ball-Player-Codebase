using System;
using Player.Animation;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Player.ControlModules
{
    public class WalkingModule : ControlModule, IFixedUpdateObserver
    {
        public int FixedUpdatePriority { get; set; }

        
        [SerializeField] private float WalkingSpeed = 5f;
        [SerializeField] private float Gravity = -9.8f;
        [SerializeField] private float RotationSpeedOnSlope = 80f;
        [SerializeField] private float RotationSpeed = 40f;
        [SerializeField] private float ManualRotationMultiplier = 2f;
        private float _verticalVelocity=0;

        private CharacterController _controller;
        private Vector2 _inputVector = Vector2.zero;
        private Vector2 _directionInputVector = Vector2.zero;
        private Quaternion _currentRotation;
            
        [SerializeField] private PlayerKneeWalkAnimator playerWalkAnimator;
        
        private Quaternion _targetRotation = Quaternion.identity;

        private Vector3 _lastFixedMovementDelta;
        private Vector3 _lastFixedMovementApplied;
        private Vector3 _lastPosition;

        private bool _wasBlocked = false;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            name = "Walk";
            FixedUpdatePriority = 0;
        }

        private void Start()
        {
            _controller = Player.Instance.CharacterController;
            playerWalkAnimator.OnOpenFinished += OpenFinished;
            _lastPosition = Player.Instance.Rigidbody.position;
            _lastFixedMovementApplied = Vector3.zero;
            _lastFixedMovementDelta = Vector3.zero;
        }

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput += HandleMovement;
                PlayerInputManager.Instance.OnLookInput += HandleDirection;
                OpenFinished();
                playerWalkAnimator.enabled = true;
                PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            }
        }

        private void OnDisable()
        {
            FixedUpdateManager.Instance?.Unregister(this);
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput -= HandleMovement;
                PlayerInputManager.Instance.OnLookInput -= HandleDirection;
                playerWalkAnimator.enabled = false;
            }
        }

        private void OpenFinished()
        {
            if(_controller)
                _controller.enabled = true;
            
            // Reset movement delta
            _lastFixedMovementDelta = Vector3.zero;
            _lastPosition = Player.Instance.Rigidbody.position;
        }
        private void HandleMovement(Vector2 input){
            _inputVector = input;
        }

        private void HandleDirection(Vector2 input)
        {
            _directionInputVector = input;
        }
        
        // Update is called once per frame
        public void ObservedFixedUpdate()
        {
            // Send to leg animator the difference of movement delta
            _lastFixedMovementDelta = Player.Instance.Rigidbody.position - _lastPosition;
            Vector3 movementDifference = _lastFixedMovementDelta - _lastFixedMovementApplied;
            
            _lastPosition = Player.Instance.Rigidbody.position;
   
            playerWalkAnimator.FollowUserMovement(movementDifference);
            playerWalkAnimator.MovementDelta = _lastFixedMovementApplied;
            
            // Add to RaycastManager the movement applied for leg anticipation
            Player.Instance.RaycastManager.MovementDelta = GetPredictedMovement();
            // Fire ground check after input applied
            playerWalkAnimator.ExecuteGrounded();
            
            // Reset last fixed movement applied
            _lastFixedMovementApplied = Vector3.zero;
            
            // Execute movement function
            ExecuteMovement();
        }

        private Vector3 GetPredictedMovement()
        {
            Vector3 groundNormal = Player.Instance.GetGroundNormal();
            Vector3 projectedMove = ProjectedMove(_inputVector,groundNormal);
            return projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
        }

        #region Movement Logic
        private void ExecuteMovement()
        {
            Vector3 groundNormal = Player.Instance.GetGroundNormal();
            Vector3 projectedMove = ProjectedMove(_inputVector,groundNormal);
            
            
            if (Player.Instance.CanMove(projectedMove) && !_wasBlocked)
            {
                if(_inputVector.magnitude > 0.01f)
                {
                    // Rotate the player according to look or move direction only if it can move
                    if (_directionInputVector.magnitude < 0.01f){ // auto
                        if (projectedMove.magnitude > 0.01f)
                        {
                            _targetRotation = Quaternion.LookRotation(projectedMove, groundNormal);
                            transform.parent.rotation = Quaternion.RotateTowards(transform.parent.rotation, _targetRotation,
                                RotationSpeed * Time.fixedDeltaTime);
                        }
                    } else { //manual
                        Vector3 projectedDirection = ProjectedMove(_directionInputVector, groundNormal);
                        _targetRotation = Quaternion.LookRotation(projectedDirection, groundNormal);
                        transform.parent.rotation = Quaternion.RotateTowards(transform.parent.rotation, _targetRotation,
                            RotationSpeed * Time.fixedDeltaTime * ManualRotationMultiplier);
                    }
                    // Rotate the player according to normal
                    _targetRotation = Quaternion.FromToRotation(transform.parent.up, groundNormal) * transform.parent.rotation;
                    ApplyRotation();
                    //Debug.DrawRay(transform.position, groundNormal, Color.red,3f);
                
                    var moveDirection = projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
                    if(moveDirection != Vector3.zero)
                        _controller.Move(moveDirection);
                
                    // Update the last movement applied by the user
                    _lastFixedMovementApplied = moveDirection;
                }
                

            }
            else
            {
                if(_wasBlocked)
                    _wasBlocked = false;
                else
                    _wasBlocked = true;
            }
            ApplyTouchGrounded();
         
            if (!Player.Instance.IsGrounded())
            {
                ApplyGravity();
            } 
            
            
        }

        private Vector3 ProjectedMove(Vector3 input, Vector3 groundNormal)
        {
            Vector3 projectedMove;
            if (Math.Abs(groundNormal.y) < 0.01f) // Climbing branch
            {
                projectedMove = GetClimbingMove(input, groundNormal);

            }else
            {
                // Plane and slope branch
                var horizontalMove = new Vector3(input.x, 0, input.y).normalized;
                projectedMove = Vector3.ProjectOnPlane(horizontalMove, groundNormal).normalized;
            }

            return projectedMove;
        }

        /// <summary>
        /// Given the input from PlayerInputManager and the normal from PhysicsModule, calculate the best projected 
        /// movement between all the sheaf of planes.
        /// </summary>
        /// <param name="input"> Player input</param>
        /// <param name="normal"> Ground normal of the surface</param>
        /// <returns>Projected movement</returns>
        private Vector3 GetClimbingMove(Vector2 input, Vector3 normal)
        {
            var normalXZ = new Vector3(normal.x, 0f, normal.z).normalized;

            // Octants
            Vector3[] directions = new Vector3[]
            {                                                        // normal direction
                new Vector3( 1, 0,  0),                              // →
                new Vector3( 1, 0,  1).normalized,                   // ↗
                new Vector3( 0, 0,  1),                              // ↑
                new Vector3(-1, 0,  1).normalized,                   // ↖
                new Vector3(-1, 0,  0),                              // ←
                new Vector3(-1, 0, -1).normalized,                   // ↙
                new Vector3( 0, 0, -1),                              // ↓
                new Vector3( 1, 0, -1).normalized                    // ↘
            };

            // Transformation for every octants
            Vector3[] inputMap = new Vector3[]
            {
                new Vector3(0, -input.x, input.y),     // → ok
                new Vector3(-input.y, -input.x, 0),    // ↗ ok
                new Vector3(input.x, -input.y, 0),     // ↑ ok
                new Vector3(input.x, -input.y, 0),     // ↖ ok
                new Vector3(0, input.x, input.y),      // ← ok
                new Vector3(-input.y, input.x, 0),     // ↙ ok
                new Vector3(input.x, input.y, 0),      // ↓ ok
                new Vector3(input.x, input.y, 0),      // ↘ ok
            };

            var bestDot = -1f;
            var bestIndex = 0;

            // Calculate nearest octants 
            for (var i = 0; i < directions.Length; i++)
            {
                var dot = Vector3.Dot(normalXZ, directions[i]);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestIndex = i;
                }
            }

            var move = inputMap[bestIndex];
            return Vector3.ProjectOnPlane(move.normalized, normal);
        }
        
        private void ApplyRotation()
        {
            PhysicsModule physicsModule = Player.Instance.PhysicsModule;
            if (physicsModule)
            {
                //Debug.Log("Movement delta " + Player.Instance.RaycastManager.GetMovementDelta());

                transform.parent.rotation = Quaternion.RotateTowards(transform.parent.rotation, _targetRotation, RotationSpeedOnSlope * Time.fixedDeltaTime);
                if (Quaternion.Angle(transform.parent.rotation, _targetRotation) < 0.01f)
                    physicsModule.OnRotatingEnd();
            }
        }
        

        private void ApplyTouchGrounded()
        {
            Vector3 attach = (Player.Instance.GetGroundNormal()) * (0.1f * Gravity);
            _controller.Move(attach * Time.fixedDeltaTime);
            //Debug.DrawRay(transform.position, attach * Time.fixedDeltaTime, Color.green,3f);

        }
        private void ApplyGravity()
        {
            _verticalVelocity += Gravity * Time.fixedDeltaTime;
            Vector3 fall = new Vector3(0f, _verticalVelocity, 0f);
            _controller.Move(fall * Time.fixedDeltaTime);
        }
        
        #endregion
    }
}
