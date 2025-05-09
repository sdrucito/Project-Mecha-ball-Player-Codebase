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
        private Vector3 _lastPosition;

        public Action OnOpenFinished;
        void Start()
        {

            // Initialize the second order resolvers for each foot
            secondOrderDynamicsFlF.Initialize(f, z, r, frontLeftFoot.position);
            secondOrderDynamicsFrF.Initialize(f, z, r, frontRightFoot.position);
            secondOrderDynamicsRlF.Initialize(f, z, r, rearLeftFoot.position);
            secondOrderDynamicsRrF.Initialize(f, z, r, rearRightFoot.position);

            Vector3 relativePos = frontLeftFoot.position - center.position;
            relativePos.y += footHeight;
            _frontLeftFootAnim = new LegAnimator(frontLeftFoot, 1f, frontLeftFoot.position, frontLeftFoot.position, secondOrderDynamicsFlF, relativePos);
            relativePos = frontRightFoot.position - Player.Instance.Rigidbody.position;
            relativePos.y += footHeight;
            _frontRightFootAnim = new LegAnimator(frontRightFoot, 1f, frontRightFoot.position, frontRightFoot.position, secondOrderDynamicsFrF, relativePos);
            relativePos = rearLeftFoot.position - Player.Instance.Rigidbody.position;
            relativePos.y += footHeight;
            _rearLeftFootAnim = new LegAnimator(rearLeftFoot, 1f, rearLeftFoot.position, rearLeftFoot.position, secondOrderDynamicsRlF, relativePos);
            relativePos = rearRightFoot.position - Player.Instance.Rigidbody.position;
            relativePos.y += footHeight;
            _rearRightFootAnim = new LegAnimator(rearRightFoot, 1f, rearRightFoot.position, rearRightFoot.position, secondOrderDynamicsRrF, relativePos);
        
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
            if ((transform.position - _lastPosition).magnitude < 0.01f)
            {
                _lastPosition = transform.position;
                return false;
            }
        
            _lastPosition = transform.position;
            return true;
            
        }

        private void InitializeLegs()
        {
            ReturnLegToIdle();
            MoveLegs(StepGroup.Idle); 
        }
