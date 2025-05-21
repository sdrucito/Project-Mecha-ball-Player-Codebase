using System;
using System.Collections.Generic;
using System.Linq;
using Player.Animation;
using UnityEngine;

public class RaycastManager : MonoBehaviour
{

    [SerializeField] private float stepLength = 5.0f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float stepAnticipationMultiplier = 25f;
    [SerializeField] private float rotAnticipationMultiplier = 0.5f;
    [SerializeField] private string terrainLayer;
    [SerializeField] private float maxMovementMagnitude = 1f;
    private Rigidbody _rigidbody;
    private readonly Dictionary<string, RaycastHit> _hitList = new Dictionary<string, RaycastHit>();
    private List<LegAnimator> _legs = new List<LegAnimator>();
    
    private Vector3 _lastRootPos;
    private Vector3 _movementDelta;
    private Quaternion _lastRotation;
    private Quaternion _rotationDelta;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _lastRootPos = _rigidbody.position;
        _lastRotation = _rigidbody.rotation;
    }

    /*
     * When enabled, reset the movement delta
     */
    public void ResetMovementDelta()
    {
        _movementDelta = Vector3.zero;
        _lastRootPos = _rigidbody.position;
    }

    /*
     * Computes, when active, the movement captured between the last FixedUpdate
     * It is used by all components that need to know the instantaneous velocity
     */
    void FixedUpdate()
    {
        _movementDelta = _rigidbody.position - _lastRootPos;
        _rotationDelta = _rigidbody.rotation * Quaternion.Inverse(_lastRotation);

        _lastRotation = _rigidbody.rotation;
        _lastRootPos = _rigidbody.position;
        // Clamp movement delta to avoid strange behaviors
        _movementDelta = Vector3.ClampMagnitude(_movementDelta, maxMovementMagnitude);
    }


    public bool IsLegGrounded(string legName)
    {
        if (_hitList.ContainsKey(legName))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public void ExecuteGroundedForLeg(LegAnimator leg)
    {
        Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        Ray ray = new Ray(worldOrigin, -_rigidbody.transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            _hitList.TryAdd(leg.Name, hit);
        }
        
    }
    
    public void ExecuteGroundedForOverLimitSlope(LegAnimator leg)
    {
        // 1. origin of the ray
        Vector3 worldOrigin    = ComputeLegPositionForOverSlope(leg);
        Vector3 bodyCenter     = _rigidbody.transform.position;
    
        // 2. pure downward direction
        Vector3 downDir        = -_rigidbody.transform.up;
    
        // 3. horizontal “side” direction from body center to leg
        Vector3 sideDir = (bodyCenter   - worldOrigin);
        sideDir.y              = 0;               // flatten to horizontal
        if (sideDir.sqrMagnitude < 1e-6f)
            sideDir = _rigidbody.transform.right; // fallback if leg is exactly under center
        sideDir.Normalize();
    
        // 4. mix them at 45°: rotate 'downDir' toward 'sideDir' by 45° (in radians)
        float   angleRad       = 45f * Mathf.Deg2Rad;
        Vector3 tiltedDir      = Vector3.RotateTowards(downDir, sideDir, angleRad, 0f).normalized;
        Debug.DrawRay(
            worldOrigin,
            tiltedDir * jumpHeight,
            Color.red,
            /* duration */ 0.1f,
            /* depthTest */ true
        );
        // 5. cast the ray
        Ray       ray           = new Ray(worldOrigin, tiltedDir);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, jumpHeight*2, LayerMask.GetMask(terrainLayer)))
        {
            _hitList.TryAdd(leg.Name, hit);
        }
    }
    
    /*
     * Function that verifies if the player can move basing on the anticipation for leg
     * It calculates, for a leg, the next position in the step and triggers the step movement
     */
    public bool ExecuteStepForLeg(LegAnimator leg)
    {

        //Vector3 relativePos = pivot + horizontalOffset + anticipation;
        //relativePos.y = _rigidbody.position.y;
        Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        Ray ray = new Ray(worldOrigin, -_rigidbody.transform.up);
        RaycastHit hit;
        bool res = _hitList.TryGetValue(leg.Name, out hit);
        // Verify if the hit distance is greater than the step length
        if (res)
        {
            if (Vector3.Distance(leg.NewPosition, hit.point) > stepLength && leg.Lerp >= 1f)
            {
                leg.Lerp = 0;
                leg.OldPosition  = leg.NewPosition;
                leg.NewPosition = hit.point;
            }

            return true;
        }
        else
        {
            return false;
        }
        
    }
    
    
    /*
     *
     public void ExecuteStepForLeg(LegAnimator leg)
    {

        //Vector3 relativePos = pivot + horizontalOffset + anticipation;
        //relativePos.y = _rigidbody.position.y;
        Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        Ray ray = new Ray(worldOrigin, -_rigidbody.transform.up);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            // Verify if the hit distance is greater than the step length
            if (Vector3.Distance(leg.NewPosition, hit.point) > stepLength && leg.Lerp >= 1f)
            {
                leg.Lerp = 0;
                leg.OldPosition  = leg.NewPosition;
                leg.NewPosition = hit.point;
            }
        }
            
           
    }
     */
    
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
        }
    }
    
    /*
    public void ExecuteResetPosition(LegAnimator leg)
    {
        Transform reference = _rigidbody.transform;
        
        Quaternion bodyRot = reference.rotation;
        Vector3 fullOffset = bodyRot * leg.RelativePosition;
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset;
        //Vector3 worldOrigin = ComputeLegPositionForStep(leg);
        RaycastHit hit;
        bool res = _hitList.TryGetValue(leg.Name, out hit);
        if (res)
        {
            leg.Transform.position = hit.point;
            leg.OldPosition = hit.point;
            leg.NewPosition = hit.point;
            leg.Lerp = 1f;
        }
    }
    */
    
    /*
     * Function that calculates the position of legs after a reset of the walking animator and resets it
     */
    public void ExecuteResetPosition(LegAnimator leg)
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
            leg.Transform.position = hit.point;
            leg.OldPosition = hit.point;
            leg.NewPosition = hit.point;
            leg.Lerp = 1f;
        }
    }
     

    private Vector3 ComputeLegPositionForStep(LegAnimator leg)
    {
        
        Transform reference = _rigidbody.transform;
        
        Quaternion bodyRot = reference.rotation;
        Vector3 fullOffset = bodyRot * leg.RelativePosition;
        // Linear movement anticipation
        Vector3 anticipation = new Vector3(_movementDelta.x, _movementDelta.y, _movementDelta.z) * stepAnticipationMultiplier;
        
        // Rotation anticipation
        Vector3 rotatedOffset = _rotationDelta * fullOffset;
        Vector3 rotationalAnt = (rotatedOffset - fullOffset) * rotAnticipationMultiplier;

        //anticipation = Vector3.ClampMagnitude(anticipation, stepLength);
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset 
                              + anticipation + rotationalAnt;
        //Debug.DrawLine(reference.position, reference.position + anticipation, Color.green);
        return worldOrigin;
    }
    
    private Vector3 ComputeLegPositionForOverSlope(LegAnimator leg)
    {
        
        Transform reference = _rigidbody.transform;
        
        Quaternion bodyRot = reference.rotation;
        Vector3 fullOffset = bodyRot * leg.RelativePosition;
        Vector3 anticipation = new Vector3(_movementDelta.x, _movementDelta.y, _movementDelta.z) * stepAnticipationMultiplier * 1.5f; // Add more anticipation
        //anticipation = Vector3.ClampMagnitude(anticipation, stepLength);
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset 
                              + anticipation;
        Debug.DrawLine(reference.position, worldOrigin, Color.green);
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
        return _hitList.Values.ToList();
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
            RaycastHit hit;
            // Show hit point
            bool res = _hitList.TryGetValue(leg.Name, out hit);
            if (res)
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
    
    /*
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
    }*/
  
    
    
}
