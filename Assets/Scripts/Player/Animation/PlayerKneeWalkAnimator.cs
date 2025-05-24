using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;

namespace Player.Animation
{


/*
 * Component that manages the procedural animation for the player movement
 */
    public class PlayerKneeWalkAnimator : MonoBehaviour, IFixedUpdateObserver
    {

        public int FixedUpdatePriority { get; set; }
        
        [SerializeField, Range(0,3)] private float f;
        [SerializeField, Range(0,3)] private float z;
        [SerializeField, Range(0,3)] private float r;
        [SerializeField] private AnimationCurve legAnimationCurve;
        [SerializeField] private Transform center;
        // Reference to IK position for each leg
        [SerializeField] private Transform frontLeftFoot;
        [SerializeField] private Transform frontRightFoot;
        [SerializeField] private Transform rearLeftFoot;
        [SerializeField] private Transform rearRightFoot;

        // One resolver of each 
        [SerializeField] SecondOrderDynamics secondOrderDynamicsFlF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsFrF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsRlF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsRrF;

        [SerializeField] private float stepHeight = 10.0f;
        [SerializeField] private float stepSpeed = 2.0f;
        [SerializeField] private float footHeight = 1.5f;

        [SerializeField] private Rig legRig;
    
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
            Floating,
            Opening,
        };
    
        private readonly List<LegAnimator> _groupALegs = new List<LegAnimator>();
        private readonly List<LegAnimator> _groupBLegs = new List<LegAnimator>();
    
        private StepGroup _currentGroup = StepGroup.Idle;
        private bool _wasMoving = false;
        private bool _wasGrounded = false;
        private Vector3 _lastPosition;
        public Action OnOpenFinished;

        public Vector3 MovementDelta {get; set;}
        public Quaternion RotationDelta {get; set;}
        private void Awake()
        {
            FixedUpdatePriority = 1;
        }

        void Start()
        {

            // Initialize the second order resolvers for each foot
            secondOrderDynamicsFlF.Initialize(f, z, r, frontLeftFoot.position);
            secondOrderDynamicsFrF.Initialize(f, z, r, frontRightFoot.position);
            secondOrderDynamicsRlF.Initialize(f, z, r, rearLeftFoot.position);
            secondOrderDynamicsRrF.Initialize(f, z, r, rearRightFoot.position);

            Vector3 relativePos = frontLeftFoot.position - center.position;
            _frontLeftFootAnim = new LegAnimator(frontLeftFoot, 1f, frontLeftFoot.position, frontLeftFoot.position, secondOrderDynamicsFlF, relativePos, "fl");
            relativePos = frontRightFoot.position - Player.Instance.Rigidbody.position;
            _frontRightFootAnim = new LegAnimator(frontRightFoot, 1f, frontRightFoot.position, frontRightFoot.position, secondOrderDynamicsFrF, relativePos, "fr");
            relativePos = rearLeftFoot.position - Player.Instance.Rigidbody.position;
            _rearLeftFootAnim = new LegAnimator(rearLeftFoot, 1f, rearLeftFoot.position, rearLeftFoot.position, secondOrderDynamicsRlF, relativePos, "rl");
            relativePos = rearRightFoot.position - Player.Instance.Rigidbody.position;
            _rearRightFootAnim = new LegAnimator(rearRightFoot, 1f, rearRightFoot.position, rearRightFoot.position, secondOrderDynamicsRrF, relativePos, "rr");
        
            _legs.Add(_frontLeftFootAnim);
            _legs.Add(_frontRightFootAnim);
            _legs.Add(_rearLeftFootAnim);
            _legs.Add(_rearRightFootAnim);
        
            _groupALegs.Add(_frontLeftFootAnim);
            _groupALegs.Add(_rearRightFootAnim);
        
            _groupBLegs.Add(_frontRightFootAnim);
            _groupBLegs.Add(_rearLeftFootAnim);
            _lastPosition = transform.position;
            
            InitializeLegs();
        }

