using System;
using System.Collections.Generic;
using Player.Animation;
using Player.PlayerController;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Player.ControlModules
{
    public class WalkingModule2 : ControlModule, IFixedUpdateObserver
    {
        public int FixedUpdatePriority { get; set; }
        
        [SerializeField] private float WalkingSpeed = 5f;
        [SerializeField] private float Gravity = -9.8f;
        //[SerializeField] private float RotationSpeedOnSlope = 80f; //not used
        [SerializeField] private float RotationSpeed = 40f;
        [SerializeField] private float ManualRotationMultiplier = 2f;
        
        private float _verticalVelocity=0;
        private bool _wasBlocked = false;
        private Quaternion _currentRotation;
        
        private Vector2 _inputVector = Vector2.zero;
        private Vector2 _directionInputVector = Vector2.zero;
        
        [SerializeField] private PlayerKneeWalkAnimator PlayerKneeWalkAnimator;
        private Rigidbody _rigidbody;
        [SerializeField] private float OverrideLinearDrag;
        [SerializeField] private float OverrideAngularDrag;

        private Vector3 _lastFixedMovementDelta;
        private Vector3 _lastFixedMovementApplied;
        private Vector3 _lastPosition;

        private Quaternion _lastFixedRotationDelta;
        private Quaternion _lastFixedRotationApplied;
        private Quaternion _lastRotation;

        #region Unity Lifecycle (Awake, Start, OnEnable, OnDisable)
        private void Awake()
        {
            name = "Walk";
            FixedUpdatePriority = 0;
        }

        private void Start()
        {
            _rigidbody = Player.Instance.Rigidbody;
            PlayerKneeWalkAnimator.OnOpenFinished += OpenFinished;
            _lastPosition = Player.Instance.Rigidbody.position;
            _lastFixedMovementApplied = Vector3.zero;
            _lastFixedMovementDelta = Vector3.zero;
            _lastFixedRotationDelta = Quaternion.identity;
            _lastFixedRotationApplied = Quaternion.identity;
        }

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput += HandleMovement;
                PlayerInputManager.Instance.OnLookInput += HandleDirection;
                OpenFinished();
                PlayerKneeWalkAnimator.enabled = true;
                
                if (!_rigidbody) _rigidbody = Player.Instance.Rigidbody;
                _rigidbody.linearDamping = OverrideLinearDrag;
                _rigidbody.angularDamping = OverrideAngularDrag;
                _rigidbody.WakeUp();
            }
        }

        private void OnDisable()
        {
            FixedUpdateManager.Instance?.Unregister(this);
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput -= HandleMovement;
                PlayerInputManager.Instance.OnLookInput -= HandleDirection;
                PlayerKneeWalkAnimator.enabled = false;
            }
        }
        #endregion
        
        #region Input Handlers
        private void OpenFinished()
        {
            // Reset movement delta
            _lastFixedMovementDelta = Vector3.zero;
            _lastFixedMovementApplied = Vector3.zero;
            _lastPosition = Player.Instance.Rigidbody.position;
            // Reset rotation delta
            _lastFixedRotationDelta = Quaternion.identity;
            _lastFixedRotationApplied = Quaternion.identity;
            _lastRotation = Player.Instance.Rigidbody.rotation;
            if (_rigidbody) _rigidbody.isKinematic = false;
            PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);

        }
        private void HandleMovement(Vector2 input){
            _inputVector = input;
        }

        private void HandleDirection(Vector2 input)
        {
            _directionInputVector = input;
        }
        #endregion
        
        public void ObservedFixedUpdate()
        {
            // Pre-movement phase
            ApplyWorldMovementToPlayer();                       // Send to leg animator the difference of movement delta
            ApplyWorldRotationToPlayer();                       // Compute world rotation and apply difference of user rotation
            Player.Instance.RaycastManager.MovementDelta = GetPredictedMovement(); // Add to RaycastManager the movement applied for leg anticipation
            Player.Instance.RaycastManager.RotationDelta = GetPredictedRotation(); // Add to RaycastManager the rotation applied for leg anticipation
            PlayerKneeWalkAnimator.ExecuteGrounded();           // Fire ground check after input applied
            
            // Movement and rotation
            _lastFixedMovementApplied = Vector3.zero;           // Reset last fixed movement applied
            _lastFixedRotationApplied = Quaternion.identity;    // Reset last fixed rotation applied
            ExecuteMovement();                                  // Execute movement function

            // Post-movement phase
            ValidateMovement();                                 // Check effective movement for walk animator and raycast manager
            PlayerKneeWalkAnimator.ExecuteGrounded();           // Re-execute ground check after movement and rotation delta applied
            ApplyGravity();
        }
        
        #region Pre-movement and rotation methods
        private void ApplyWorldRotationToPlayer()
        {
            _lastFixedRotationDelta = Player.Instance.Rigidbody.rotation * Quaternion.Inverse(_lastRotation);
            Quaternion rotationDifference = _lastFixedRotationDelta * Quaternion.Inverse(_lastFixedRotationApplied);
            _lastRotation = Player.Instance.Rigidbody.rotation;
            PlayerKneeWalkAnimator.FollowUserRotation(rotationDifference);
        }

        private void ApplyWorldMovementToPlayer()
        {
            _lastFixedMovementDelta = Player.Instance.Rigidbody.position - _lastPosition;
            Vector3 movementDifference = _lastFixedMovementDelta - _lastFixedMovementApplied;
            
            _lastPosition = Player.Instance.Rigidbody.position;
   
            PlayerKneeWalkAnimator.FollowUserMovement(movementDifference);
        }

        private Vector3 GetPredictedMovement()
        {
            Vector3 groundNormal = Player.Instance.GetGroundNormal();
            Vector3 projectedMove = ProjectedMove(_inputVector,groundNormal);
            return projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
        }

        private Quaternion GetPredictedRotation()
        {
            Vector3 groundNormal = Player.Instance.GetGroundNormal();
            Vector3 projectedMove = GetPredictedMovement();
            // Rotate the player according to look or move direction only if it can move
            if (_directionInputVector.magnitude < 0.01f){ // auto
                if (projectedMove.magnitude > 0.01f)
                {
                    Quaternion target = Quaternion.LookRotation(projectedMove, groundNormal);
                    
                    float maxDegreesDelta = RotationSpeed * Time.fixedDeltaTime;
                    Quaternion next = Quaternion.RotateTowards(_lastRotation, target, maxDegreesDelta);

                    Quaternion delta = next * Quaternion.Inverse(_lastRotation);

                    return delta;
                }
            } else { //manual
                Vector3 projectedDirection = ProjectedMove(_directionInputVector, groundNormal);
                Quaternion target = Quaternion.LookRotation(projectedDirection, groundNormal);

                float maxDegrees = RotationSpeed * Time.fixedDeltaTime * ManualRotationMultiplier;
                Quaternion next = Quaternion.RotateTowards(_lastRotation, target, maxDegrees);

                return next * Quaternion.Inverse(_lastRotation);
                
            }
            return Quaternion.identity;
        }
        #endregion
        
        #region Movement methods
        private void ExecuteMovement()
        {
            var groundNormal = Player.Instance.GetGroundNormal();
            Debug.DrawLine(_rigidbody.position,_rigidbody.position + groundNormal * 2.0f, Color.red);
            var projectedMove = ProjectedMove(_inputVector,groundNormal);
            if(Player.Instance.IsGrounded())
                ApplyTouchGrounded();
            ExecuteRotation(projectedMove, groundNormal);

            if (Player.Instance.CanMove(projectedMove) && !_wasBlocked)
            {
                Player.Instance.RaycastManager.MovementDelta = Vector3.zero;
                Player.Instance.RaycastManager.RotationDelta = Quaternion.identity;
                PlayerKneeWalkAnimator.ExecuteGrounded(); 
                groundNormal = Player.Instance.GetGroundNormal();
                projectedMove = ProjectedMove(_inputVector, groundNormal);
                
                _lastFixedRotationApplied = GetPredictedRotation();     // Re-call the prediction that now is "real"
                
                var moveDirection = projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
                if(moveDirection != Vector3.zero)
                    _rigidbody.MovePosition(_rigidbody.position + moveDirection);
                
                // Update the last movement applied by the user
                _lastFixedMovementApplied = moveDirection;
            }else {
                Debug.Log("Blocked movement");
                if(_wasBlocked)
                    _wasBlocked = false;
                else
                    _wasBlocked = true;
            }
            
        }

        private void ExecuteRotation(Vector3 projectedMove, Vector3 groundNormal)
        {
            //Debug.Log("Executing Rotation");
            Quaternion rotationAroundPlayer;
            if (_directionInputVector.magnitude < 0.01f) // auto
            {
                if (projectedMove.magnitude > 0.01f)
                {
                    rotationAroundPlayer = Quaternion.LookRotation(projectedMove, groundNormal);
                }
                else
                {
                    rotationAroundPlayer = Quaternion.LookRotation(_rigidbody.transform.forward, groundNormal);
                }
                
            }
            else // manual
            {
                var projectedDirection = ProjectedMove(_directionInputVector, groundNormal);
                rotationAroundPlayer = Quaternion.LookRotation(projectedDirection, groundNormal);
            }
                
            var rotationSpeed = (_directionInputVector.magnitude < 0.01f) ? RotationSpeed : RotationSpeed * ManualRotationMultiplier;

            _rigidbody.MoveRotation(Quaternion.RotateTowards(
                transform.parent.rotation,
                rotationAroundPlayer,
                rotationSpeed * Time.fixedDeltaTime
            ));
            if (Player.Instance.PhysicsModule &&
                Quaternion.Angle(transform.parent.rotation, rotationAroundPlayer) < 0.01f)
            {
                Player.Instance.PhysicsModule.OnRotatingEnd();
            }
        }

        private void ValidateMovement()
        {            
            // Verify the input movement is effectively moving the player
            if (_lastFixedMovementDelta.magnitude < 0.01f)
            {
                // Flush the input movement if the player isn't moving
                _lastFixedMovementApplied = Vector3.zero;
            }
            // The same goes for the rotation
            if (_lastFixedRotationDelta == Quaternion.identity)
            {
                // Flush the input movement if the player isn't moving
                _lastFixedRotationApplied = Quaternion.identity;
            }
            
            // Update the movement delta for both the walk animator and raycast manager
            PlayerKneeWalkAnimator.MovementDelta = _lastFixedMovementApplied;
            PlayerKneeWalkAnimator.RotationDelta = _lastFixedRotationApplied;
            // If I can't walk, flush the movement delta in the raycast manager
            Player.Instance.RaycastManager.MovementDelta = _lastFixedMovementApplied;
            Player.Instance.RaycastManager.RotationDelta = _lastFixedRotationApplied;
            
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

        private void ApplyTouchGrounded() {
            List<RaycastHit> contactPoints = Player.Instance.RaycastManager.GetHitList();
            if (contactPoints.Count <= 0) return; //floating

            var allNormalsEqual = true;
            RaycastHit groundHit = contactPoints[0];
            for (var i = 1; i < contactPoints.Count; i++) {
                if (Vector3.Angle(contactPoints[i].normal, groundHit.normal) > 1f) // 1° degree of tollerance
                {
                    allNormalsEqual = false;
                    break;
                }
            }

            
            bool isRotating = Player.Instance.PhysicsModule.IsRotating;
            if (allNormalsEqual || isRotating)
            {
                Vector3 attach;
                if(isRotating)
                    attach = Player.Instance.GetGroundNormal() * (0.1f * Gravity);
                else
                    attach = groundHit.normal * (0.1f * Gravity);
                _rigidbody.MovePosition(_rigidbody.position + attach * Time.fixedDeltaTime);
                _rigidbody.AddForce(attach*10f, ForceMode.Impulse);
                _lastFixedMovementApplied += attach * Time.fixedDeltaTime;
                //if(Player.Instance.PhysicsModule.IsRotating)
                    //Debug.DrawRay(transform.position, attach * (100 * Time.fixedDeltaTime), Color.green, 3f);
            }
            
        }
        private void ApplyGravity()
        {
            if (!Player.Instance.IsGrounded())
            {
                _verticalVelocity += Gravity * Time.fixedDeltaTime;
            }
            else if (_verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
            }

            if (_verticalVelocity != 0f)
            {
                Vector3 fall = new Vector3(0f, _verticalVelocity, 0f) * Time.fixedDeltaTime;
                _rigidbody.MovePosition(_rigidbody.position + fall);
            }
        }
        #endregion

        #region static methods
        // <summary>
        /// Given the input from PlayerInputManager and the normal from PhysicsModule, calculate the best projected 
        /// movement between all the sheaf of planes.
        /// </summary>
        /// <param name="input"> Player input</param>
        /// <param name="normal"> Ground normal of the surface</param>
        /// <returns>Projected movement</returns>
        private static Vector3 GetClimbingMove(Vector2 input, Vector3 normal)
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
        # endregion
    }
}
