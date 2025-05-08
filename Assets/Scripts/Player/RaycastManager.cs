using System;
using System.Collections.Generic;
using Player.Animation;
using UnityEngine;

public class RaycastManager : MonoBehaviour
{

    [SerializeField] private float stepLength = 2.0f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float stepAnticipationMultiplier = 25f;
    [SerializeField] private string terrainLayer;

    private Rigidbody _rigidbody;
    private readonly List<RaycastHit> _hitList = new List<RaycastHit>();
    private List<LegAnimator> _legs = new List<LegAnimator>();
    
    private Vector3 _lastRootPos;
    private Vector3 _movementDelta;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _lastRootPos = _rigidbody.position;
    }

    /*
     * Computes, when active, the movement captured between the last FixedUpdate
     * It is used by all components that need to know the instantaneous velocity
     */
    void FixedUpdate()
    {
        _movementDelta = _rigidbody.position - _lastRootPos;
        _lastRootPos = _rigidbody.position;
    }

    /*
     * Function that verifies if the player can move basing on the anticipation for leg
     * It calculates, for a leg, the next position in the step and triggers the step movement
     */
    public void ExecuteStepForLeg(LegAnimator leg)
    {

        //Vector3 relativePos = pivot + horizontalOffset + anticipation;
        //relativePos.y = _rigidbody.position.y;

        RaycastHit? hit = ExecuteGroundedForLeg(leg);
        if (hit.HasValue)
        {
            // Verify if the hit distance is greater than the step length
            if (Vector3.Distance(leg.NewPosition, hit.Value.point) > stepLength && leg.Lerp >= 1f)
            {
                leg.Lerp = 0;
                leg.OldPosition  = leg.NewPosition;
                leg.NewPosition = hit.Value.point;
            }
        }
            
           
    }

    public RaycastHit? ExecuteGroundedForLeg(LegAnimator leg)
    {
        Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        Ray ray = new Ray(worldOrigin, -_rigidbody.transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            _hitList.Add(hit);
        }

        return hit;

    }
    
    /*
     * Function that calculates the Idle position for a leg (using raycasts) and triggers
     * the repositioning
     */
    public void ExecuteReturnToIdle(LegAnimator leg)
    {
        Transform reference = _rigidbody.transform;
        
        Quaternion bodyRot = reference.rotation;
        Vector3 fullOffset = bodyRot * leg.RelativePosition;
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset;
        //Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        
        Ray ray = new Ray(worldOrigin, -_rigidbody.transform.up);
        if (Physics.Raycast(ray, out RaycastHit hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            if (leg.Lerp >= 1f)
            {
                leg.Lerp = 0;
                leg.OldPosition  = leg.NewPosition;
                leg.NewPosition = hit.point;
            }
            _hitList.Add(hit);
        }
    }
    

    private Vector3 ComputeLegPositionForStep(LegAnimator leg)
    {
        
        Transform reference = _rigidbody.transform;
        
        Quaternion bodyRot = reference.rotation;
        Vector3 fullOffset = bodyRot * leg.RelativePosition;
        Vector3 anticipation = new Vector3(_movementDelta.x, _movementDelta.y, _movementDelta.z) * stepAnticipationMultiplier;
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset 
                              + anticipation;
        Debug.DrawLine(reference.position, reference.position + anticipation, Color.green);
        return worldOrigin;
    }
    
    /*
     * Clears the last FixedUpdate frame hit list, preparing the logic
     * to the next raycast
     */
    public void FlushRaycasts()
    {
        _hitList.Clear();
    }
    
    
    public Vector3 GetMovementDelta()
    {
        return _movementDelta;
    }

    public List<RaycastHit> GetHitList()
    {
        return _hitList;
    }
   
    public void SetLegs(List<LegAnimator> legs)
    {
        _legs = legs;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var leg in _legs)
        {
         
            Vector3 origin = ComputeLegPositionForStep(leg);
            Vector3 direction = -_rigidbody.transform.up.normalized;

            // Draw full test ray
            Gizmos.DrawLine(origin, origin + direction * jumpHeight);
            // Draw every test ray in Play mode
            Debug.DrawRay(origin, direction * jumpHeight, Color.yellow);
            // Show hit point
            if (Physics.Raycast(origin, direction, out var hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.color = Color.yellow;  // reset for next leg
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(leg.NewPosition, 0.10f);
            }
        }
    }
  
    
    
}
