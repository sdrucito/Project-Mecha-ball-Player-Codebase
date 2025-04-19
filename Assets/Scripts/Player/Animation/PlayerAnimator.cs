using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[System.Serializable]
public class LegAnimator
{
    
    public Transform Transform;
    public float Lerp;
    public Vector3 NewPosition;
    public Vector3 OldPosition;
    public SecondOrderDynamics SecondOrderDynamics;
    public Vector3 RelativePosition;
    public LegAnimator(Transform transform, float lerp, Vector3 newPosition, Vector3 oldPosition, SecondOrderDynamics secondOrderDynamics, Vector3 relativePosition)
    {
        Transform = transform;
        Lerp = lerp;
        NewPosition = newPosition;
        OldPosition = oldPosition;
        SecondOrderDynamics = secondOrderDynamics;
        RelativePosition = relativePosition;
    }
}

/*
 * Component that manages the procedural animation for the player movement
 */
public class PlayerAnimator : MonoBehaviour
{

    [SerializeField, Range(0,3)] private float f;
    [SerializeField, Range(0,3)] private float z;
    [SerializeField, Range(0,3)] private float r;

    // Reference to IK position for each leg
    [SerializeField] private Transform frontLeftFoot;
    [SerializeField] private Transform frontRightFoot;
    [SerializeField] private Transform rearLeftFoot;
    [SerializeField] private Transform rearRightFoot;

    // One resolver of each 
    [SerializeField] SecondOrderDynamics secondOrderDynamics_flF;
    [SerializeField] SecondOrderDynamics secondOrderDynamics_frF;
    [SerializeField] SecondOrderDynamics secondOrderDynamics_rlF;
    [SerializeField] SecondOrderDynamics secondOrderDynamics_rrF;

    [SerializeField] private float stepLength = 2.0f;
    [SerializeField] private float stepHeight = 10.0f;
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float stepSpeed = 2.0f;
    [SerializeField] private float footHeight = 1.5f;

    [SerializeField] private string terrainLayer;

    private Rigidbody _rigidbody;
    private LegAnimator _frontLeftFootAnim;
    private LegAnimator _frontRightFootAnim;
    private LegAnimator _rearLeftFootAnim;
    private LegAnimator _rearRightFootAnim;
    
    private List<LegAnimator> _legs = new List<LegAnimator>();
    
    enum StepGroup
    {
        Idle,
        GroupA,
        GroupB,
    };
    
    private List<LegAnimator> groupALegs = new List<LegAnimator>();
    private List<LegAnimator> groupBLegs = new List<LegAnimator>();
    
    private StepGroup _currentGroup = StepGroup.Idle;
    private bool _wasMoving = false;
    
    private Player _player;
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _player = GetComponent<Player>();
        // Initialize the second order resolvers for each foot
        secondOrderDynamics_flF.Initialize(f, z, r, frontLeftFoot.position);
        secondOrderDynamics_frF.Initialize(f, z, r, frontRightFoot.position);
        secondOrderDynamics_rlF.Initialize(f, z, r, rearLeftFoot.position);
        secondOrderDynamics_rrF.Initialize(f, z, r, rearRightFoot.position);

        _frontLeftFootAnim = new LegAnimator(frontLeftFoot, 1f, frontLeftFoot.position, frontLeftFoot.position, secondOrderDynamics_flF, frontLeftFoot.position-_rigidbody.position);
        _frontRightFootAnim = new LegAnimator(frontRightFoot, 1f, frontRightFoot.position, frontRightFoot.position, secondOrderDynamics_frF, frontRightFoot.position-_rigidbody.position );
        _rearLeftFootAnim = new LegAnimator(rearLeftFoot, 1f, rearLeftFoot.position, rearLeftFoot.position, secondOrderDynamics_rlF, rearLeftFoot.position-_rigidbody.position);
        _rearRightFootAnim = new LegAnimator(rearRightFoot, 1f, rearRightFoot.position, rearRightFoot.position, secondOrderDynamics_rrF, rearRightFoot.position-_rigidbody.position);
        
        _legs.Add(_frontLeftFootAnim);
        _legs.Add(_frontRightFootAnim);
        _legs.Add(_rearLeftFootAnim);
        _legs.Add(_rearRightFootAnim);
        
        groupALegs.Add(_frontLeftFootAnim);
        groupALegs.Add(_rearRightFootAnim);
        
