using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FMOD.Studio;
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
        public string Name;

        public LegAnimator(Transform transform, float lerp, Vector3 newPosition, Vector3 oldPosition, SecondOrderDynamics secondOrderDynamics, Vector3 relativePosition, string name)
        {
            Transform = transform;
            Lerp = lerp;
            NewPosition = newPosition;
            OldPosition = oldPosition;
            SecondOrderDynamics = secondOrderDynamics;
            RelativePosition = relativePosition;
            Name = name;
        }

    }
    /// <summary>
    /// Component that manages the procedural animation for the player's knee-walking movement,
    /// coordinating leg IK targets, stepping timing, and rig weight.
    /// </summary>
    public class PlayerKneeWalkAnimator : MonoBehaviour, IFixedUpdateObserver
    {
        #region Serialized Parameters
        [Header("Dynamics Parameters")]
        [SerializeField, Range(0,3)] private float f;
        [SerializeField, Range(0,3)] private float z;
        [SerializeField, Range(0,3)] private float r;
        [Header("Animation Curve & Center")]
        [SerializeField] private AnimationCurve legAnimationCurve;
        [SerializeField] private Transform center;
        [Header("Sound & VFX")]
        [SerializeField] private PlayerSound playerSound;
        #endregion

        #region IK Targets
        [Header("Front & Rear Foot IK Targets")]
        [SerializeField] private Transform frontLeftFoot;
        [SerializeField] private Transform frontRightFoot;
        [SerializeField] private Transform rearLeftFoot;
        [SerializeField] private Transform rearRightFoot;
        #endregion

        #region Dynamics Resolvers
        [Header("Second Order Dynamics Resolvers")]
        [SerializeField] SecondOrderDynamics secondOrderDynamicsFlF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsFrF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsRlF;
        [SerializeField] SecondOrderDynamics secondOrderDynamicsRrF;
        #endregion

        #region Step & Foot Settings
        [Header("Step Settings")]
        [SerializeField] private float stepHeight = 10.0f;
        [SerializeField] private float stepSpeed = 2.0f;
        [Header("Foot Height")]
        [SerializeField] private float footHeight = 1.5f;
        #endregion

        #region Rig Reference
        [Header("Rig Component")]
        [SerializeField] private Rig legRig;
        #endregion

        #region Leg Animators
        private LegAnimator _frontLeftFootAnim;
        private LegAnimator _frontRightFootAnim;
        private LegAnimator _rearLeftFootAnim;
        private LegAnimator _rearRightFootAnim;
        private List<LegAnimator> _legs = new List<LegAnimator>();
        #endregion

        #region Step Groups
        enum StepGroup { Idle, GroupA, GroupB, Floating, Opening }
        private readonly List<LegAnimator> _groupALegs = new List<LegAnimator>();
        private readonly List<LegAnimator> _groupBLegs = new List<LegAnimator>();
        #endregion

        #region State
        private StepGroup _currentGroup = StepGroup.Idle;
        private bool _wasMoving = false;
        private bool _wasGrounded = false;
        private Vector3 _lastPosition;
        public Action OnOpenFinished;

        public Vector3 MovementDelta { get; set; }
        public Quaternion RotationDelta { get; set; }
        public int FixedUpdatePriority { get; set; }
        public bool IsReady  { get; private set; }
        #endregion

        /// <summary>
        /// Initialize dynamics, leg animators, and group assignments at startup.
        /// Sets the execution priority for fixed updates.
        /// </summary>
        private void Awake()
        {
            FixedUpdatePriority = 1;
            // Initialize the second order resolvers for each foot
            InitializeSecondOrderDynamics();

            // Instantiate LegAnimator for each IK target
            InitializeLegAnimator();

            // Collect all legs and assign to alternating step groups
            _legs.Add(_frontLeftFootAnim);
            _legs.Add(_frontRightFootAnim);
            _legs.Add(_rearLeftFootAnim);
            _legs.Add(_rearRightFootAnim);

            _groupALegs.Add(_frontLeftFootAnim);
            _groupALegs.Add(_rearRightFootAnim);

            _groupBLegs.Add(_frontRightFootAnim);
            _groupBLegs.Add(_rearLeftFootAnim);

            _lastPosition = transform.position;
        }

        /// <summary>
        /// Initialize legs and build whiten scale
        /// </summary>
        private void Start()
        {
            BuildWhitenScale();
            InitializeLegs();
        }

        /// <summary>
        /// Sets initial position for all legs
        /// </summary>
        private void InitializeLegAnimator()
        {
            Vector3 relativePos = frontLeftFoot.position - center.position;
            _frontLeftFootAnim = new LegAnimator(frontLeftFoot, 1f, frontLeftFoot.position, frontLeftFoot.position, secondOrderDynamicsFlF, relativePos, "fl");
            relativePos = frontRightFoot.position - Player.Instance.Rigidbody.position;
            _frontRightFootAnim = new LegAnimator(frontRightFoot, 1f, frontRightFoot.position, frontRightFoot.position, secondOrderDynamicsFrF, relativePos, "fr");
            relativePos = rearLeftFoot.position - Player.Instance.Rigidbody.position;
            _rearLeftFootAnim = new LegAnimator(rearLeftFoot, 1f, rearLeftFoot.position, rearLeftFoot.position, secondOrderDynamicsRlF, relativePos, "rl");
            relativePos = rearRightFoot.position - Player.Instance.Rigidbody.position;
            _rearRightFootAnim = new LegAnimator(rearRightFoot, 1f, rearRightFoot.position, rearRightFoot.position, secondOrderDynamicsRrF, relativePos, "rr");
        }

        /// <summary>
        /// Initializes SecondOrderDynamics component for each leg with the initial position
        /// </summary>
        private void InitializeSecondOrderDynamics()
        {
            secondOrderDynamicsFlF.Initialize(f, z, r, frontLeftFoot.position);
            secondOrderDynamicsFrF.Initialize(f, z, r, frontRightFoot.position);
            secondOrderDynamicsRlF.Initialize(f, z, r, rearLeftFoot.position);
            secondOrderDynamicsRrF.Initialize(f, z, r, rearRightFoot.position);
        }


        public void BuildWhitenScale()
        {
            var localPositions = _legs
                .Select(l => transform.parent.transform.InverseTransformPoint(l.Transform.position))
                .ToList();
            float minX = localPositions.Min(p => p.x);
            float maxX = localPositions.Max(p => p.x);
            float minZ = localPositions.Min(p => p.z);
            float maxZ = localPositions.Max(p => p.z);
            float halfWidth  = (maxX - minX) * 0.5f;
            float halfLength = (maxZ - minZ) * 0.5f;
            Vector2 whitenScale = new Vector2(
                1f / halfWidth,
                1f / halfLength
            );
            Player.Instance.UpdateWhitenScaleForLegs(whitenScale);

        }

        /// <summary>
        /// Check if the movement delta indicates significant motion.
        /// </summary>
        private bool VerifyMove()
        {
            return MovementDelta.magnitude >= 0.01f || RotationDelta != Quaternion.identity;
        }

        /// <summary>
        /// Reset legs to idle configuration and halt any stepping.
        /// </summary>
        private void InitializeLegs()
        {
            ReturnLegToIdle();
            MoveLegs(StepGroup.Idle);
        }

        /// <summary>
        /// Called by FixedUpdateManager each physics step. Manages stepping logic,
        /// transitions between step groups, and updates IK targets.
        /// </summary>
        public void ObservedFixedUpdate()
        {
            if (_currentGroup == StepGroup.Opening) return;

            bool isMoving = VerifyMove();
            bool isGrounded = Player.Instance.IsGrounded();
            if (!isGrounded && !_wasGrounded)
            {
                _currentGroup = StepGroup.Floating;
            }
            else if (IsStopped(isMoving) || _currentGroup == StepGroup.Floating)
            {
                _currentGroup = StepGroup.Idle;
                ReturnLegToIdle();
                playerSound.LegMove();
                playerSound.Step();
            }
            else
            {
                if (IsStartedMoving(isMoving) || (_wasMoving && isMoving && _currentGroup == StepGroup.Idle))
                {
                    _currentGroup = StepGroup.GroupA;
                    StartStepForGroup(_currentGroup);
                    playerSound.LegMove();

                }
                else if (_currentGroup == StepGroup.GroupA && IsMovementGroupFinished(StepGroup.GroupA))
                {
                    _currentGroup = StepGroup.GroupB;
                    StartStepForGroup(_currentGroup);
                    //playerSound.Step();

                }
                else if (_currentGroup == StepGroup.GroupB && IsMovementGroupFinished(StepGroup.GroupB))
                {
                    _currentGroup = StepGroup.GroupA;
                    StartStepForGroup(_currentGroup);
                    //playerSound.Step();

                }
            }
            MoveLegs(_currentGroup);

            _wasMoving = isMoving;
            _wasGrounded = isGrounded;

            if (Player.Instance.RaycastManager)
                Player.Instance.RaycastManager.SetLegs(_legs);
        }

        /// <summary>
        /// Execute ground checks for both step groups via the RaycastManager.
        /// </summary>
        public void ExecuteGrounded()
        {
            Player.Instance.RaycastManager.FlushRaycasts();
            VerifyGroundedForGroup(StepGroup.GroupA);
            VerifyGroundedForGroup(StepGroup.GroupB);
        }

        #region Movement State Checks
        /// <summary>
        /// Determine if movement has just started.
        /// </summary>
        private bool IsStartedMoving(bool isMoving)
        {
            return isMoving && !_wasMoving && (_currentGroup == StepGroup.Idle || _currentGroup == StepGroup.Floating || _currentGroup == StepGroup.Opening);
        }

        /// <summary>
        /// Determine if stepping should stop based on movement and rotation deltas.
        /// </summary>
        private bool IsStopped(bool newMoving)
        {
            return !newMoving && _currentGroup != StepGroup.Idle
                && IsMovementGroupFinished(StepGroup.GroupA)
                && IsMovementGroupFinished(StepGroup.GroupB)
                && MovementDelta.magnitude < 0.01f
                && RotationDelta == Quaternion.identity;
        }

        /// <summary>
        /// Check if a given step group has completed all its leg lerps.
        /// </summary>
        private bool IsMovementGroupFinished(StepGroup stepGroup)
        {
            switch (stepGroup)
            {
                case StepGroup.Idle: return true;
                case StepGroup.GroupA: return _groupALegs.All(leg => leg.Lerp >= 1f);
                case StepGroup.GroupB: return _groupBLegs.All(leg => leg.Lerp >= 1f);
                default: return true;
            }
        }
        #endregion

        /// <summary>
        /// Immediately place all legs back to their idle positions.
        /// </summary>
        public void ReturnLegToIdle()
        {
            foreach (var leg in _legs)
            {
                Player.Instance.RaycastManager?.ExecuteReturnToIdle(leg);
            }
        }

        /// <summary>
        /// Route leg updates based on the current step group,
        /// blocking opposite group while active.
        /// </summary>
        private void MoveLegs(StepGroup stepGroup)
        {
            if (stepGroup == StepGroup.GroupA) BlockLegGroup(_groupBLegs);
            else if (stepGroup == StepGroup.GroupB) BlockLegGroup(_groupALegs);
            else if (stepGroup == StepGroup.Floating)
            {
                MoveLegReturnToBody(_frontLeftFootAnim);
                MoveLegReturnToBody(_frontRightFootAnim);
                MoveLegReturnToBody(_rearLeftFootAnim);
                MoveLegReturnToBody(_rearRightFootAnim);
            }

            MoveLegGroup(_groupALegs);
            MoveLegGroup(_groupBLegs);
        }

        /// <summary>
        /// Force a group of legs to complete their lerp (stay planted).
        /// </summary>
        private void BlockLegGroup(List<LegAnimator> legs)
        {
            foreach (var leg in legs)
                leg.Lerp = 1f;
        }

        /// <summary>
        /// Update each leg in the provided list by executing its step logic.
        /// </summary>
        private void MoveLegGroup(List<LegAnimator> legs)
        {
            foreach (var leg in legs)
                MoveLegStep(leg);
        }

        /// <summary>
        /// Perform stepping logic: interpolate along curve, apply vertical offset,
        /// and update IK target positions using second-order dynamics.
        /// </summary>
        private void MoveLegStep(LegAnimator legAnimator)
        {
            if (legAnimator.NewPosition == Vector3.zero)
                legAnimator.NewPosition = legAnimator.OldPosition + GetFootHeight();
            
            if (legAnimator.Lerp == 0.0f)
            {
                // Step just started
                //playerSound.LegMove();
            }
            if (legAnimator.Lerp < 1f)
            {
                legAnimator.Lerp = Mathf.Min(legAnimator.Lerp + Time.fixedDeltaTime * stepSpeed, 1f);
                float t = legAnimationCurve.Evaluate(legAnimator.Lerp);
                float verticalOffset = Mathf.Sin(t * Mathf.PI) * stepHeight;

                Vector3 localVertical = transform.parent.up * (verticalOffset + footHeight/2);
                Vector3 planarPos = legAnimator.SecondOrderDynamics.UpdatePosition(Time.fixedDeltaTime, legAnimator.NewPosition + localVertical);

                legAnimator.Transform.position = planarPos + localVertical;

                if (legAnimator.Lerp >= 1f)
                {
                    // Step ended
                    legAnimator.OldPosition = legAnimator.NewPosition;
                    playerSound.Step();
                }
            }
            else
            {
                //SnapLegToPosition(legAnimator);
                legAnimator.Transform.position = legAnimator.SecondOrderDynamics.UpdatePosition(Time.fixedDeltaTime, legAnimator.OldPosition + GetFootHeight());
                legAnimator.Transform.position = legAnimator.OldPosition + GetFootHeight();
                //Vector3 targetPos = legAnimator.OldPosition + GetFootHeight();
                
            }
        }
        

        /// <summary>
        /// Apply a translation delta to all leg targets, useful when character root moves.
        /// </summary>
        public void FollowUserMovement(Vector3 movement)
        {
            foreach (var leg in _legs)
            {
                leg.Transform.position += movement;
                leg.OldPosition += movement;
                leg.NewPosition += movement;
            }
        }

        /// <summary>
        /// Apply a rotation delta to all leg targets around the character pivot.
        /// </summary>
        public void FollowUserRotation(Quaternion deltaRot)
        {
            Vector3 pivot = Player.Instance.Rigidbody.position;
            foreach (var leg in _legs)
            {
                leg.Transform.position = pivot + deltaRot * (leg.Transform.position - pivot);
                leg.OldPosition = pivot + deltaRot * (leg.OldPosition - pivot);
                leg.NewPosition = pivot + deltaRot * (leg.NewPosition - pivot);
            }
        }

        /// <summary>
        /// Compute the vertical foot height offset vector.
        /// </summary>
        private Vector3 GetFootHeight()
        {
            return transform.parent.up * footHeight;
        }

        /// <summary>
        /// Calculate the filtered planar position of the leg using second order dynamics.
        /// </summary>
        private Vector3 GetLegPlanarPosition(LegAnimator legAnimator)
        {
            Vector3 worldTarget = legAnimator.NewPosition;
            Vector3 localTarget = transform.parent.InverseTransformPoint(worldTarget);
            Vector3 localResult = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, localTarget);
            return transform.parent.TransformPoint(localResult);
        }

        /// <summary>
        /// Reset a leg's target to a position directly under its hip using relative offset.
        /// </summary>
        void MoveLegReturnToBody(LegAnimator legAnimator)
        {
            if (legAnimator == null) return;

            var rb = Player.Instance.Rigidbody;
            Vector3 hipPos = rb.position + rb.rotation * legAnimator.RelativePosition;
            Vector3 resetPos = hipPos - rb.transform.up * footHeight;

            legAnimator.NewPosition = resetPos;
            legAnimator.Lerp = 0f;
        }

        /// <summary>
        /// Request the RaycastManager to plan steps for each leg in the specified group.
        /// </summary>
        void StartStepForGroup(StepGroup group)
        {
            RaycastManager rm = Player.Instance.RaycastManager;

            if (rm.isActiveAndEnabled)
            {
                var legToMove = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                foreach (var leg in legToMove)
                {
                    if (!rm.ExecuteStepForLeg(leg))
                        MoveLegReturnToBody(leg);
                }
            }
        }

        /// <summary>
        /// Delegate grounded state verification for each leg in the group.
        /// </summary>
        void VerifyGroundedForGroup(StepGroup group)
        {
            RaycastManager rm = Player.Instance.RaycastManager;
            if (rm.isActiveAndEnabled)
            {
                var list = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                foreach (var leg in list)
                    rm.ExecuteGroundedForLeg(leg);
            }
            else
            {
                Debug.Log("RaycastManager not active");
            }
        }

        /// <summary>
        /// Enable the component: register for fixed updates, reset movement, and fade in rig.
        /// </summary>
        private void OnEnable()
        {
            IsReady = false;
            FixedUpdateManager.Instance.Register(this);
            RaycastManager raycastManager = Player.Instance.RaycastManager;
            if(raycastManager != null)
                raycastManager.enabled = true;
            if (raycastManager.isActiveAndEnabled)
            {
                raycastManager.ResetMovementDelta();
            }
            ResetAnimator();
        }

        public void ResetAnimator()
        {
            MovementDelta = Vector3.zero;
            RotationDelta = Quaternion.identity;
            
            _currentGroup = StepGroup.Opening;
            StartCoroutine(FadeInRig(0.1f));
        }

        /// <summary>
        /// Disable the component: unregister and zero out the rig.
        /// </summary>
        private void OnDisable()
        {
            Player.Instance.RaycastManager.enabled = false;
            legRig.weight = 0f;
            FixedUpdateManager.Instance?.Unregister(this);
        }

        /// <summary>
        /// Coroutine to gradually fade in the leg rig weight over a given duration.
        /// Invokes OnOpenFinished upon completion.
        /// </summary>
        public IEnumerator FadeInRig(float duration)
        {
            float elapsed = 0f;
            legRig.weight = 0f;
            ResetAllLegs();
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                legRig.weight = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            legRig.weight = 1f;
            _currentGroup = StepGroup.Idle;        
            ReturnLegToIdle(); 
            OnOpenFinished?.Invoke();
            IsReady = true;
        }

        /// <summary>
        /// Reset all leg positions via RaycastManager and reinitialize dynamics.
        /// </summary>
        public void ResetAllLegs()
        {
            var rm = Player.Instance.RaycastManager;
            if (rm.isActiveAndEnabled && IsInitialized())
            {
                rm.ExecuteResetPosition(_frontLeftFootAnim);
                rm.ExecuteResetPosition(_frontRightFootAnim);
                rm.ExecuteResetPosition(_rearLeftFootAnim);
                rm.ExecuteResetPosition(_rearRightFootAnim);
                InitializeSecondOrderDynamics();
                _currentGroup = StepGroup.Idle;
                _wasMoving = false;
            }
        }

        /// <summary>
        /// Determine whether leg animators are initialized.
        /// </summary>
        private bool IsInitialized()
        {
            return _frontLeftFootAnim != null && _frontRightFootAnim != null && _rearLeftFootAnim != null && _rearRightFootAnim != null;
        }

        /// <summary>
        /// Reset a single leg's transform, old/new positions, and lerp state.
        /// </summary>
        private void ResetLegRelativePosition(LegAnimator leg)
        {
            if (leg == null) return;
            var rb = Player.Instance.Rigidbody;
            Vector3 hipPos = rb.position + rb.rotation * leg.RelativePosition;
            Vector3 resetPos = hipPos - rb.transform.up * footHeight;
            leg.Transform.position = resetPos;
            leg.OldPosition = resetPos;
            leg.NewPosition = resetPos;
            leg.Lerp = 1f;
        }

        public List<Vector3> GetActualGroundNormals()
        {
            List<Vector3> normals = new List<Vector3>();

            foreach (var leg in _legs)
            {
                normals.Add(leg.Transform.up);
            }

            return normals;
        }
    }
}
