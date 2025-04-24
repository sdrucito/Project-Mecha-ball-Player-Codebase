using System;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public bool IsOpening = false;
    public bool IsClosing = false;

   

    public void Open()
    {
        IsOpening = true;
        animator.SetBool("IsOpening", IsOpening);
    }

    public void Close()
    {
        IsClosing = true;
        animator.SetBool("IsClosing", IsClosing);
    }

    
    public void OnOpenEnd()
    {
        IsOpening = false;
        animator.SetBool("IsOpening", IsOpening);
    }
    
    public void OnCloseEnd()
    {
        IsClosing = false;
        animator.SetBool("IsClosing", IsClosing);
    }
    
}
