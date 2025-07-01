using System;
using System.Collections.Generic;
using System.Linq;
using Player.Animation;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Manages raycasting for leg IK stepping: tracks movement/rotation deltas,
/// performs terrain checks per leg, and caches raycast hits each physics frame.
/// </summary>
public class RaycastManager : MonoBehaviour
{
    #region Serialized Fields
    [Header("Step & Jump Settings")]
    [SerializeField] private float stepLength = 5.0f;
    [SerializeField] private float jumpHeight = 2f;
    [Header("Anticipation Multipliers")]
    [SerializeField] private float stepAnticipationMultiplier = 25f;
    [SerializeField] private float rotAnticipationMultiplier = 0.5f;
    [Header("Terrain Layer")]
    [SerializeField] private string terrainLayer;
    [SerializeField] private float maxMovementMagnitude = 1f;

    [Header("Foot Plane Rotation")] 
    [SerializeField] private int footPlaneSteps = 3;

    [SerializeField] private int footPlaneRotationAngle = 20;
    #endregion

    #region Private Fields
    private Rigidbody _rigidbody;
    private readonly Dictionary<string, RaycastHit> _hitList = new Dictionary<string, RaycastHit>();
    private List<LegAnimator> _legs = new List<LegAnimator>();
    #endregion

    #region Public Properties
    public int FixedUpdatePriority { get; set; }
    public Vector3 MovementDelta { get; set; }
    public Quaternion RotationDelta { get; set; }
    #endregion