/*
        void FixedUpdate()
        {
            bool isMoving   = VerifyMove();
            bool isGrounded = Player.Instance.IsGrounded();
            Debug.Log("IsGrounded: " + isGrounded);

            // 1) Opening / floating / idle all just snap home
            if (_currentGroup == StepGroup.Opening || !isGrounded || (!isMoving && _currentGroup != StepGroup.Idle))
            {
                if (_currentGroup != StepGroup.Idle)
                    ReturnLegToIdle();
                _currentGroup = StepGroup.Idle;
                MoveLegs(StepGroup.Idle);
                _wasMoving = false;
                return;
            }

            // 1b) Don’t start your very first step until we’ve done at least one grounded pass
            if (_justFinishedOpening)
            {
                // run your raycasts so every leg.NewPosition is set
                VerifyGroundedForGroup(StepGroup.GroupA);
                VerifyGroundedForGroup(StepGroup.GroupB);
                _justFinishedOpening = false;
                return;
            }

            // 2) Normal walking
            if (isMoving && !_wasMoving)
            {
                _currentGroup = StepGroup.GroupA;
                StartStepForGroup(_currentGroup);
            }
            else if (_currentGroup == StepGroup.GroupA && IsMovementGroupFinished(_currentGroup))
            {
                _currentGroup = StepGroup.GroupB;
                StartStepForGroup(_currentGroup);
            }
            else if (_currentGroup == StepGroup.GroupB && IsMovementGroupFinished(_currentGroup))
            {
                _currentGroup = StepGroup.GroupA;
                StartStepForGroup(_currentGroup);
            }

            MoveLegs(_currentGroup);
            _wasMoving = isMoving;
        }
        */

        void FixedUpdate()
        {

            if (_currentGroup != StepGroup.Opening)
            {
                //Debug.Log("CurrentGroup: " + _currentGroup);
                // Call the update for each leg and set the new IK position
                bool isMoving = VerifyMove();
                bool isGrounded = Player.Instance.IsGrounded();
                Debug.Log("IsGrounded: " + isGrounded);
                VerifyGroundedForGroup(StepGroup.GroupA);
                VerifyGroundedForGroup(StepGroup.GroupB);
                if (!isGrounded)
                {
                    if (_currentGroup != StepGroup.Floating)
                    {
                        _currentGroup = StepGroup.Floating;
                        ReturnLegToIdle();
                    }
                }else
                if (IsStopped(isMoving))
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
        
                if (Player.Instance.RaycastManager)
                {
                    Player.Instance.RaycastManager.SetLegs(_legs);
                }
                //Debug.Log("Current group: " + _currentGroup);
                //Debug.Log("Current grounded: " + Player.Instance.IsGrounded());
            }
            
        }

        bool IsStartedMoving(bool isMoving)
        {
            return isMoving && !_wasMoving && (_currentGroup == StepGroup.Idle || _currentGroup == StepGroup.Floating ||
                                               _currentGroup == StepGroup.Opening);
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
                    /*
                    MoveLegReturnToBody(_frontLeftFootAnim);
                    MoveLegReturnToBody(_frontRightFootAnim);
                    MoveLegReturnToBody(_rearLeftFootAnim);
                    MoveLegReturnToBody(_rearRightFootAnim);
                    */
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
        void MoveLegStep(LegAnimator legAnimator)
        {
            if (legAnimator.NewPosition == Vector3.zero)
                 legAnimator.NewPosition = legAnimator.OldPosition;
        
            if (legAnimator.Lerp < 1f)
            {
                legAnimator.Lerp = Mathf.Min(legAnimator.Lerp + Time.deltaTime * stepSpeed, 1f);
                float verticalOffset = 0f;
                verticalOffset = Mathf.Sin(legAnimator.Lerp * Mathf.PI) * stepHeight;
        
                //Vector3 planarPos = GetLegPlanarPosition(legAnimator);

                Vector3 planarPos = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
                Vector3 localVertical = transform.parent.transform.up * verticalOffset;
                Vector3 finalPos = new Vector3(planarPos.x, planarPos.y, planarPos.z) + localVertical;
                legAnimator.Transform.position = finalPos;

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
            legAnimator.Transform.position = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
        }


        void StartStepForGroup(StepGroup group)
        {
            if (Player.Instance.RaycastManager)
            {
                List<LegAnimator> legToMove = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                
                for (int i = 0; i < legToMove.Count; i++)
                {
                    Player.Instance.RaycastManager.ExecuteStepForLeg(legToMove[i]);
                }
            }
        }

        void VerifyGroundedForGroup(StepGroup group)
        {
            if (Player.Instance.RaycastManager)
            {
                Player.Instance.RaycastManager.FlushRaycasts();
                
                List<LegAnimator> leg = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                
                for (int i = 0; i < leg.Count; i++)
                {
                    Player.Instance.RaycastManager.ExecuteGroundedForLeg(leg[i]);
                }
            }
        }

        private void OnEnable()
        {
            Player.Instance.RaycastManager.enabled = true;
            Player.Instance.RaycastManager.ResetMovementDelta();
            _currentGroup = StepGroup.Opening;
            //ResetAllLegs();
            //InitializeLegs();
            StartCoroutine(FadeInRig(0.1f));
            //StartCoroutine(DelaySetWeight());
        }

        private void OnDisable()
        {
            Player.Instance.RaycastManager.enabled = false;
            legRig.weight = 0.0f;
            
        }
    
        private IEnumerator DelaySetWeight()
        {
            yield return null; 
            yield return null;          
            legRig.weight = 1.0f;
        }
        
        private bool _justFinishedOpening = false;

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
            PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            _currentGroup = StepGroup.Idle;
            _justFinishedOpening = true;
            OnOpenFinished?.Invoke();
        }

        public void ResetAllLegs()
        {
            
            //_currentGroup = StepGroup.Idle;
            ResetLegRelativePosition(_frontLeftFootAnim);
            ResetLegRelativePosition(_frontRightFootAnim);
            ResetLegRelativePosition(_rearLeftFootAnim);
            ResetLegRelativePosition(_rearRightFootAnim);
            // Reset Second Order Dynamics
            secondOrderDynamicsFlF.Initialize(f, z, r, frontLeftFoot.position);
            secondOrderDynamicsFrF.Initialize(f, z, r, frontRightFoot.position);
            secondOrderDynamicsRlF.Initialize(f, z, r, rearLeftFoot.position);
            secondOrderDynamicsRrF.Initialize(f, z, r, rearRightFoot.position);
            _currentGroup = StepGroup.Idle;
            _wasMoving = false;
            ReturnLegToIdle();
            
            
        }
/*
        private void ResetLegRelativePosition(LegAnimator leg)
        {
            if (leg != null)
            {
                //Debug.Log("Resetting relative position");
                Rigidbody instanceRigidbody = Player.Instance.Rigidbody;
                Vector3 worldOffset = instanceRigidbody.rotation * leg.RelativePosition;
                Vector3 resetPos = instanceRigidbody.position + worldOffset;
                resetPos.y -= footHeight;
                leg.Transform.position = resetPos;
                leg.OldPosition = resetPos;
                leg.NewPosition = resetPos;
                leg.Lerp = 1.0f;
                #if UNITY_EDITOR
                var debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.transform.position = resetPos;
                debugSphere.transform.localScale = Vector3.one * 0.1f;
                Destroy(debugSphere.GetComponent<Collider>());  // optional, to avoid physics
                Destroy(debugSphere, 0.5f);
                #endif
            }
            
        }
        */
        
        private void ResetLegRelativePosition(LegAnimator leg)
        {
            if (leg == null) return;

            // 1) extract the foot's stored local offset in XZ
            Vector3 localOffset = leg.RelativePosition;
            localOffset.y = 0f;

            // 2) build a yaw-only quaternion from the body
            float bodyYaw = Player.Instance.Rigidbody.rotation.eulerAngles.y;
            Quaternion yawOnly = Quaternion.Euler(0f, bodyYaw, 0f);

            // 3) rotate that horizontal offset by yaw, and drop to slope
            Vector3 worldOffset = yawOnly * localOffset;
            Vector3 resetPos   = Player.Instance.Rigidbody.position + worldOffset;

            // 4) now raycast or just lower by footHeight
            // TODO: AGGIUSTARE QUA PER LO SLOPE, PER ALCUNE GAMBE E' + PER ALTRE E' -
            resetPos -= Player.Instance.GetGroundNormal() * footHeight;
            // (Or if you have your own ground‐ray routine, call that here)

            // 5) assign
            leg.Transform.position = resetPos;
            leg.OldPosition        = resetPos;
            leg.NewPosition        = resetPos;
            leg.Lerp               = 1f;
        }

    
    
    
    }
}