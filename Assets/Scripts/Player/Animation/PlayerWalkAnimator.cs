using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;


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
public class PlayerWalkAnimator : MonoBehaviour
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

    [SerializeField] private float stepHeight = 10.0f;
    [SerializeField] private float stepSpeed = 2.0f;
    [SerializeField] private float footHeight = 1.5f;


    [SerializeField] private RaycastManager raycast;
    [SerializeField] private Rig legRig;
    private bool _isWalking = true;
    
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
        _rigidbody = GetComponentInParent<Rigidbody>();
        _player = GetComponentInParent<Player>();
        
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
        if (_isWalking)
        {
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

            if (IsStopped(isMoving))
            {
                _currentGroup = StepGroup.Idle;
                ReturnLegToIdle();
            }
    
            _wasMoving = isMoving;
    
            MoveLegStep(_frontLeftFootAnim);
            MoveLegStep(_frontRightFootAnim);
            MoveLegStep(_rearLeftFootAnim);
            MoveLegStep(_rearRightFootAnim);

            if (raycast)
            {
                raycast.SetLegs(_legs);
            }

        }
        
    }

    bool IsStopped(bool newMoving)
    {
        return !newMoving && _currentGroup != (StepGroup.Idle) && IsMovementGroupFinished(StepGroup.GroupA) &&
               IsMovementGroupFinished(StepGroup.GroupB);
    }
    void ReturnLegToIdle()
    {
        for (int i = 0; i < _legs.Count; i++)
        {
            if (raycast)
            {
                raycast.ExecuteReturnToIdle(_legs[i]);
            }
           
        }
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

    void MoveLegStep(LegAnimator legAnimator)
    {
        
        if (legAnimator.Lerp < 1f)
        {
            legAnimator.Lerp = Mathf.Min(legAnimator.Lerp + Time.deltaTime * stepSpeed, 1f);
            float verticalOffset = 0f;
            verticalOffset = Mathf.Sin(legAnimator.Lerp * Mathf.PI) * stepHeight;
        
            // Second order function that commands the xz plain movement
            Vector3 planarPos = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
           
            legAnimator.Transform.position = new Vector3(planarPos.x, legAnimator.NewPosition.y + verticalOffset, planarPos.z);
            if (legAnimator.Lerp >= 1f)
            {
                legAnimator.OldPosition = legAnimator.NewPosition;
            }
            //Debug.Log("NewPosition: " + legAnimator.NewPosition);

        }
        else
        {
            legAnimator.Transform.position = legAnimator.OldPosition;
            //Debug.Log("OldPosition: " + legAnimator.OldPosition);
        }

        
    }
    
    void MoveLegReturnToBody(ref LegAnimator legAnimator)
    {
        legAnimator.Transform.position = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
    }


    void StartStepForGroup(StepGroup group)
    {
        List<LegAnimator> legToMove = (group == StepGroup.GroupA) ? groupALegs : groupBLegs;

        for (int i = 0; i < legToMove.Count; i++)
        {
            if (raycast)
            {
                raycast.ExecuteStepForLeg(legToMove[i]);
            }
           
        }
    }

    private void OnEnable()
    {
        raycast.enabled = true;
        StartCoroutine(DelaySetWeight());
    }

    private void OnDisable()
    {
        raycast.enabled = false;
        legRig.weight = 0.0f;
    }
    
    private IEnumerator DelaySetWeight()
    {
        yield return null;          
        legRig.weight = 1.0f;
    }

    public void SetWalking(bool walking)
    {
        _isWalking = walking;
    }

    
    
    
}
