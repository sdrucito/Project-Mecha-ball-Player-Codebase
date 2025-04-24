using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerInputManager : MonoBehaviour
{
    public static PlayerInputManager Instance;
    private PlayerInput _playerInput;
    
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _changeModeAction;
    
    public event Action<Vector2> OnMoveInput;
    public event Action OnJumpInput;
    public event Action OnModeChangeInput;
    
    private Vector2 _currentMoveInput = Vector2.zero;
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
        
        _moveAction.performed += ctx => _currentMoveInput = ctx.ReadValue<Vector2>();
        _moveAction.canceled += ctx => _currentMoveInput = Vector2.zero;
        _jumpAction.started += ctx => OnJumpInput?.Invoke();
        _changeModeAction.started += ctx => OnModeChangeInput?.Invoke();
        
    }

    private void FixedUpdate()
    {
        OnMoveInput?.Invoke(_currentMoveInput);
    }
    
}