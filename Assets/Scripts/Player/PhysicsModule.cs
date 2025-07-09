using System;
using System.Collections;
using System.Collections.Generic;
using Player.PlayerController;
using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    /// <summary>
    /// Carries details for the last player position
    /// </summary>
    public struct PlayerRepositionInfo
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Normal;
    }

    /// <summary>
    /// Carries collision details including Unity Collision, layer, and tag.
    /// </summary>
    public struct CollisionData
    {
        public Collision CollisionInfo;
        public int Layer;
        public string Tag;
        public float VelocityMagnitude;

        /// <summary>
        /// Constructs collision data record from collision event parameters.
        /// </summary>
        public CollisionData(Collision collisionInfo, int layer, string tag, float velocityMagnitude)
        {
            CollisionInfo = collisionInfo;
            Layer = layer;
            Tag = tag;
            VelocityMagnitude = velocityMagnitude;
        }
    }

    /// <summary>
    /// Component that manages all physics interactions for the player,
    /// handling collision callbacks, grounding logic, and terrain-normal updates.
    /// </summary>
    public class PhysicsModule : MonoBehaviour, IFixedUpdateObserver
    {
        #region Serialized Parameters
        [Header("Ground Settings")]
        [SerializeField] private float minCorrelationMove = 0.9f;
        [SerializeField] private float ballHalfHeight = 0.5f;
        [SerializeField] private float repositionYOffset = 0.4f;
        #endregion

        #region State Fields
        private bool _isGrounded;
        private bool _isRotating = false;
        private int _groundLayer;
        private float _collisionAngle;
        private Vector3 _groundNormal = Vector3.up;
        private Vector2 _whitenScale = Vector2.one;
        private PlayerRepositionInfo _playerRepositionInfo;
        private Vector3 _lastPosition;
        private Vector3 _velocity;
        #endregion

        #region Contact & Collision Queues
        private List<ContactPoint> _contactPoints = new List<ContactPoint>();
        private List<int> _collisionLayers = new List<int>();
        private List<string> _collisionTags = new List<string>();
        #endregion
        public int FixedUpdatePriority { get; set; }

        #region Public Properties
        /// <summary>
        /// Indicates whether the module is currently rotating to align with terrain.
        /// </summary>
        public bool IsRotating => _isRotating;

        [SerializeField] private float maxAngularSpeed = 10f;
        /*
        /// <summary>
        /// Indicates whether the module has to reposition the player to the last saved position/rotation
        /// </summary>
        public bool RepositionFlag { get; set; }
*/
        #endregion

        #region Unity Callbacks
        /// <summary>
        /// Cache ground layer index on awake.
        /// </summary>
        private void Awake()
        {
            _groundLayer = LayerMask.NameToLayer("Ground");
            _lastPosition = Player.Instance.Rigidbody.position;
        }
        
        private void OnEnable() {
            FixedUpdateManager.Instance.Register(this);
        }

        private void OnDisable() {
            FixedUpdateManager.Instance?.Unregister(this);
        }

        public void ObservedFixedUpdate()
        {
            var currentPosition = Player.Instance.Rigidbody.position;
            _velocity = (currentPosition - _lastPosition) / Time.fixedDeltaTime;
            _lastPosition = currentPosition;
            LimitAngularVelocity();
        }

        #endregion

        #region Ground Check & Movement
        /// <summary>
        /// Updates grounded state when walking module is active based on raycast hits.
        /// </summary>
        private void UpdateGrounded()
        {
            var raycastManager = Player.Instance.RaycastManager;
            var controlModule = Player.Instance.ControlModuleManager;
            if (controlModule != null && !controlModule.IsSwitching && controlModule.GetActiveModuleName() == "Walk")
            {
                var hits = raycastManager?.GetHitList();
                _isGrounded = hits != null && hits.Count > 0;
                if (_isGrounded)
                    UpdateGroundNormal(hits);
            }
            else
            {
                UpdateBallGrounded();
            }
        }
        

        /// <summary>
        /// Determines if movement is allowed given current grounded state and surface correlation.
        /// </summary>
        public bool CanMove(Vector3 movement)
        {
            if (!_isGrounded)
            {
                return false;
            }

            if (movement.magnitude <= 0f)
            {
                return true;
            }

            var hits = Player.Instance.RaycastManager.GetHitList();
            float sumCorrelation = 0f;
            int corrCounter = 0;
            foreach (var hit in hits)
            {
                float corr = GetMovementCorrelation(hit.point, movement);
                if (corr > 0f)
                {
                    sumCorrelation += corr;
                    corrCounter++;
                }
            }
            
            return sumCorrelation > minCorrelationMove && corrCounter >= 2;
        }

        public void InjectGroundLayer()
        {
            _collisionLayers.Add(_groundLayer);
        }
        
        public void RemoveGroundLayer()
        {
            _collisionLayers.Remove(_groundLayer);
        }

        
        

        /// <summary>
        /// Updates the value of the scale used to remap uneven positioning of legs for movement correlation
        /// </summary>
        public void UpdateWhitenScale(Vector2 scale)
        {
            _whitenScale = scale;
        }
        
        public Vector3 GetVelocity()
        {
            return _velocity;
        }

        void LimitAngularVelocity()
        {
            var rb = Player.Instance.Rigidbody;

            float angularSpeed = rb.angularVelocity.magnitude;
            if (angularSpeed > maxAngularSpeed)
            {
                rb.angularVelocity = rb.angularVelocity.normalized * maxAngularSpeed;
            }
        }
        
        #endregion

        #region Ground Normal Computation
        /// <summary>
        /// Updates terrain normal by averaging hit normals and triggers rotation.
        /// </summary>
        private void UpdateGroundNormal(List<RaycastHit> hits)
        {
            //if (_isRotating) return;
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
            {
                sum += v.normalized;
            }
            return sum.sqrMagnitude < Mathf.Epsilon ? Vector3.zero : sum.normalized;
        }
        #endregion

        #region Collision Callbacks
        /// <summary>
        /// Called when physics collision begins: updates normal and grounding queue.
        /// </summary>
        public void OnEnterPhysicsUpdate(CollisionData hitData)
        {
            if (CanUpdateBallGround())
            {
                if (hitData.Layer == _groundLayer)
                {
                    Debug.Log("Adding new ground");
                    _groundNormal = GetCollisionNormal(hitData);
                }
                _collisionLayers.Add(hitData.Layer);
                _collisionTags.Add(hitData.Tag);
                UpdateBallGrounded();
                
                // Pass the velocity to modulate the volume of the hit ground
                Player.Instance.PlayerSound.HitGround(hitData.Tag, hitData.VelocityMagnitude);
            }
        }

        private static bool CanUpdateBallGround()
        {
            Player player = Player.Instance;
            return (player.ControlModuleManager.GetActiveModuleName() == "Ball" &&
                    !player.ControlModuleManager.IsSwitching) || (player.ControlModuleManager.GetActiveModuleName() == "Walk" &&
                                                                  player.ControlModuleManager.IsSwitching);
        }

        /// <summary>
        /// Called when physics collision ends: removes layer and updates grounding.
        /// </summary>
        public void OnExitPhysicsUpdate(CollisionData hitData)
        {
            Player player = Player.Instance;
            if (Player.Instance.ControlModuleManager.GetActiveModuleName() == "Ball" && !player.ControlModuleManager.IsSwitching)
            {
                TryDequeueTerrain(hitData);
                UpdateBallGrounded();
            }
            
        }

        private void TryDequeueTerrain(CollisionData hitData)
        {
            _collisionLayers.Remove(hitData.Layer);
            _collisionTags.Remove(hitData.Tag);
        }
        #endregion

        #region Grounded Queries
        /// <summary>
        /// Returns current grounded state, performing walk-specific check.
        /// </summary>
        public bool IsGrounded()
        {
            UpdateGrounded();
            return _isGrounded;
        }

        public bool IsWalkable()
        {
            return !_collisionTags.Contains("BallOnly");
        }

        /// <summary>
        /// Returns the current averaged ground normal.
        /// </summary>
        public Vector3 GetGroundNormal()
        {
            UpdateGrounded();
            return _groundNormal;
        }

        private void UpdateBallGrounded()
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
            return cp?.normal ?? Vector3.up;
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
            // Get both vectors into the same coordinate space:
            Vector3 toP  = transform.InverseTransformPoint(point);
            Vector3 vel = transform.InverseTransformDirection(velocity);

            // Drop the up‐component
            toP.y  = 0f;
            vel.y = 0f;

            // Whiten
            toP.x *= _whitenScale.x;
            toP.z *= _whitenScale.y;
            vel.x *= _whitenScale.x;
            vel.z *= _whitenScale.y;

            // Normalize and dot
            toP.Normalize();
            vel.Normalize();
            return Vector3.Dot(toP, vel);
        }

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
        
        #region Player Reposition

        public void RepositionOnFall()
        {
            Player player = Player.Instance;
            PlayerRepositionInfo playerRepositionInfo = _playerRepositionInfo;
            playerRepositionInfo.Position = player.ControlModuleManager.GetActiveModuleName() == "Walk"
                ? playerRepositionInfo.Position += _playerRepositionInfo.Normal * 2.0f
                : playerRepositionInfo.Position;
            if (player.RaycastManager.CanRepositionAfterFall(playerRepositionInfo, ballHalfHeight))
            {
                Reposition(playerRepositionInfo.Position, playerRepositionInfo.Rotation);
            }
            else
            {
                // Reposition player to the last spawn position
                Reposition(player.CurrentCheckpoint.position, player.CurrentCheckpoint.rotation);
            }
            Player.Instance.TakeDamage(10.0f);
        }

        public void Reposition(Vector3 position, Quaternion rotation)
        {
            Rigidbody rb = Player.Instance.Rigidbody;
            rb.isKinematic = true;
            rb.position = position;
            rb.position = position;
            rb.rotation = rotation;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }

        public void SaveReposition()
        {
            _playerRepositionInfo.Position = transform.position;
            _playerRepositionInfo.Position.y += repositionYOffset;
            _playerRepositionInfo.Rotation = transform.rotation;
            _playerRepositionInfo.Normal = _groundNormal;
        }

   
        #endregion


        public void ClearGroundData()
        {
            _contactPoints.Clear();
            _collisionLayers.Clear();
        }
    }
}
