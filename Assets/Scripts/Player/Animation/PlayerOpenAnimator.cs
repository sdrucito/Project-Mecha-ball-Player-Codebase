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
 
        public IEnumerator AlignToNormalRoutine(Action onAligned)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            Player player = Player.Instance;
            _integralError = 0f;

            float maxPTerm = 5f;
            float maxDTerm = 5f;
            float maxTorque = 15f;
            float maxAngularVelocity = 8f;

            bool success = true;
            while (true)
            {
                if (!player.IsGrounded())
                {
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
                if (angleDeg <= alignmentTolerance)
                    break;

                Vector3 axis = Vector3.Cross(currentUp, groundNormal).normalized;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                // Integral clamp
                _integralError += angleRad * Time.fixedDeltaTime;
                _integralError = Mathf.Clamp(_integralError, -maxIntegral, maxIntegral);

                // Proportional term + clamp
                float pTermUnclamped = kp * angleRad;
                float pTerm = Mathf.Clamp(pTermUnclamped, -maxPTerm, maxPTerm);

                // Derivative term (projected angular velocity) + clamp
                float rawD = Vector3.Dot(rb.angularVelocity, axis);
                float dTermUnclamped = kd * rawD;
                float dTerm = Mathf.Clamp(dTermUnclamped, -maxDTerm, maxDTerm);

                // Build torque and clamp total magnitude
                Vector3 torque = axis * (pTerm + ki * _integralError - dTerm);
                torque = Vector3.ClampMagnitude(torque, maxTorque);

                // Optionally cap the rigidbody’s angular velocity each frame
                if (rb.angularVelocity.sqrMagnitude > maxAngularVelocity * maxAngularVelocity)
                {
                    rb.angularVelocity = rb.angularVelocity.normalized * maxAngularVelocity;
                }

                // Apply forces
                rb.AddTorque(torque, ForceMode.Force);
                rb.AddForce(-rb.linearVelocity.normalized * speedDownForce, ForceMode.VelocityChange);

                yield return new WaitForFixedUpdate();
            }

            if (success)
            {
                player.PhysicsModule.ClearGroundData();
                onAligned?.Invoke();
            }
            else
            {
                Player.Instance.PlayerSound.OpenDenial();
                Player.Instance.ControlModuleManager.RollbackSwitch();
            }
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
