using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.PlayerController
{
    public class PlayerInputManager : MonoBehaviour
    {
        public static PlayerInputManager Instance;
        private PlayerInput _playerInput;
    
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _changeModeAction;
        private InputAction _sprintImpulseAction;
        private InputAction _previousCameraAction;
        private InputAction _nextCameraAction;

        private const float ISOMETRIC_OFFSET = 45;
        [SerializeField] private bool MouseEnabled = false;
        
        public event Action<Vector2> OnMoveInput;
        public event Action<Vector2> OnLookInput;
        public event Action OnJumpInput;
        public event Action OnModeChangeInput;
        public event Action<Vector2> OnSprintImpulseInput;
        public event Action PreviousCamera;
        public event Action NextCamera;
    
        private Vector2 _currentMoveInput = Vector2.zero;
        private Vector2 _currentDirectionInput = Vector2.zero;
        private bool _isSprintImpulse = false;
        private float _inputRotationAngle = 0f;
        
        #region Private Methods
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

            _playerInput = GetComponent<PlayerInput>();
            _moveAction = _playerInput.actions.FindAction("Move");
            _lookAction = _playerInput.actions.FindAction("Look");
            _jumpAction = _playerInput.actions.FindAction("Jump");
            _changeModeAction = _playerInput.actions.FindAction("ChangeMode");
            _sprintImpulseAction = _playerInput.actions.FindAction("Sprint");
            _previousCameraAction = _playerInput.actions.FindAction("Previous Camera");
            _nextCameraAction = _playerInput.actions.FindAction("Next Camera");
        
            _moveAction.performed += ctx => _currentMoveInput = ctx.ReadValue<Vector2>();
            _moveAction.canceled += ctx => _currentMoveInput = Vector2.zero;
            _lookAction.performed += ctx =>
            {
                var device = ctx.control.device;

                if (!MouseEnabled && device is Mouse)
                {
                    return;
                }

                _currentDirectionInput = ctx.ReadValue<Vector2>();
            };
            _lookAction.canceled += ctx => _currentDirectionInput = Vector2.zero;
            _jumpAction.started += ctx => OnJumpInput?.Invoke();
            _changeModeAction.started += ctx => OnModeChangeInput?.Invoke();
            _sprintImpulseAction.started += ctx => _isSprintImpulse = true;
            _sprintImpulseAction.canceled += ctx => _isSprintImpulse = false;
            _previousCameraAction.started += ctx => PreviousCamera?.Invoke();
            _nextCameraAction.started += ctx => NextCamera?.Invoke();
        }

        private void FixedUpdate()
        {
            var inputCameraRelative = RotateInput(_currentMoveInput, _inputRotationAngle+ISOMETRIC_OFFSET);
            OnMoveInput?.Invoke(inputCameraRelative);
            if (_isSprintImpulse && _currentMoveInput != Vector2.zero)
            {
                OnSprintImpulseInput?.Invoke(inputCameraRelative);
            }
            
            var rotationCameraRelative = RotateInput(_currentDirectionInput, _inputRotationAngle+ISOMETRIC_OFFSET);
            OnLookInput?.Invoke(rotationCameraRelative);

            UpdateCursorState();
        }
        
        private Vector2 RotateInput(Vector2 input, float angleDegrees)
        {
            if (angleDegrees == 0) return input;
            var radians = angleDegrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(radians);
            var sin = Mathf.Sin(radians);
            return new Vector2(
                input.x * cos - input.y * sin,
                input.x * sin + input.y * cos
            );
        }
        
        private void UpdateCursorState()
        {
            if (!MouseEnabled)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        #endregion

        #region Public Methods

        public void SetInputEnabled(bool inputEnabled)
        {
            if (inputEnabled)
            {
                //_playerInput.ActivateInput();
                _playerInput.actions.FindActionMap("Player", true).Enable();
            }
            else
            {
                _playerInput.actions.FindActionMap("Player", true).Disable();
                //_playerInput.DeactivateInput();
            }
        }
        
        public void SetActionEnabled(string actionName, bool enabled)
        {
            var action = _playerInput.actions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"No action called '{actionName}'");
                return;
            }

            if (enabled)
            {
                ResetAction(actionName);
            }
            else
            {
                action.Disable();
            }
        }
        
        public void ResetAction(string actionName)
        {
            var action = _playerInput.actions.FindAction(actionName);
            if (action == null)
            {
                Debug.LogWarning($"[ResetAction] No action named '{actionName}'");
                return;
            }
            action.Disable();  
            action.Enable();
        }
        
        public void SetInputRotation(float angle)
        {
            _inputRotationAngle += angle;
        }
        #endregion
    }
}