        private bool VerifyMove()
        {
            /*
            if ((transform.position - _lastPosition).magnitude < 0.01f)
            {
                _lastPosition = transform.position;
                return false;
            }
        
            _lastPosition = transform.position;
            return true;
            */
            
            if (MovementDelta.magnitude < 0.01f)
            {
                return false;
            }

            return true;

        }

        private void InitializeLegs()
        {
            ReturnLegToIdle();
            MoveLegs(StepGroup.Idle); 
        }

        public void ObservedFixedUpdate() 
        {
            if (_currentGroup != StepGroup.Opening)
            {
                Debug.Log("CurrentGroup: " + _currentGroup);
                // Call the update for each leg and set the new IK position
                
                // Re-compute the grounded when executing step, after having flushed the movement delta applied
                //ExecuteGrounded();

                bool isMoving = VerifyMove();
                bool isGrounded = Player.Instance.IsGrounded();
                
                if (!isGrounded && !_wasGrounded)
                {
                    _currentGroup = StepGroup.Floating;
                }else
                if (IsStopped(isMoving) || _currentGroup == StepGroup.Floating)
                {
                    _currentGroup = StepGroup.Idle;
                    ReturnLegToIdle();
                }else
                {
                    // just started moving?
                    if (IsStartedMoving(isMoving))
                    {
                        _currentGroup = StepGroup.GroupA;
                        StartStepForGroup(_currentGroup);
                    }
                    // A finished?
                    else if (_currentGroup == StepGroup.GroupA && IsMovementGroupFinished(StepGroup.GroupA))
                    {
                        _currentGroup = StepGroup.GroupB;
                        StartStepForGroup(_currentGroup);
                    }
                    // B finished?
                    else if (_currentGroup == StepGroup.GroupB && IsMovementGroupFinished(StepGroup.GroupB))
                    {
                        _currentGroup = StepGroup.GroupA;
                        StartStepForGroup(_currentGroup);
                    }
                }
                MoveLegs(_currentGroup);

                _wasMoving = isMoving;
                _wasGrounded = isGrounded;
                if (Player.Instance.RaycastManager)
                {
                    Player.Instance.RaycastManager.SetLegs(_legs);
                }

            }
            
        }

        public void ExecuteGrounded()
        {
            Player.Instance.RaycastManager.FlushRaycasts();
            VerifyGroundedForGroup(StepGroup.GroupA);
            VerifyGroundedForGroup(StepGroup.GroupB);
        }