    #region Unity Callbacks
    /// <summary>
    /// Cache Rigidbody reference and set fixed update execution order.
    /// </summary>
    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        FixedUpdatePriority = 1;
    }

    /// <summary>
    /// Draws debug gizmos for each leg's raycast and hit points in the editor.
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var leg in _legs)
        {
            Vector3 origin = ComputeLegPositionForStep(leg, 0.0f);
            Vector3 direction = -_rigidbody.transform.up;

            Gizmos.DrawLine(origin, origin + direction * jumpHeight);
            Debug.DrawRay(origin, direction * jumpHeight, Color.yellow);

            if (_hitList.TryGetValue(leg.Name, out var hit))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.DrawSphere(leg.NewPosition, 0.10f);
                Gizmos.color = Color.yellow;
            }
        }
    }
    #endregion

    #region Public API
    /// <summary>
    /// Resets movement and rotation deltas to zero.
    /// </summary>
    public void ResetMovementDelta()
    {
        MovementDelta = Vector3.zero;
        RotationDelta = Quaternion.identity;
    }

    /// <summary>
    /// Returns whether a given leg currently has a valid ground hit cached.
    /// </summary>
    public bool IsLegGrounded(string legName)
    {
        return _hitList.ContainsKey(legName);
    }

    /// <summary>
    /// Casts a downward ray at leg's anticipated position and caches hit for grounding logic.
    /// </summary>
    public void ExecuteGroundedForLeg(LegAnimator leg)
    {
        Vector3 origin = ComputeLegPositionForStep(leg,0.0f);

        var ray = new Ray(origin, -_rigidbody.transform.up);
        if (Physics.Raycast(ray, out var hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            _hitList.TryAdd(leg.Name, hit);
            if (leg.Lerp < 1f)
            {
                // If step is executing update step
                leg.NewPosition = hit.point;
            }
        }
        else
        {
            //float angle = -footPlaneSteps / 2 * footPlaneRotationAngle;
            SweepGroundedForLeg(leg);
        }
    }

    private void SweepGroundedForLeg(LegAnimator leg)
    {
        Vector3 origin;
        Ray ray;
        RaycastHit hit;
        for (int i = 0; i < footPlaneSteps; i++)
        {
            float stepIndex = i - (footPlaneSteps - 1) * 0.5f;
            float angle = stepIndex * footPlaneRotationAngle;
            origin = ComputeLegPositionForStep(leg, angle - 25.0f);
            Vector3 legDirection = origin - _rigidbody.position;
            // double‐cross gives you the projection of down onto the plane ⟂ legDir
            Vector3 newDownDirection = Vector3.Cross(legDirection, Vector3.Cross(-_rigidbody.transform.up, legDirection)).normalized;
            Debug.DrawLine(origin, origin + newDownDirection * jumpHeight, Color.blue, 1f);
            //Debug.DrawLine(origin, origin -_rigidbody.transform.up * jumpHeight, Color.blue, 1.0f);
            ray = new Ray(origin, newDownDirection);
            if (Physics.Raycast(ray, out hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
            {
                _hitList.TryAdd(leg.Name, hit);
                break;
            } 
        }
    }
    
    public bool GetLegHit(string legName, out RaycastHit hit)
    {
        if (_hitList.TryGetValue(legName, out hit))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Plans a step: if leg is fully planted and too far from cached hit, updates its target.
    /// </summary>
    public bool ExecuteStepForLeg(LegAnimator leg)
    {
        if (_hitList.TryGetValue(leg.Name, out var hit))
        {
            //Debug.DrawLine(leg.NewPosition, hit.point, Color.blue);
            if (Vector3.Distance(leg.NewPosition, hit.point) > stepLength && leg.Lerp >= 1f)
            {
                leg.Lerp = 0f;
                leg.OldPosition = leg.NewPosition;
                leg.NewPosition = hit.point;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Repositions leg to idle stance by raycasting directly under its hip offset.
    /// </summary>
    public void ExecuteReturnToIdle(LegAnimator leg)
    {
        var refTrans = _rigidbody.transform;
        Vector3 origin = refTrans.position + refTrans.rotation * leg.RelativePosition;
        var ray = new Ray(origin, -_rigidbody.transform.up);
        if (Physics.Raycast(ray, out var hit, jumpHeight, LayerMask.GetMask(terrainLayer)) && leg.Lerp >= 1f)
        {
            leg.Lerp = 0f;
            leg.OldPosition = leg.NewPosition;
            leg.NewPosition = hit.point;
        }
    }

    /// <summary>
    /// Immediately sets leg's transform and positions to the ground hit point.
    /// </summary>
    public void ExecuteResetPosition(LegAnimator leg)
    {
        var refTrans = _rigidbody.transform;
        Vector3 origin = refTrans.position + refTrans.rotation * leg.RelativePosition;
        var ray = new Ray(origin, -_rigidbody.transform.up);
        if (Physics.Raycast(ray, out var hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
        {
            leg.Transform.position = hit.point;
            leg.OldPosition = hit.point;
            leg.NewPosition = hit.point;
            leg.Lerp = 1f;
        }
    }

    /// <summary>
    /// Clears all cached raycast hits for the next physics frame.
    /// </summary>
    public void FlushRaycasts()
    {
        _hitList.Clear();
    }

    /// <summary>
    /// Returns the last computed movement delta vector.
    /// </summary>
    public Vector3 GetMovementDelta() => MovementDelta;

    /// <summary>
    /// Returns a list of all current ground hit points.
    /// </summary>
    public List<RaycastHit> GetHitList() => _hitList.Values.ToList();

    /// <summary>
    /// Provides the set of legs managed by this raycast manager for gizmos.
    /// </summary>
    public void SetLegs(List<LegAnimator> legs)
    {
        _legs = legs;
    }
    #endregion

    #region Private Helpers
    /// <summary>
    /// Calculates world-space origin for a leg raycast based on anticipation and rotation.
    /// </summary>
    private Vector3 ComputeLegPositionForStep(LegAnimator leg, float tiltAngleDeg)
    {
        var body = _rigidbody.transform;
        Vector3 offset = body.rotation * leg.RelativePosition;
        Vector3 anticipation = MovementDelta * stepAnticipationMultiplier;
        // Assume RotationDelta is a quaternion representing some rotation delta
        RotationDelta.ToAngleAxis(out float angle, out Vector3 axis);

        float anticipatedAngle = angle * rotAnticipationMultiplier;

        Quaternion anticipatedRotation = Quaternion.AngleAxis(anticipatedAngle, axis);

        Vector3 rotatedOffset = anticipatedRotation * offset;
        //Vector3 rotAnt = (rotatedOffset - offset) * rotAnticipationMultiplier;
        Vector3 origin = body.position + rotatedOffset + anticipation;
        
        // Compute relative rotation of the projection
        Vector3 v = rotatedOffset + anticipation;
        Vector3 newAxis = Vector3.Cross(v, body.up).normalized;
        Vector3 tiltedV = Quaternion.AngleAxis(tiltAngleDeg, newAxis) * v;
        Vector3 finalPos = body.position + tiltedV;
        Debug.DrawLine(body.position, finalPos, Color.red);

        return finalPos;
    }
    #endregion
}
