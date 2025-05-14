using System;
using System.Collections;
using System.Collections.Generic;
using Player.PlayerController;
using UnityEngine;

namespace Player
{
    /*
 * Component that manages every physics interaction with the world
 * Receives collision callbacks information from the player rigidbody and applies
 * rules basing on the associated physics logic using world tags
 */
    public struct CollisionData
    {
        public Collision CollisionInfo;
        public int Layer;
        public string Tag;

        public CollisionData(Collision collisionInfo, int layer, string tag)
        {
            CollisionInfo = collisionInfo;
            Layer = layer;
            Tag = tag;
        }
    }

    public class PhysicsModule : MonoBehaviour
    {
        [SerializeField] private float minCorrelationForTransition = 0.1f;
    
        private bool _isGrounded;
        private List<ContactPoint> _contactPoints = new List<ContactPoint>();
    
        private Queue<int> _collisionLayers = new Queue<int>();
        private float _collisionAngle;
        private Vector3 _groundNormal = Vector3.up;

        private bool _isRotating = false; 
        public bool IsRotating
        {
            get => _isRotating;
        }
        private int _groundLayer;


        private void FixedUpdate()
        {
            //Debug.Log("GroundNormal: " + _groundNormal);
            Debug.DrawRay(transform.position, _groundNormal*5f, Color.red,3f);

        }

        private void Awake()
        {
            _groundLayer = LayerMask.NameToLayer("Ground");
        }


        private void SetWalkGrounded()
        {
            RaycastManager raycastManager = Player.Instance.RaycastManager;
            ControlModuleManager controlModule = Player.Instance.ControlModuleManager;
            if (controlModule && !controlModule.IsSwitching && controlModule.GetActiveModuleName() == "Walk")
            {
                if (raycastManager)
                {
                    List<RaycastHit> hits = raycastManager.GetHitList();
                    if (hits.Count > 0)
                    {
                        // Is walking normally
                        _isGrounded = true;
                        UpdateGroundNormal(hits, raycastManager);
                    }
                    else
                    {
                        // For some reason, the robot is falling
                        _isGrounded = false;
                    }
                    
                }
            }
        }
        
        public bool CanMove(Vector3 movement)
        {
            //Debug.DrawLine(Player.Instance.Rigidbody.position, Player.Instance.Rigidbody.position + movement*50.0f, Color.green);
            if (movement.magnitude > 0.0f)
            {
                RaycastManager raycastManager = Player.Instance.RaycastManager;
                List<RaycastHit> hits = raycastManager.GetHitList();
                float maxCorrelation = 0.5f;
                float sumCorrelation = 0.0f;
                foreach (var hit in hits)
                {
                    float correlation = GetMovementCorrelation(hit.point, movement);
                    if (correlation > 0.0f)
                    {
                        sumCorrelation += correlation;
                    }
                    
                }
                //Debug.Log("Computed correlation: " + correlation);
                if (sumCorrelation > maxCorrelation)
                {
                    return true;
                }
                return false;
            }
            else
            {
                return true;
            }
            
        }


        private void Fall()
        {
            // Apply movement in the direction of fall to simulate the weight
            CharacterController characterController = Player.Instance.CharacterController;
            Vector3 movementDirection = Player.Instance.RaycastManager.GetMovementDelta();
            
            // Apply first horizontal movement
            
            Player.Instance.ControlModuleManager.SwitchMode();
        }
        /*
         * Function that computes the istantaneous normal that determines the rotation of the player
         */
        private void UpdateGroundNormal(List<RaycastHit> hits, RaycastManager raycastManager)
        {
            if (!_isRotating)
            {
                Vector3 newNormal = AverageDirection(hits.ConvertAll(x => x.normal));
                if (newNormal != _groundNormal)
                {
                    _groundNormal = newNormal;
                    _isRotating = true;
                }
                else
                {
                    _isRotating = false;
                }
            }
            
        }
        