        bool IsStartedMoving(bool isMoving)
        {
            return isMoving && !_wasMoving && (_currentGroup == StepGroup.Idle || _currentGroup == StepGroup.Floating ||
                                               _currentGroup == StepGroup.Opening);
        }
        bool IsStopped(bool newMoving)
        {
            return !newMoving && _currentGroup != (StepGroup.Idle) && IsMovementGroupFinished(StepGroup.GroupA) &&
                   IsMovementGroupFinished(StepGroup.GroupB) && MovementDelta.magnitude < 0.01f && RotationDelta == Quaternion.identity;
        }
        void ReturnLegToIdle()
        {
            for (int i = 0; i < _legs.Count; i++)
            {
                if (Player.Instance.RaycastManager)
                {
                    Player.Instance.RaycastManager.ExecuteReturnToIdle(_legs[i]);
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
                    return _groupALegs.All(leg => leg.Lerp >= 1f);
                case StepGroup.GroupB:
                    return _groupBLegs.All(leg => leg.Lerp >= 1f);
            }

            return true;
        }

        private void MoveLegs(StepGroup stepGroup)
        {
            
            switch (stepGroup)
            {
                case StepGroup.GroupA:
                    BlockLegGroup(_groupBLegs);
                    break;
                case StepGroup.GroupB:
                    BlockLegGroup(_groupALegs);
                    break;
                case StepGroup.Floating:
                    MoveLegReturnToBody(_frontLeftFootAnim);
                    MoveLegReturnToBody(_frontRightFootAnim);
                    MoveLegReturnToBody(_rearLeftFootAnim);
                    MoveLegReturnToBody(_rearRightFootAnim);
                    break;
            }
            MoveLegGroup(_groupALegs);
            MoveLegGroup(_groupBLegs);
            
        }


        private void BlockLegGroup(List<LegAnimator> legs)
        {
            foreach (var leg in legs)
            {
                leg.Lerp = 1.0f;
            }
        }
        
        private void MoveLegGroup(List<LegAnimator> legs)
        {
            foreach (var leg in legs)
            {
                MoveLegStep(leg);
            }

        }
        /*
         * Function that manages the movement of a leg. A leg can move in three different way:
         * -Execute a step
         * -Remain in place
         * -Follow the user's movement
         * To make a leg follow the user's movement we add an independent movement value that is
         * applied whenever the player is moving but the movement is not result of an input
         */
        private void MoveLegStep(LegAnimator legAnimator)
        {
            if (legAnimator.NewPosition == Vector3.zero)
                 legAnimator.NewPosition = legAnimator.OldPosition;
        
            if (legAnimator.Lerp < 1f)
            {
                legAnimator.Lerp = Mathf.Min(legAnimator.Lerp + Time.deltaTime * stepSpeed, 1f);
                // remap into an animation curve
                float t = legAnimationCurve.Evaluate(legAnimator.Lerp);
                //float t = Mathf.SmoothStep(0f, 1f, legAnimator.Lerp);
                float verticalOffset = 0f;
                verticalOffset = Mathf.Sin(t * Mathf.PI) * stepHeight;
        
                //Vector3 planarPos = GetLegPlanarPosition(legAnimator);

                Vector3 planarPos = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
                Vector3 localVertical = transform.parent.transform.up * verticalOffset + transform.parent.transform.up * footHeight;
                Vector3 finalPos = new Vector3(planarPos.x, planarPos.y, planarPos.z) + localVertical;
                legAnimator.Transform.position = finalPos;

                if (legAnimator.Lerp >= 1f)
                {
                    legAnimator.OldPosition = legAnimator.NewPosition;
                }

            }
            else
            {
                legAnimator.Transform.position = legAnimator.OldPosition + GetFootHeight();
            }

        
        }

        public void FollowUserMovement(Vector3 movement)
        {
            foreach (var leg in _legs)
            {
                leg.Transform.position += movement;
                leg.OldPosition += movement;
                leg.NewPosition += movement;
            }
        }
        
        public void FollowUserRotation(Quaternion deltaRot)
        {

            Vector3 pivot = Player.Instance.Rigidbody.position;

            foreach (var leg in _legs)
            {
                Vector3 curOffset = leg.Transform.position - pivot;
                leg.Transform.position = pivot + deltaRot * curOffset;

                Vector3 oldOffset = leg.OldPosition - pivot;
                leg.OldPosition = pivot + deltaRot * oldOffset;

                Vector3 newOffset = leg.NewPosition - pivot;
                leg.NewPosition = pivot + deltaRot * newOffset;
            }
        }

        private Vector3 GetFootHeight()
        {
            return transform.parent.transform.up * footHeight;
        }

        private Vector3 GetLegPlanarPosition(LegAnimator legAnimator)
        {
            Vector3 worldTarget = legAnimator.NewPosition;
            Vector3 localTarget = transform.parent.transform.InverseTransformPoint(worldTarget);
            Vector3 localResult = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, localTarget);
            Vector3 worldResult = transform.parent.transform.transform.TransformPoint(localResult);
            Vector3 planarPos = worldResult;
            return planarPos;
        }

        void MoveLegReturnToBody(LegAnimator legAnimator)
        {
            if (legAnimator == null) return;

            var rb = Player.Instance.Rigidbody;
            
            Vector3 worldOffset = rb.rotation * legAnimator.RelativePosition;

            Vector3 hipPos = rb.position + worldOffset;

            Vector3 resetPos = hipPos - rb.transform.up * footHeight;

            // 4) assign
            legAnimator.NewPosition = resetPos;
            legAnimator.Lerp = 0f;
        }


