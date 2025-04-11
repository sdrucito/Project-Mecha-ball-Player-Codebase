using System;
using UnityEngine;


/*
 * Component that manages every physics interaction with the world
 * Receives collision callbacks information from the player rigidbody and applies
 * rules basing on the associated physics logic using world tags
 */
public struct CollisionData
{
    public Collision CollisionInfo;
    public string Tag;

    public CollisionData(Collision collisionInfo, string tag)
    {
        CollisionInfo = collisionInfo;
        Tag = tag;
    }
}

public class PhysicsModule : MonoBehaviour
{
    
    private bool _isGrounded;
    private Rigidbody _rigidbody;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void OnEnterPhysicsUpdate(CollisionData hitData)
    {
        switch (hitData.Tag)
        {
            case "Ground":
                
                _isGrounded = true;
                break;
        }
    }
    
    public void OnExitPhysicsUpdate(CollisionData hitData)
    {
        switch (hitData.Tag)
        {
            case "Ground":
                _isGrounded = false;
                break;
        }
    }

    public bool IsGrounded()
    {
        return _isGrounded;
    }
}
