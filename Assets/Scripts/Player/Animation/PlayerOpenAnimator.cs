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
                player.PhysicsModule.ClearGroundData();
                onAligned?.Invoke();
            }
            else
            {
                // Rollback module switch
                Player.Instance.PlayerSound.OpenDenial();
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
        
        private bool IsAreaClear(Vector3 center, Vector2 halfExtentsXZ)
        {
            Vector3 halfExtents = new Vector3(halfExtentsXZ.x, 0.05f, halfExtentsXZ.y);
            Vector3 boxCenter = center + Vector3.down * 0.05f;
            Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, Quaternion.identity, groundLayerMask);
            return hits.Length == 0;
        }
        
        private bool IsGroundCoverageSufficient(Vector3 center, Vector2 halfExtentsPlane, Vector3 groundNormal, float requiredCoverage = 0.7f, int samplesPerAxis = 10) {
            // Build an orthonormal basis (tangent, bitangent, normal)
            Vector3 n = groundNormal.normalized;
            // pick any vector not parallel to n:
            Vector3 arbitrary = Mathf.Abs(Vector3.Dot(n, Vector3.up)) < 0.9f ? Vector3.up : Vector3.right;
            Vector3 tangent = Vector3.Cross(arbitrary, n).normalized;
            Vector3 bitangent = Vector3.Cross(n, tangent);

            // Ray setup
            float halfHeight = 0.05f;        
            float startDist = halfHeight + 0.01f;
            float rayLength = halfHeight + 2.3f; 
            Vector3 rayDir = -n;             

            // Grid step sizes in plane‐local space
            float dx = (halfExtentsPlane.x * 2f) / (samplesPerAxis - 1);
            float dz = (halfExtentsPlane.y * 2f) / (samplesPerAxis - 1);

            int hits = 0;
            int total = samplesPerAxis * samplesPerAxis;

            // Sample loop
            for (int ix = 0; ix < samplesPerAxis; ix++) {
                for (int iz = 0; iz < samplesPerAxis; iz++) {
                    // plane offsets
                    float offX = -halfExtentsPlane.x + dx * ix;
                    float offZ = -halfExtentsPlane.y + dz * iz;

                    // sample origin: center lifted out along normal, then offset in plane
                    Vector3 sampleOrigin = center + n * startDist + tangent * offX + bitangent * offZ;

                    Debug.DrawRay(sampleOrigin, rayDir * rayLength,
                                  Color.Lerp(Color.red, Color.green, 0.5f),
                                  0.1f);

                    // cast along inverted ground normal
                    if (Physics.Raycast(sampleOrigin, rayDir, rayLength, groundLayerMask)) {
                        hits++;
                    }
                }
            }

            float coverage = (float)hits / total;

            return coverage >= requiredCoverage;
        }
        
    }
}