        groupBLegs.Add(_frontRightFootAnim);
        groupBLegs.Add(_rearLeftFootAnim);
    }

    void FixedUpdate()
    {
        // Verify if player is grounded

        // Call the update for each leg and set the new IK position
        bool isMoving = transform.hasChanged;
        transform.hasChanged = false;
        // Detect fresh start from idle
        if (isMoving && !_wasMoving && _currentGroup == StepGroup.Idle)
        {
            _currentGroup = StepGroup.GroupA;
            StartStepForGroup(StepGroup.GroupA);
        }
    
        // GroupA finishes, go to GroupB
        if (_currentGroup == StepGroup.GroupA && IsMovementGroupFinished(StepGroup.GroupA))
        {
            _currentGroup = StepGroup.GroupB;
            StartStepForGroup(StepGroup.GroupB);
        }
    
        // GroupB finishes, go to GroupA again
        if (_currentGroup == StepGroup.GroupB && IsMovementGroupFinished(StepGroup.GroupB))
        {
            _currentGroup = StepGroup.GroupA;
            StartStepForGroup(StepGroup.GroupA);
        }
    
        if (!isMoving)
            _currentGroup = StepGroup.Idle;
    
        _wasMoving = isMoving;
    
        MoveLegStep(ref _frontLeftFootAnim);
        MoveLegStep(ref _frontRightFootAnim);
        MoveLegStep(ref _rearLeftFootAnim);
        MoveLegStep(ref _rearRightFootAnim);
        
        
        
    }

    void SnapFootToGround(ref LegAnimator legAnimator)
    {
        
    }

    bool IsMovementGroupFinished(StepGroup stepGroup)
    {
        switch (stepGroup)
        {
            case StepGroup.Idle:
                return true;
            case StepGroup.GroupA:
                return groupALegs.All(leg => leg.Lerp >= 1f);
            case StepGroup.GroupB:
                return groupBLegs.All(leg => leg.Lerp >= 1f);
        }

        return true;
    }

    void MoveLegStep(ref LegAnimator legAnimator)
    {
        
        if (legAnimator.Lerp < 1f)
        {
            legAnimator.Lerp = Mathf.Min(legAnimator.Lerp + Time.deltaTime * stepSpeed, 1f);
            float verticalOffset = 0f;
            verticalOffset = Mathf.Sin(legAnimator.Lerp * Mathf.PI) * stepHeight;
        
            // Second order function that commands the xz plain movement
            Vector3 planarPos = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
           
            legAnimator.Transform.position = new Vector3(planarPos.x, legAnimator.NewPosition.y + verticalOffset, planarPos.z);
        }
        else
        {
            legAnimator.Transform.position = legAnimator.OldPosition;
        }

        
    }
    
    void MoveLegReturnToBody(ref LegAnimator legAnimator)
    {
        legAnimator.Transform.position = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
    }


    void StartStepForGroup(StepGroup group)
    {
        List<LegAnimator> legToMove = (group == StepGroup.GroupA) ? groupALegs : groupBLegs;
        int mask = LayerMask.GetMask(terrainLayer);

        for (int i = 0; i < legToMove.Count; i++)
        {
            LegAnimator leg = legToMove[i];
            Vector3 relativePos = _rigidbody.position + leg.RelativePosition;
            Ray ray = new Ray(relativePos, Vector3.down);
            if (Physics.Raycast(ray, out RaycastHit hit, jumpHeight, LayerMask.GetMask(terrainLayer)))
            {
                // Verify if the hit distance is greater than the step length
                if (Vector3.Distance(leg.NewPosition, hit.point) > stepLength && leg.Lerp >= 1f)
                {
                    leg.Lerp = 0;
                    leg.NewPosition = hit.point;
                    leg.OldPosition = leg.NewPosition;
                    leg.NewPosition.y += footHeight;
                    leg.OldPosition.y += footHeight;
                }
            }

            legToMove[i] = leg;
        }
    }
    /*
void MoveLeg(LegAnimator leg)
    {
        Vector3 origin = _rigidbody.position + leg.RelativePosition;
        int mask = LayerMask.GetMask(terrainLayer);
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, jumpHeight, mask))
        {
            if(Vector3.Distance(leg.NewPosition, hit.point) > stepLength)
            {
                /*
                 Debug.Log("Starting new step");
                Debug.Log("New pos:" + leg.NewPosition);
                Debug.Log("Hit point:" + hit.point);
                Debug.Log("Computed distance: " + Vector3.Distance(leg.NewPosition, hit.point));
                 * /
                
                leg.NewPosition = hit.point;         
                leg.Lerp = 0f;
            }
        }

        if (leg.Lerp < 1f)
        {
            Vector3 planarPos = leg.SecondOrderDynamics.UpdatePosition(Time.deltaTime, leg.NewPosition);

            Debug.Log("Lerping " + leg.Lerp);
            Vector3 footPosition = Vector3.Lerp(leg.OldPosition, leg.NewPosition, leg.Lerp);
            footPosition.y += Mathf.Sin(leg.Lerp * Mathf.PI) * stepHeight;
            leg.Lerp += Time.deltaTime * stepSpeed;
            leg.Transform.position = new Vector3(planarPos.x, footPosition.y, planarPos.z);
        }
        else
        {
            leg.OldPosition = leg.NewPosition;
        }
        
        
        

    }

     */
    
    
    [ExecuteInEditMode]
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var leg in _legs)
        {
            Vector3 origin    = _rigidbody.position
                                + leg.RelativePosition;
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
