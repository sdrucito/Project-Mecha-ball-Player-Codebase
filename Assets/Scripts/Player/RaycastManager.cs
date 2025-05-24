using System;
using System.Collections.Generic;
using System.Linq;
using Player.Animation;
using UnityEngine;
using UnityEngine.Serialization;

public class RaycastManager : MonoBehaviour, IFixedUpdateObserver
{
    public int FixedUpdatePriority { get; set; }

    [SerializeField] private float stepLength = 5.0f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float stepAnticipationMultiplier = 25f;
    [SerializeField] private float rotAnticipationMultiplier = 0.5f;
    [SerializeField] private string terrainLayer;
    [SerializeField] private float maxMovementMagnitude = 1f;
    private Rigidbody _rigidbody;
    private readonly Dictionary<string, RaycastHit> _hitList = new Dictionary<string, RaycastHit>();
    private List<LegAnimator> _legs = new List<LegAnimator>();
    
    public Vector3 MovementDelta {get; set;}
    public Quaternion RotationDelta {get; set;}
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        FixedUpdatePriority = 1;
    }



    /*
     * When enabled, reset the movement delta
     */
    public void ResetMovementDelta()
    {
        MovementDelta = Vector3.zero;
        RotationDelta = Quaternion.identity;
    }

    /*
     * Computes, when active, the movement captured between the last FixedUpdate
     * It is used by all components that need to know the instantaneous velocity
     */
    public void ObservedFixedUpdate()
    {
        /*
        MovementDelta = _rigidbody.position - _lastRootPos;
        _rotationDelta = _rigidbody.rotation * Quaternion.Inverse(_lastRotation);

        _lastRotation = _rigidbody.rotation;
        _lastRootPos = _rigidbody.position;
        // Clamp movement delta to avoid strange behaviors
        MovementDelta = Vector3.ClampMagnitude(MovementDelta, maxMovementMagnitude);
        */
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
            Debug.DrawLine(leg.NewPosition, hit.point, Color.blue);

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
        Vector3 anticipation = new Vector3(MovementDelta.x, MovementDelta.y, MovementDelta.z) * stepAnticipationMultiplier;
        
        
        // Rotation anticipation
        Vector3 rotatedOffset = RotationDelta * fullOffset;
        Vector3 rotationalAnt = (rotatedOffset - fullOffset) * rotAnticipationMultiplier;

        //anticipation = Vector3.ClampMagnitude(anticipation, stepLength);
        Vector3 worldOrigin = _rigidbody.transform.position 
                              + fullOffset + rotationalAnt;
        Debug.DrawLine(worldOrigin, worldOrigin + anticipation, Color.red);
        worldOrigin += anticipation;
        //Debug.DrawLine(reference.position, reference.position + anticipation, Color.green);
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
        return MovementDelta;
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
    


    private void OnEnable()
    {
        FixedUpdateManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        FixedUpdateManager.Instance?.Unregister(this);
        
    }
}
