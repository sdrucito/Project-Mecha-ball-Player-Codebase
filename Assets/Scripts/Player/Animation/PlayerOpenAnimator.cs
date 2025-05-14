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
            StartCoroutine(AlignToNormalRoutine(_onAligned));
            //StartCoroutine(AlignFast(targetRot, 2.0f));
        }

        // Callback when the body is aligned
        private void OnAlignEndend()
        {
            playerAnimator.Open();
        }
 
        // PID Controller function
        public IEnumerator AlignToNormalRoutine(Action onAligned)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            Player player = Player.Instance;
            _integralError = 0f;
            bool success = true;
            while (true)
            {
                // Verify if the robot is still on the ground, if it changes ground abort the open and start again
                if (!player.IsGrounded())
                {
                    Debug.Log("Not grounded anymore");
                    success = false;
                    break;
                }

                
                Vector3 currentUp = transform.up;
                Vector3 groundNormal = player.GetGroundNormal();
                if (!IsGroundCoverageSufficient(rb.position, spotDimensions, groundNormal))
                {
                    success = false;
                    break;
                }
                Debug.DrawRay(transform.position, groundNormal*5f, Color.green,3f);

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

            if (success)
            {
                // Correctly aligned: can open
                onAligned?.Invoke();
            }
            else
            {
                // Rollback module switch
                Player.Instance.ControlModuleManager.RollbackSwitch();
            }
        }

        IEnumerator AlignFast(Quaternion targetRot, float duration)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            Player player = Player.Instance;
            Quaternion start = rb.rotation;
            float t = 0f;
            bool success = true;

            while (t < duration)
            {
                if (!player.IsGrounded())
                {
                    Debug.Log("Not grounded anymore");
                    success = false;
                    break;
                }
                t += Time.fixedDeltaTime;
                Quaternion r = Quaternion.Slerp(start, targetRot, t / duration);
                rb.MoveRotation(r);
                rb.AddForce(-rb.linearVelocity*speedDownForce, ForceMode.VelocityChange);
                yield return null;
            }
            if(success)
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
        */
        private bool IsAreaClear(Vector3 center, Vector2 halfExtentsXZ)
        {
            Vector3 halfExtents = new Vector3(halfExtentsXZ.x, 0.05f, halfExtentsXZ.y);
            Vector3 boxCenter = center + Vector3.down * 0.05f;
            Collider[] hits = Physics.OverlapBox(
                boxCenter,
                halfExtents,
                Quaternion.identity,
                groundLayerMask
            );
            return hits.Length == 0;
        }
        
        private bool IsGroundCoverageSufficient(
            Vector3 center,
            Vector2 halfExtentsXZ,
            Vector3 groundNormal,
            int samplesPerAxis = 10
        ) {
            int hits = 0;
            int total = samplesPerAxis * samplesPerAxis;
            float requiredCoverage = 0.7f;

            float halfHeight = 0.05f;

            float rayStartY = center.y + halfHeight + 0.01f;

            float rayLength = halfHeight + 1.5f;
            float dx = (halfExtentsXZ.x * 2f) / (samplesPerAxis - 1);
            float dz = (halfExtentsXZ.y * 2f) / (samplesPerAxis - 1);

            Vector3 origin = center;
            origin.y = rayStartY;
            Color debugColor = Color.green;

            for (int ix = 0; ix < samplesPerAxis; ix++) {
                for (int iz = 0; iz < samplesPerAxis; iz++) {
                    // compute sample position within box on XZ plane
                    float offsetX = -halfExtentsXZ.x + dx * ix;
                    float offsetZ = -halfExtentsXZ.y + dz * iz;
                    Vector3 samplePos = new Vector3(origin.x + offsetX, origin.y, origin.z + offsetZ);
                    Debug.DrawRay(samplePos, groundNormal * rayLength, debugColor, 0.1f);

                    // raycast straight down
                    if (Physics.Raycast(
                            samplePos,
                            Vector3.down,
                            out RaycastHit hit,
                            rayLength,
                            groundLayerMask
                        )) {
                        hits++;
                        debugColor = Color.green;
                    }
                    else
                    {
                        debugColor = Color.red;
                    }
                }
            }

            float coverage = (float)hits / total;
            Debug.Log("Computed coverage: " + coverage);
            return coverage >= requiredCoverage;
        }
        
        /*

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
