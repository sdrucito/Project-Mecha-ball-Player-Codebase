using System;
using UnityEngine;

public class Mock_InputController : MonoBehaviour
{
    private BallModule _ballModule;
    
    public static event Action<Vector3> OnMovementInput; // Callback for directional input
    public static event Action OnJumpInput; // Callback for jump input
    public static event Action<Vector3> OnSprintInput; // Callback for sprint input

    public static event Action OnModeChangeInput; 
    
    public

    void FixedUpdate()
    {
        Vector3 direction = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            direction += Vector3.back;
        if (Input.GetKey(KeyCode.A))
            direction += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            direction += Vector3.right;

        if (direction != Vector3.zero)
            OnMovementInput?.Invoke(direction.normalized);
        else
            direction = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.Space))
            OnJumpInput?.Invoke();

        if (Input.GetKey(KeyCode.LeftShift))
            OnSprintInput?.Invoke(direction);
        
        if(Input.GetKey(KeyCode.LeftControl))
            OnModeChangeInput?.Invoke();
        
    }

    
}