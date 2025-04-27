using UnityEngine;

namespace Player.Animation
{
    public class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private bool _isOpening = false;
        private bool _isClosing = false;


        private void Start()
        {
            Player player = Player.Instance;
            player.ControlModuleManager.GetModule("Ball").OnActivated += Close;
            //player.ControlModuleManager.GetModule("Walk").OnActivated += Open;
        }

        public void Open()
        {
            Player.Instance.Rigidbody.isKinematic = true;
            _isOpening = true;
            animator.SetBool("IsOpening", _isOpening);
            
        }

        public void Close()
        {
            
            _isClosing = true;
            animator.SetBool("IsClosing", _isClosing);
        }

    
        public void OnOpenEnd()
        {
            _isOpening = false;
            animator.SetBool("IsOpening", _isOpening);
        }
    
        public void OnCloseEnd()
        {
            _isClosing = false;
            animator.SetBool("IsClosing", _isClosing);
        }
    
    }
}