        private Vector3 AverageDirection(List<Vector3> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (var v in vectors)
            {
                sum += v.normalized;
            }

            if (sum.sqrMagnitude < Mathf.Epsilon)
                return Vector3.zero;

            return sum.normalized;
        }
        /*
        private void UpdateGroundNormal(List<RaycastHit> hits, RaycastManager raycastManager)
        {
            if (!_isRotating)
            {
                if (raycastManager.GetMovementDelta().magnitude > 0.0f)
                {
                    float maxCorrelation = 0.0f;
                    foreach (var hit in hits)
                    {
                        float correlation = GetMovementCorrelation(hit.point, raycastManager.GetMovementDelta());
                        if (correlation > maxCorrelation && correlation > minCorrelationForTransition)
                        {
                            if (hit.normal != _groundNormal)
                            {
                                _isRotating = true;
                            }
                            else
                            {
                                _isRotating = false;
                            }
                            _groundNormal = hit.normal;
                            maxCorrelation = correlation;
                        }
                    }
                }
                else
                {
                    _groundNormal = hits[0].normal;
                }
            }
            
        }*/
      
        public void OnRotatingEnd()
        {
            _isRotating = false;
        }

        public void OnEnterPhysicsUpdate(CollisionData hitData)
        {
            if(hitData.Layer == _groundLayer)
                _groundNormal = GetCollisionNormal(hitData);
            switch (hitData.Tag)
            {
                default:
                    break;
            }
            _collisionLayers.Enqueue(hitData.Layer);
            UpdateGrounded();
        }
    
        public void OnExitPhysicsUpdate(CollisionData hitData)
        {
            switch (hitData.Tag)
            {
                default:
                    break;
            }
            Debug.Log("Called OnExit");

            TryDequeueTerrain(hitData);
            UpdateGrounded();
        }
    
        private void TryDequeueTerrain(CollisionData hitData)
        {
            _collisionLayers.Dequeue();
        }

        public bool IsGrounded()
        {
            SetWalkGrounded();
            return _isGrounded;
        }

        private void UpdateGrounded()
        {
            if (_collisionLayers.Contains(_groundLayer))
            {
                _isGrounded = true;
            }
            else
            {
                _isGrounded = false;
            }
        }
        public Vector3 GetGroundNormal()
        {
            return _groundNormal;
        }
        
        private Vector3 GetCollisionNormal(CollisionData hitData)
        {
            // Take the instantaneous velocity of the player to infer its movement direction
            // and understand the correct terrain point to consider
            hitData.CollisionInfo.GetContacts(_contactPoints);
            ContactPoint? collisionPoint; 
            GetCollisionPoint(out collisionPoint);


            // Get the normal and calculate the global angle with respect to the global Y axis
            if (collisionPoint.HasValue)
            {
                Debug.DrawRay(transform.position, collisionPoint.Value.normal*5f, Color.red,3f);
                return collisionPoint.Value.normal;
            }
            else
            {
                return Vector3.zero;
            }
        }

    
        /*
            * Function that calculates the *new* ground angle
            * when a collision is triggered, thus when the player moves from one terrain to another
        */
        private float GetCollisionAngle(CollisionData hitData)
        {
            // Take the instantaneous velocity of the player to infer its movement direction
            // and understand the correct terrain point to consider
            hitData.CollisionInfo.GetContacts(_contactPoints);
            ContactPoint? collisionPoint; 
            GetCollisionPoint(out collisionPoint);
            //Debug.Log("CollisionPoint: " + collisionPoint);
        
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
         * Function that returns the collision point to be considered as correct for ground normal computation
         * It is taken based on the Down direction of the world
         */
        private void GetCollisionPoint(out ContactPoint? contactPoint)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            if (_contactPoints.Count > 0)
            {
                float maxCorrelation = Vector3.Dot((rb.position - _contactPoints[0].point), Vector3.down);
                int maxIndex = 0;
                for (int i = 1; i < _contactPoints.Count; i++)
                {
                    float correlation = Vector3.Dot((rb.position - _contactPoints[i].point), Vector3.down);
                    if (correlation > maxCorrelation)
                    {
                        maxIndex = i;
                    }
                }
                
                contactPoint = _contactPoints[maxIndex];
             
            }
            else
            {
                contactPoint = null;
            }
        
        }

        
        /*
         * Function that calculates the movement correlation for each contact points
         * and chooses the one with the highest value of correlation
         */
        private void GetCollisionPoint_Deprecated(Vector3 instantaneousVelocity, out ContactPoint? contactPoint)
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
            
            Rigidbody rb= Player.Instance.Rigidbody;
            Vector3 toPointLocal = rb.transform.InverseTransformPoint(point);
            toPointLocal.y = 0f;

            Vector3 velLocal = rb.transform.InverseTransformDirection(velocity);
            velLocal.y = 0f;

            // 4) dot in local XZ
            return Vector3.Dot(toPointLocal.normalized, velLocal.normalized);
        }
    }
}