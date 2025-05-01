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
        private InputAction _jumpAction;
        private InputAction _changeModeAction;
        private InputAction _sprintImpulseAction;
        private InputAction _previousCameraAction;
        private InputAction _nextCameraAction;
    
        public event Action<Vector2> OnMoveInput;
        public event Action OnJumpInput;
        public event Action OnModeChangeInput;
        public event Action<Vector2> OnSprintImpulseInput;
        public event Action SetCamera1;
        public event Action SetCamera2;
    
        private Vector2 _currentMoveInput = Vector2.zero;
        private bool _isSprintImpulse = false;
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
            _jumpAction = _playerInput.actions.FindAction("Jump");
            _changeModeAction = _playerInput.actions.FindAction("ChangeMode");
            _sprintImpulseAction = _playerInput.actions.FindAction("Sprint");
            _previousCameraAction = _playerInput.actions.FindAction("Previous Camera");
            _nextCameraAction = _playerInput.actions.FindAction("Next Camera");
        
            _moveAction.performed += ctx => _currentMoveInput = ctx.ReadValue<Vector2>();
            _moveAction.canceled += ctx => _currentMoveInput = Vector2.zero;
            _jumpAction.started += ctx => OnJumpInput?.Invoke();
            _changeModeAction.started += ctx => OnModeChangeInput?.Invoke();
            _sprintImpulseAction.started += ctx => _isSprintImpulse = true;
            _sprintImpulseAction.canceled += ctx => _isSprintImpulse = false;
            _previousCameraAction.started += ctx => SetCamera1?.Invoke();
            _nextCameraAction.started += ctx => SetCamera2?.Invoke();
        }

        private void FixedUpdate()
        {
            OnMoveInput?.Invoke(_currentMoveInput);
            if (_isSprintImpulse && _currentMoveInput != Vector2.zero)
            {
                OnSprintImpulseInput?.Invoke(_currentMoveInput);
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
    
    }
}