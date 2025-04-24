using System;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private float minCorrelationForTransition = 0.5f;
    
    private bool _isGrounded;
    private Rigidbody _rigidbody;
    private ControlModuleManager _controlModuleManager;
    private RaycastManager _raycastManager;
    private List<ContactPoint> _contactPoints = new List<ContactPoint>();
    
    private Queue<string> _collisionTags = new Queue<string>();
    private float _collisionAngle;
    [SerializeField] private Vector3 _groundNormal;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _controlModuleManager = GetComponent<ControlModuleManager>();
        _raycastManager = GetComponent<RaycastManager>();
    }

    private void Start()
    {
        
    }

    private void FixedUpdate()
    {
        SetWalkGrounded();
    }

    private void SetWalkGrounded()
    {
        if (_controlModuleManager && _controlModuleManager.GetActiveModuleName() == "Walk")
        {
            List<RaycastHit> hits = _raycastManager.GetHitList();
            if (hits.Count > 1)
            {
                _isGrounded = true;
            }
            else
            {
                _isGrounded = false;
            }
        }
    }

    public void OnEnterPhysicsUpdate(CollisionData hitData)
    {
        switch (hitData.Tag)
        {
            case "Ground":
                _collisionAngle = GetCollisionAngle(hitData);
                break;
        }
        _collisionTags.Enqueue(hitData.Tag);
        UpdateGrounded();
    }
    
    public void OnExitPhysicsUpdate(CollisionData hitData)
    {
        switch (hitData.Tag)
        {
            case "Ground":
                _isGrounded = false;
                break;
        }

        TryDequeueTerrain(hitData);
        UpdateGrounded();
    }
    
    private void TryDequeueTerrain(CollisionData hitData)
    {
        string tag;
        _collisionTags.TryPeek(out tag);
        if (tag == hitData.Tag)
        {
            _collisionTags.Dequeue();
        }
    }

    public bool IsGrounded()
    {
        return _isGrounded;
    }

    private void UpdateGrounded()
    {
        if (_collisionTags.Contains("Ground"))
        {
            _isGrounded = true;
        }
    }
    public Vector3 GetGroundNormal()
    {
        return _groundNormal;
    }

    
    /*
     * Function that calculates the *new* ground angle
     * when a collision is triggered, thus when the player moves from one terrain to another
     */
    private float GetCollisionAngle(CollisionData hitData)
    {
        // Take all contact points
        int numContactPoints = hitData.CollisionInfo.GetContacts(_contactPoints);
        
        // Take the instantaneous velocity of the player to infer its movement direction
        // and understand the correct terrain point to consider
        Vector3 instantaneousVelocity = _rigidbody.linearVelocity;
        ContactPoint? collisionPoint; 
        GetCollisionPoint(instantaneousVelocity, out collisionPoint);
        
        // Get the normal and calculate the global angle with respect to the global Y axis
        if (collisionPoint.HasValue)
        {
            return Vector3.Angle(collisionPoint.Value.normal, Vector3.up);
        }
        else
        {
            return float.NaN;
        }
    }

    /*
     * Function that calculates the movement correlation for each contact points
     * and chooses the one with the highest value of correlation
     */
    private void GetCollisionPoint(Vector3 instantaneousVelocity, out ContactPoint? contactPoint)
    {
        if (_contactPoints.Count > 0)
        {
            float maxCorrelation = GetMovementCorrelation(_contactPoints[0].point, instantaneousVelocity);
            int maxIndex = 0;
            for (int i = 1; i < _contactPoints.Count; i++)
            {
                float correlation = GetMovementCorrelation(_contactPoints[i].point, instantaneousVelocity);
                if (correlation > maxCorrelation)
                {
                    maxIndex = i;
                }
            }

            if (maxCorrelation > minCorrelationForTransition)
            {
                contactPoint = _contactPoints[maxIndex];
            }
            else
            {
                // For now return the same value
                contactPoint = _contactPoints[maxIndex];
            }
        }
        else
        {
            contactPoint = null;
        }
        
    }

    /*
     * Function that estimates the probability that the player is moving
     * toward a point given its instant velocity
     */
    private float GetMovementCorrelation(Vector3 point, Vector3 velocity)
    {
        Vector3 positionVector = point - _rigidbody.position;
        return Vector3.Dot(positionVector, velocity);
    }
}
