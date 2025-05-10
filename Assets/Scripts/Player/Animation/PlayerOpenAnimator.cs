using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player.Animation
{
    /*
     * Component that manages the logic of correctly position the player before starting
     * the switch animation of the robot
     */
    public class PlayerOpenAnimator : MonoBehaviour
    {
        
        [SerializeField] private PlayerAnimator playerAnimator;
        
        [SerializeField] private Vector2 searchExtents = new Vector2(5f, 5f);
        [SerializeField] private Vector2 spotDimensions = new Vector2(2f, 2f);
        [SerializeField] private LayerMask groundLayerMask;
        
        [Tooltip("Proportional gain on the orientation error (Nm per degree).")]
        public float kp = 10f;
        [Tooltip("Derivative gain on the angular velocity (Nm⋅s).")]
        public float kd = 2f;
        [Tooltip("Integrative gain on the angular velocity (Nm⋅s).")]
        public float ki =  1f;       
        public float maxIntegral = 0.5f;

        public float speedDownForce = 0.1f;
        float _integralError;
        
        [Tooltip("How many degrees of error counts as “aligned”.")]
        public float alignmentTolerance = 0.5f;
        private Action _onAligned;
        
        private void Start()
        {
            Player player = Player.Instance;

            player.ControlModuleManager.GetModule("Walk").OnActivated += OnOpen;
            _onAligned += OnAlignEndend;
            player.Rigidbody.solverIterations = 16;
            player.Rigidbody.solverVelocityIterations = 16;
        }

        // Function called when the player fires the switch to Walk state
        // Manages how the player ball should rotate before starting to switch
        private void OnOpen()
        {
            Vector3 projFwd = Vector3.ProjectOnPlane(transform.forward, Player.Instance.PhysicsModule.GetGroundNormal()).normalized;
            Quaternion targetRot = Quaternion.LookRotation(projFwd, Player.Instance.PhysicsModule.GetGroundNormal());
            StartCoroutine(AlignToNormalRoutine(Player.Instance.PhysicsModule.GetGroundNormal(), _onAligned));
            //StartCoroutine(AlignFast(targetRot, 2.0f));
        }

        // Callback when the body is aligned
        private void OnAlignEndend()
        {
            playerAnimator.Open();
        }
 
        // PID Controller function
        public IEnumerator AlignToNormalRoutine(Vector3 groundNormal, Action onAligned)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            _integralError = 0f;
            while (true)
            {
                Vector3 currentUp = transform.up;
                float angleDeg = Vector3.Angle(currentUp, groundNormal);
                if (angleDeg <= alignmentTolerance) break;

                Vector3 axis = Vector3.Cross(currentUp, groundNormal).normalized;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                _integralError += angleRad * Time.fixedDeltaTime;
                _integralError = Mathf.Clamp(_integralError, -maxIntegral, maxIntegral);

                Vector3 omegaAlong = Vector3.Dot(rb.angularVelocity, axis) * axis;

                Vector3 torque = axis * (kp * angleRad + ki * _integralError) - (kd * omegaAlong);

                rb.AddTorque(torque, ForceMode.Force);
                rb.AddForce(-rb.linearVelocity.normalized*speedDownForce, ForceMode.VelocityChange);
                yield return new WaitForFixedUpdate();
            }

            onAligned?.Invoke();
        }

        IEnumerator AlignFast(Quaternion targetRot, float duration)
        {
            Rigidbody rb = Player.Instance.Rigidbody;

            Quaternion start = rb.rotation;
            float t = 0f;

            while (t < duration)
            {
                t += Time.fixedDeltaTime;
                Quaternion r = Quaternion.Slerp(start, targetRot, t / duration);
                rb.MoveRotation(r);
                yield return null;
            }
            _onAligned?.Invoke();


        }
        
        /*
         // Method that takes the linear velocity and calculates the best position regarding to it
        private Vector3 FindBestPosition(List<Vector3> positions)
        {
            Vector3 movementDirection = Player.Instance.Rigidbody.linearVelocity;

            Vector3 posVector = Player.Instance.Rigidbody.position - positions[0];
            float bestPosVec = Vector3.Dot(posVector.normalized, movementDirection);
            int bestIndex = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                posVector = Player.Instance.Rigidbody.position - positions[i];
                float posVec = Vector3.Dot(posVector.normalized, movementDirection);
                if (posVec > bestPosVec)
                {
                    bestPosVec = posVec;
                    bestIndex = i;
                }
                
            }
            return positions[bestIndex];
        }
        
        private bool IsAreaClear(Vector3 center, Vector2 halfExtentsXZ)
        {
            Vector3 halfExtents = new Vector3(halfExtentsXZ.x, 0.05f, halfExtentsXZ.y);
            Vector3 boxCenter  = center + Vector3.down * 0.05f;
            Collider[] hits   = Physics.OverlapBox(
                boxCenter,
                halfExtents,
                Quaternion.identity,
                groundLayerMask
            );
            return hits.Length == 0;
        }
        
        
        
        private List<Vector3> FindFreeSpots()
        {
            List<Vector3> freeSpots = new List<Vector3>();
            Vector3 origin = transform.position;
            float halfW = spotDimensions.x * 0.5f;
            float halfD = spotDimensions.y * 0.5f;

            for (float dx = -searchExtents.x; dx <= searchExtents.x; dx += samplingStep)
            {
                for (float dz = -searchExtents.y; dz <= searchExtents.y; dz += samplingStep)
                {
                    Vector3 candidate = new Vector3(
                        origin.x + dx,
                        origin.y,
                        origin.z + dz
                    );

                    if (IsAreaClear(candidate, new Vector2(halfW, halfD)))
                        freeSpots.Add(candidate);
                }
            }

            return freeSpots;
        }
         */
        
    }
}