        void StartStepForGroup(StepGroup group)
        {
            if (Player.Instance.RaycastManager)
            {
                List<LegAnimator> legToMove = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                
                for (int i = 0; i < legToMove.Count; i++)
                {
                    if (!Player.Instance.RaycastManager.ExecuteStepForLeg(legToMove[i]))
                    {
                        MoveLegReturnToBody(legToMove[i]);
                    };
                }
            }
        }

        void VerifyGroundedForGroup(StepGroup group)
        {
            RaycastManager raycastManager = Player.Instance.RaycastManager;
            if (raycastManager)
            {
                List<LegAnimator> leg = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                
                for (int i = 0; i < leg.Count; i++)
                {
                    raycastManager.ExecuteGroundedForLeg(leg[i]);
                }
            }
        }

        private void OnEnable()
        {
            FixedUpdateManager.Instance.Register(this);
            Debug.Log("Enabling Walk Animator");
            Player.Instance.RaycastManager.enabled = true;
            Player.Instance.RaycastManager.ResetMovementDelta();
            _currentGroup = StepGroup.Opening;
            //ResetAllLegs();
            //InitializeLegs();
            // Call the raycast to update normals
            ExecuteGrounded();
            StartCoroutine(FadeInRig(0.1f));
            //StartCoroutine(DelaySetWeight());
        }

        private void OnDisable()
        {
            Player.Instance.RaycastManager.enabled = false;
            legRig.weight = 0.0f;
            FixedUpdateManager.Instance?.Unregister(this);
        }
    
        private IEnumerator DelaySetWeight()
        {
            yield return null; 
            yield return null;          
            legRig.weight = 1.0f;
        }
        

        public IEnumerator FadeInRig(float duration)
        {
            float elapsed = 0f;
            legRig.weight = 0f;
            // Snap to idle position
            ResetAllLegs();
          
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                legRig.weight = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            legRig.weight = 1f;
             
            // Re-Activate switch input
            _currentGroup = StepGroup.Idle;
            
            OnOpenFinished?.Invoke();
        }

        public void ResetAllLegs()
        {
            
            RaycastManager raycaster = Player.Instance.RaycastManager;
            if (raycaster && IsInitialized())
            {
                raycaster.ExecuteResetPosition(_frontLeftFootAnim);
                raycaster.ExecuteResetPosition(_frontRightFootAnim);
                raycaster.ExecuteResetPosition(_rearLeftFootAnim);
                raycaster.ExecuteResetPosition(_rearRightFootAnim);
                // Reset Second Order Dynamics
                secondOrderDynamicsFlF.Initialize(f, z, r, frontLeftFoot.position);
                secondOrderDynamicsFrF.Initialize(f, z, r, frontRightFoot.position);
                secondOrderDynamicsRlF.Initialize(f, z, r, rearLeftFoot.position);
                secondOrderDynamicsRrF.Initialize(f, z, r, rearRightFoot.position);
                _currentGroup = StepGroup.Idle;
                _wasMoving = false;
                ReturnLegToIdle();
            }
            
            
            
            
        }

        private bool IsInitialized()
        {
            return _frontLeftFootAnim != null && _frontRightFootAnim != null && _rearLeftFootAnim != null && _rearRightFootAnim != null;
        }

       
        private void ResetLegRelativePosition(LegAnimator leg)
        {
            if (leg == null) return;

            var rb = Player.Instance.Rigidbody;
            
            Vector3 worldOffset = rb.rotation * leg.RelativePosition;

            Vector3 hipPos = rb.position + worldOffset;

            Vector3 resetPos = hipPos - rb.transform.up * footHeight;

            // 4) assign
            leg.Transform.position = resetPos;
            leg.OldPosition = resetPos;
            leg.NewPosition = resetPos;
            leg.Lerp = 1f;
        }

    
    
    
    }
}