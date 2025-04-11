using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] protected float distToGround;

    public bool isGrounded;

    private void FixedUpdate()
    {
        GroundCheck();
    }

    public void GroundCheck()
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, distToGround))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;

        }
    }
}
