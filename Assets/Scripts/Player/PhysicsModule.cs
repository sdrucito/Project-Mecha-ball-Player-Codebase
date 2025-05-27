using System;
using System.Collections;
using System.Collections.Generic;
using Player.PlayerController;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// Carries collision details including Unity Collision, layer, and tag.
    /// </summary>
    public struct CollisionData
    {
        public Collision CollisionInfo;
        public int Layer;
        public string Tag;

        /// <summary>
        /// Constructs collision data record from collision event parameters.
        /// </summary>
        public CollisionData(Collision collisionInfo, int layer, string tag)
        {
            CollisionInfo = collisionInfo;
            Layer = layer;
            Tag = tag;
        }
    }

    /// <summary>
    /// Component that manages all physics interactions for the player,
    /// handling collision callbacks, grounding logic, and terrain-normal updates.
    /// </summary>
    public class PhysicsModule : MonoBehaviour
    {
        #region Serialized Parameters
        [Header("Ground Transition Settings")]
        [SerializeField] private float minCorrelationForTransition = 0.9f;
        #endregion

        #region State Fields
        private bool _isGrounded;
        private bool _isRotating = false;
        private int _groundLayer;
        private float _collisionAngle;
        private Vector3 _groundNormal = Vector3.up;
        private Vector2 _whitenScale = Vector2.one;
        #endregion

        #region Contact & Collision Queues
        private List<ContactPoint> _contactPoints = new List<ContactPoint>();
        private Queue<int> _collisionLayers = new Queue<int>();
        #endregion

        #region Public Properties
        /// <summary>
        /// Indicates whether the module is currently rotating to align with terrain.
        /// </summary>
        public bool IsRotating => _isRotating;
        #endregion

        #region Unity Callbacks
        /// <summary>
        /// Cache ground layer index on awake.
        /// </summary>
        private void Awake()
        {
            _groundLayer = LayerMask.NameToLayer("Ground");
        }
        #endregion

        #region Ground Check & Movement
        /// <summary>
        /// Updates grounded state when walking module is active based on raycast hits.
        /// </summary>
        private void SetWalkGrounded()
        {
            var raycastManager = Player.Instance.RaycastManager;
            var controlModule = Player.Instance.ControlModuleManager;
            if (controlModule != null && !controlModule.IsSwitching && controlModule.GetActiveModuleName() == "Walk")
            {
                var hits = raycastManager?.GetHitList();
                _isGrounded = hits != null && hits.Count > 0;
                if (_isGrounded)
                    UpdateGroundNormal(hits, raycastManager);
            }
        }

        /// <summary>
        /// Determines if movement is allowed given current grounded state and surface correlation.
        /// </summary>
        public bool CanMove(Vector3 movement)
        {
            if (!_isGrounded)
                return false;
            if (movement.magnitude <= 0f)
                return true;

            var hits = Player.Instance.RaycastManager.GetHitList();
            float sumCorrelation = 0f;
            foreach (var hit in hits)
            {
                float corr = GetMovementCorrelation(hit.point, movement);
                if (corr > 0f)
                    sumCorrelation += corr;
            }
            //Debug.Log("Computed correlation: " + sumCorrelation);
            return sumCorrelation > minCorrelationForTransition;
        }

        /// <summary>
        /// Handles falling behaviour by applying downward weight movement and switching mode.
        /// </summary>
        private void Fall()
        {
            //var characterController = Player.Instance.CharacterController;
            var movementDirection = Player.Instance.RaycastManager.GetMovementDelta();
            // apply gravity-influenced movement here as needed
            Player.Instance.ControlModuleManager.SwitchMode();
        }

        /// <summary>
        /// Updates the value of the scale used to remap uneven positioning of legs for movement correlation
        /// </summary>
        public void UpdateWhitenScale(Vector2 scale)
        {
            _whitenScale = scale;
        }
        #endregion

        #region Ground Normal Computation
        /// <summary>
        /// Updates terrain normal by averaging hit normals and triggers rotation.
        /// </summary>
        private void UpdateGroundNormal(List<RaycastHit> hits, RaycastManager raycastManager)
        {
            if (_isRotating) return;
            var normals = hits.ConvertAll(x => x.normal);
            Vector3 newNormal = AverageDirection(normals);
            if (newNormal != _groundNormal)
            {
                _groundNormal = newNormal;
                _isRotating = true;
            }
        }

        /// <summary>
        /// Computes normalized average direction of a list of vectors.
        /// </summary>
        private Vector3 AverageDirection(List<Vector3> vectors)
        {
            if (vectors == null || vectors.Count == 0)
                return Vector3.zero;
            Vector3 sum = Vector3.zero;
            foreach (var v in vectors)
                sum += v.normalized;
            return sum.sqrMagnitude < Mathf.Epsilon ? Vector3.zero : sum.normalized;
        }
        #endregion

        #region Collision Callbacks
        /// <summary>
        /// Called when physics collision begins: updates normal and grounding queue.
        /// </summary>
        public void OnEnterPhysicsUpdate(CollisionData hitData)
        {
            if (hitData.Layer == _groundLayer)
                _groundNormal = GetCollisionNormal(hitData);
            _collisionLayers.Enqueue(hitData.Layer);
            UpdateGrounded();
        }

        /// <summary>
        /// Called when physics collision ends: removes layer and updates grounding.
        /// </summary>
        public void OnExitPhysicsUpdate(CollisionData hitData)
        {
            TryDequeueTerrain(hitData);
            UpdateGrounded();
        }

        private void TryDequeueTerrain(CollisionData hitData)
        {
            if (_collisionLayers.Count > 0)
                _collisionLayers.Dequeue();
        }
        #endregion

        #region Grounded Queries
        /// <summary>
        /// Returns current grounded state, performing walk-specific check.
        /// </summary>
        public bool IsGrounded()
        {
            SetWalkGrounded();
            return _isGrounded;
        }

        /// <summary>
        /// Returns the current averaged ground normal.
        /// </summary>
        public Vector3 GetGroundNormal() => _groundNormal;

        private void UpdateGrounded()
        {
            _isGrounded = _collisionLayers.Contains(_groundLayer);
        }
        #endregion

        #region Collision Normal Helpers
        /// <summary>
        /// Computes the normal at collision point most aligned with downward direction.
        /// </summary>
        private Vector3 GetCollisionNormal(CollisionData hitData)
        {
            hitData.CollisionInfo.GetContacts(_contactPoints);
            GetCollisionPoint(out ContactPoint? cp);
            return cp?.normal ?? Vector3.zero;
        }

        /// <summary>
        /// Finds the contact point that maximizes downward correlation for grounding.
        /// </summary>
        private void GetCollisionPoint(out ContactPoint? contactPoint)
        {
            var rb = Player.Instance.Rigidbody;
            if (_contactPoints.Count == 0)
            {
                contactPoint = null;
                return;
            }
            float maxCorr = Vector3.Dot(rb.position - _contactPoints[0].point, Vector3.down);
            int idx = 0;
            for (int i = 1; i < _contactPoints.Count; i++)
            {
                float corr = Vector3.Dot(rb.position - _contactPoints[i].point, Vector3.down);
                if (corr > maxCorr) { maxCorr = corr; idx = i; }
            }
            contactPoint = _contactPoints[idx];
        }

        /// <summary>
        /// Calculates dot-product correlation between movement and flat surface vector.
        /// </summary>
        private float GetMovementCorrelation(Vector3 point, Vector3 velocity)
        {
            // 1) Get both vectors into the same coordinate space (let's pick local):
            Vector3 toP  = transform.InverseTransformPoint(point);
            Vector3 vel = transform.InverseTransformDirection(velocity);

            // 2) Drop the up‐component
            toP.y  = 0f;
            vel.y = 0f;

            // 3) Whiten (i.e. scale X and Z by the inverse‐half‐extents):
            toP.x *= _whitenScale.x;
            toP.z *= _whitenScale.y;
            vel.x *= _whitenScale.x;
            vel.z *= _whitenScale.y;

            // 4) Normalize and dot
            toP.Normalize();
            vel.Normalize();
            return Vector3.Dot(toP, vel);
        }
        /*
        private float GetMovementCorrelation(Vector3 point, Vector3 velocity)
        {
            Vector3 toPointFlat = Vector3.ProjectOnPlane(point - transform.position, _groundNormal).normalized;
            Vector3 velFlat = Vector3.ProjectOnPlane(velocity, _groundNormal).normalized;
            return Vector3.Dot(toPointFlat, velFlat);
        }
        */
        #endregion

        #region Rotation Completion
        /// <summary>
        /// Called by rotation tween when alignment ends.
        /// </summary>
        public void OnRotatingEnd()
        {
            _isRotating = false;
        }
        #endregion
    }
}
