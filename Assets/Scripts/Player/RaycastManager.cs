using System;
using System.Collections.Generic;
using UnityEngine;

public class RaycastManager : MonoBehaviour
{

    [SerializeField] private float stepLength = 2.0f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private string terrainLayer;
    [SerializeField] private float stepAnticipationMultiplier = 25f;
    
    private Rigidbody _rigidbody;
    private CharacterController _characterController;
    private List<RaycastHit> _hitList = new List<RaycastHit>();
    private List<LegAnimator> _legs = new List<LegAnimator>();

    
    private Vector3 _lastRootPos;
    private Vector3 _movementDelta;
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        _lastRootPos = _rigidbody.position;
    }

    void FixedUpdate()
    {
        _movementDelta = transform.position - _lastRootPos;
        _lastRootPos = _rigidbody.position;
    }

    public void SetLegs(List<LegAnimator> legs)
    {
        _legs = legs;
    }

    public List<RaycastHit> GetHitList()
    {
        return _hitList;
    }
    public void FlushRaycasts()
    {
        _hitList.Clear();
    }
    
    public void ExecuteStepForLeg(LegAnimator leg)
    {
        // Enhance the raycast in the movement direction in order for the leg to anticipate the body movement
        Vector3 worldOffset     = _rigidbody.rotation * leg.RelativePosition;
        Vector3 relativePos     = _rigidbody.position 
                                  + worldOffset + _movementDelta*stepAnticipationMultiplier;            
        Ray ray = new Ray(relativePos, Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            // Verify if the hit distance is greater than the step length
            if (Vector3.Distance(leg.NewPosition, hit.point) > stepLength && leg.Lerp >= 1f)
            {
                leg.Lerp = 0;
                leg.OldPosition  = leg.NewPosition;
                leg.NewPosition = hit.point;
            }
            _hitList.Add(hit);
        }
    }
    
    public void ExecuteReturnToIdle(LegAnimator leg)
    {
        // Enhance the raycast in the movement direction in order for the leg to anticipate the body movement
        Vector3 worldOffset     = _rigidbody.rotation * leg.RelativePosition;
        Vector3 relativePos     = _rigidbody.position 
                                  + worldOffset;            
        Ray ray = new Ray(relativePos, Vector3.down);
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
    
    
 
  private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var leg in _legs)
        {
            Vector3 worldOffset     = _rigidbody.rotation * leg.RelativePosition;
            Vector3 origin    = _rigidbody.position 
                                + worldOffset + _movementDelta;
            Vector3 direction = Vector3.down;

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
