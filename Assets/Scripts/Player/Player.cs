using System;
using UnityEngine;


[RequireComponent(typeof(Rigidbody),typeof(PlayerAttributes))]
public class Player : MonoBehaviour
{
    [SerializeField] protected float distToGround;

    
    private PhysicsModule _physicsModule;
    private PlayerAttributes _playerAttributes;


    private void Start()
    {
        _physicsModule = GetComponent<PhysicsModule>();
        _playerAttributes = GetComponent<PlayerAttributes>();
        InitializePlayer();
    }

    private void FixedUpdate()
    {
        //_isGrounded = _physicsModule.IsGrounded();
    }

    private void InitializePlayer()
    {
        _playerAttributes.ResetMaxHealth();
    }
    private void OnCollisionEnter(Collision other)
    {
        // Create collision data wrapper
        CollisionData collisionData = new CollisionData(other, other.gameObject.tag);
        _physicsModule.OnEnterPhysicsUpdate(collisionData);
    }

    private void OnCollisionExit(Collision other)
    {
        // Create collision data wrapper
        CollisionData collisionData = new CollisionData(other, other.gameObject.tag);
        _physicsModule.OnExitPhysicsUpdate(collisionData);    
    }

    public bool IsGrounded()
    {
        return _physicsModule.IsGrounded();
    }

    public Vector3 GetGroundNormal()
    {
        return _physicsModule.GetGroundNormal();
    }
}
