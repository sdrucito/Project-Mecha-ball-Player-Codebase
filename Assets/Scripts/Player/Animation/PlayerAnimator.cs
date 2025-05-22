using UnityEngine;

namespace Player.Animation
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private bool _isOpening = false;
        private bool _isClosing = false;

        /*
         * Used as savestate to allow rollback between state switch
         */
        private bool _isOpened = false;
        private bool _isClosed = false;
        private void Start()
        {
            Player player = Player.Instance;
            player.ControlModuleManager.GetModule("Ball").OnActivated += Close;
            _isOpened = true;
            //player.ControlModuleManager.GetModule("Walk").OnActivated += Open;
        }

        public void Open()
        {
            Player.Instance.Rigidbody.isKinematic = true;
            _isOpening = true;
            animator.SetBool("IsOpening", _isOpening);
            if (_isOpened)
            {
                OnOpenEnd();
            }
            else
            {
                AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotOpenEvent.eventReference, transform.position);
            }
            
        }

        public void Close()
        {
            _isClosing = true;
            animator.SetBool("IsClosing", _isClosing);
            if(_isClosed)
                OnCloseEnd();
        }

    
        public void OnOpenEnd()
        {
            _isOpening = false;
            animator.SetBool("IsOpening", _isOpening);
            Player.Instance.ControlModuleManager.ActivateNextModule();
            _isClosed = false;
            _isOpened = true;
        }
    
        public void OnCloseEnd()
        {
            _isClosing = false;
            animator.SetBool("IsClosing", _isClosing);
            Player.Instance.ControlModuleManager.ActivateNextModule();
            _isClosed = true;
            _isOpened = false;
        }
    
    }
}
