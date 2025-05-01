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
        };
    
        private readonly List<LegAnimator> _groupALegs = new List<LegAnimator>();
        private readonly List<LegAnimator> _groupBLegs = new List<LegAnimator>();
    
        private StepGroup _currentGroup = StepGroup.Idle;
        private bool _wasMoving = false;

    

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
        }

        void FixedUpdate()
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
            }
            
            // Allow step verification for Idle to fire grounding control
            if (_currentGroup == StepGroup.Idle)
            {
                StartStepForGroup(StepGroup.GroupA);
                StartStepForGroup(StepGroup.GroupB);
            }
            
            // If is floating, return legs to body position
            if (!Player.Instance.IsGrounded())
            {
                _currentGroup = StepGroup.Floating;
            }
            else if (_currentGroup == StepGroup.Floating)
            {
                _currentGroup = StepGroup.Idle;

            }
            if (_currentGroup == StepGroup.Floating)
            {
                StartStepForGroup(StepGroup.GroupA);
                StartStepForGroup(StepGroup.GroupB);
                MoveLegReturnToBody(_frontLeftFootAnim);
                MoveLegReturnToBody(_frontRightFootAnim);
                MoveLegReturnToBody(_rearLeftFootAnim);
                MoveLegReturnToBody(_rearRightFootAnim);
            }
            else
            {
                MoveLegStep(_frontLeftFootAnim);
                MoveLegStep(_frontRightFootAnim);
                MoveLegStep(_rearLeftFootAnim);
                MoveLegStep(_rearRightFootAnim);
            }

            _wasMoving = isMoving;
            
            if (Player.Instance.RaycastManager)
            {
                Player.Instance.RaycastManager.SetLegs(_legs);
            }
            //Debug.Log("Current group: " + _currentGroup);
            //Debug.Log("Current grounded: " + Player.Instance.IsGrounded());
        
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
    
        void MoveLegReturnToBody(LegAnimator legAnimator)
        {
            legAnimator.Transform.position = legAnimator.SecondOrderDynamics.UpdatePosition(Time.deltaTime, legAnimator.NewPosition);
        }


        void StartStepForGroup(StepGroup group)
        {
            if (Player.Instance.RaycastManager)
            {
                Player.Instance.RaycastManager.FlushRaycasts();
                
                List<LegAnimator> legToMove = (group == StepGroup.GroupA) ? _groupALegs : _groupBLegs;
                
                for (int i = 0; i < legToMove.Count; i++)
                {
                    
                    Player.Instance.RaycastManager.ExecuteStepForLeg(legToMove[i]);
                    
               
                }
            }
        }

        private void OnEnable()
        {
            Player.Instance.RaycastManager.enabled = true;
            ResetAllLegs();
            StartCoroutine(FadeInRig(1f));
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
        
        public IEnumerator FadeInRig(float duration)
        {
            float elapsed = 0f;
            legRig.weight = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                legRig.weight = Mathf.Lerp(0f, 1f, elapsed / duration);
                yield return null;
            }
            legRig.weight = 1f;
            // Re-Activate switch input
            PlayerInputManager.Instance.SetActionEnabled("ChangeMode", true);
            
        }

        public void ResetAllLegs()
        {
            ResetLegRelativePosition(_frontLeftFootAnim);
            ResetLegRelativePosition(_frontRightFootAnim);
            ResetLegRelativePosition(_rearLeftFootAnim);
            ResetLegRelativePosition(_rearRightFootAnim);
        }

        private void ResetLegRelativePosition(LegAnimator leg)
        {
            if (leg != null)
            {
                Rigidbody instanceRigidbody = Player.Instance.Rigidbody;
                Vector3 worldOffset = instanceRigidbody.rotation * leg.RelativePosition;
                Vector3 resetPos = instanceRigidbody.position + worldOffset;
                resetPos.y -= stepHeight;
                leg.Transform.position = resetPos;
                leg.OldPosition = resetPos;
                leg.NewPosition = resetPos;
            }
            
        }

    
    
    
    }
}