using UnityEngine;

namespace Player.Animation
{
    /// <summary>
    /// Handles opening and closing animation states for the player character,
    /// coordinating Animator parameters and module activation callbacks.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Animator Reference")]
        [SerializeField] private Animator animator;
        #endregion

        #region Animation State Flags
        private bool _isOpening = false;
        private bool _isClosing = false;
        private static readonly int TookDamageHash = Animator.StringToHash("TookDamage");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        /*
         * Used as savestate to allow rollback between state switch
         */
        private bool _isOpened = false;
        private bool _isClosed = false;
        #endregion
        
        private Rigidbody _rigidbody;

        #region Unity Callbacks
        /// <summary>
        /// Subscribes to control module events and initializes open state.
        /// </summary>
        private void Start()
        {
            Player player = Player.Instance;
            player.ControlModuleManager.GetModule("Ball").OnActivated += Close;
            player.OnPlayerDeath += Die;
            _isOpened = true;
            _rigidbody=GetComponentInParent<Rigidbody>();
            //player.ControlModuleManager.GetModule("Walk").OnActivated += Open;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Begins open animation: sets flags and Animator parameter.
        /// </summary>
        public void Open()
        {
            _rigidbody.isKinematic = true;
            _isOpening = true;
            animator.SetBool("IsOpening", _isOpening);
            if (_isOpened)
            {
                OnOpenEnd();
            }
            else
            {
                Player.Instance.PlayerSound.Open();
            }
            
        }

        /// <summary>
        /// Begins close animation: sets flags and Animator parameter.
        /// </summary>
        public void Close()
        {
            _isClosing = true;
            animator.SetBool("IsClosing", _isClosing);
            if (_isClosed)
            {
                OnCloseEnd();
            }
            else
            {
                Player.Instance.PlayerSound.Close();
            }
        }

        /// <summary>
        /// Fires damage animation: sets flags and Animator parameter.
        /// </summary>
        public void TakeDamage()
        {
            animator.SetTrigger(TookDamageHash);
        }
        
        /// <summary>
        /// Fires death animation: sets flags and Animator parameter.
        /// </summary>
        public void Die()
        {
            animator.SetTrigger(IsDead);
        }
        #endregion

        #region Animation Event Handlers
        /// <summary>
        /// Called when open animation finishes: resets flags, activates next module.
        /// </summary>
        public void OnOpenEnd()
        {
            _isOpening = false;
            animator.SetBool("IsOpening", _isOpening);
            Player.Instance.ControlModuleManager.ActivateNextModule();
            _isClosed = false;
            _isOpened = true;
        }

        /// <summary>
        /// Called when close animation finishes: resets flags, activates next module.
        /// </summary>
        public void OnCloseEnd()
        {
            _isClosing = false;
            animator.SetBool("IsClosing", _isClosing);
            Player.Instance.ControlModuleManager.ActivateNextModule();
            _isClosed = true;
            _isOpened = false;
            
            Player.Instance.PhysicsModule.SaveReposition();
        }
        
        /// <summary>
        /// Called when close animation finishes: resets flags, activates next animation.
        /// </summary>
        public void OnDeathEnd()
        {
            _isClosing = false;
            animator.SetBool("IsClosing", _isClosing);
            Player.Instance.ControlModuleManager.ActivateNextModule();
            _isClosed = true;
            _isOpened = false;
            
            Player.Instance.PhysicsModule.SaveReposition();
        }
        
        /// <summary>
        /// Called when damage animation finishes: resets flags
        /// </summary>
        public void OnDamageEnd()
        {
            Player.Instance.SetPlayerState(PlayerState.Unoccupied);
        }
        #endregion
    }
}
