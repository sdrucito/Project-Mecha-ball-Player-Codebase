using System;
using Player.Animation;
using Player.PlayerController;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

namespace Player.ControlModules
{
    public class WalkingModule : ControlModule
    {
        [SerializeField] private float WalkingSpeed = 5f;
        [SerializeField] private float Gravity = -9.8f;
        [SerializeField] private float rotationSpeed = 120f;

        private float _verticalVelocity=0;

        private CharacterController _controller;
        private Vector2 _inputVector = Vector2.zero;
    
        [SerializeField] private PlayerKneeWalkAnimator playerWalkAnimator;
        
        private Quaternion _targetRotation = Quaternion.identity;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            name = "Walk";
        }

        private void Start()
        {
            _controller = Player.Instance.CharacterController;
            playerWalkAnimator.OnOpenFinished += OpenFinished;
        }

        private void OnEnable()
        {
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput += HandleMovement;
                OpenFinished();
                playerWalkAnimator.enabled = true;
                PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            }
        }

        private void OnDisable()
        {
            if (PlayerInputManager.Instance != null)
            {
                PlayerInputManager.Instance.OnMoveInput -= HandleMovement;
                //_controller.enabled = false;
                playerWalkAnimator.enabled = false;

            }

        }

        private void OpenFinished()
        {
            if(_controller)
                _controller.enabled = true;
        }
        private void HandleMovement(Vector2 input){
            _inputVector = input;
        }
        
        // Update is called once per frame
        void FixedUpdate()
        {
            ExecuteMovement();
        }

        #region Movement Logic
        private void ExecuteMovement()
        {
            Vector3 groundNormal = Player.Instance.GetGroundNormal();
            Vector3 projectedMove = ProjectedMove(groundNormal);
            if (Player.Instance.CanMove(projectedMove))
            {
                // Rotate the player according to normal
                _targetRotation = Quaternion.FromToRotation(transform.parent.up, groundNormal) * transform.parent.rotation;
                ApplyRotation();
                //Debug.DrawRay(transform.position, groundNormal, Color.red,3f);

                /*
                // Calculate the movement
                if(move != Vector3.zero)
                    _controller.Move(move);
                */
                
                var moveDirection = projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
                if(moveDirection != Vector3.zero)
                    _controller.Move(moveDirection);
            
                
            }
            ApplyTouchGrounded();

            if (!Player.Instance.IsGrounded())
            {
                ApplyGravity();
            }
            
        }

        private Vector3 ProjectedMove(Vector3 groundNormal)
        {
            Vector3 projectedMove;
            if (Math.Abs(groundNormal.y) < 0.01f) // Climbing branch
            {
                projectedMove = GetClimbingMove(_inputVector, groundNormal);

            }else
            {
                // Plane and slope branch
                var horizontalMove = new Vector3(_inputVector.x, 0, _inputVector.y).normalized;
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

        private Vector3 GetMovement(Vector3 groundNormal)
        {
            Vector3 horizontalMove = Vector3.zero;
            Vector3 projectedMove = Vector3.zero;
            if (Math.Abs(groundNormal.y) < 0.01f){
                projectedMove = new Vector3(0, -_inputVector.x, _inputVector.y).normalized;
            }else{
                horizontalMove = new Vector3(_inputVector.x, 0, _inputVector.y).normalized;
                projectedMove = Vector3.ProjectOnPlane(horizontalMove, groundNormal).normalized;
            }
            var move = projectedMove * (WalkingSpeed * Time.fixedDeltaTime);
            return move;
        }
        private void ApplyRotation()
        {
            PhysicsModule physicsModule = Player.Instance.PhysicsModule;
            if (physicsModule && physicsModule.IsRotating)
            {
                //Debug.Log("Movement delta " + Player.Instance.RaycastManager.GetMovementDelta());

                transform.parent.rotation = Quaternion.RotateTowards(transform.parent.rotation, _targetRotation, rotationSpeed * Time.fixedDeltaTime);
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
