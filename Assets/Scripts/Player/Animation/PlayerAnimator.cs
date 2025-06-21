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
        private static readonly int TookDamage = Animator.StringToHash("TookDamage");
        private static readonly int IsDead = Animator.StringToHash("IsDead");
        private static readonly int Reset = Animator.StringToHash("Reset");
        

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
            _rigidbody=GetComponentInParent<Rigidbody>();
            Initialize();
        }
        #endregion

        #region Public API
        /// <summary>
        /// Reset all animator parameters and states
        /// </summary>
        public void Initialize()
        {
            // Reset all animator params
            _isOpened = true;
            _isClosed = false;
            _isOpening = false;
            _isClosing = false;
            animator.ResetTrigger(TookDamage);
            animator.ResetTrigger(IsDead);
            animator.SetBool("IsOpening", _isOpening);
            animator.SetBool("IsClosing", _isClosing);
        }
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
            if(_isOpened)
                animator.SetTrigger(TookDamage);
            else
            {
                Player.Instance.SetPlayerState(PlayerState.Unoccupied);
            }
        }
        
        /// <summary>
        /// Fires death animation: sets flags and Animator parameter.
        /// </summary>
        public void Die()
        {
            animator.SetTrigger(IsDead);
            animator.ResetTrigger(Reset);
        }
        
        /// <summary>
        /// Fires respawn to reset animator.
        /// </summary>
        public void Rebirth()
        {
            if(Player.Instance.PawnAttributes && _isOpened)
                animator.SetTrigger(Reset);
            animator.ResetTrigger(IsDead);
            animator.ResetTrigger(TookDamage);
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
        /// Called when damage animation finishes: resets flags
        /// </summary>
        public void OnDamageEnd()
        {
            animator.ResetTrigger(TookDamage);
            Player.Instance.SetPlayerState(PlayerState.Unoccupied);
        }
        #endregion
    }
}
