using System.Collections.Generic;
using Player.Animation;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
#endif
public class LegRayDebugger : MonoBehaviour
{
    [SerializeField]
    private List<LegAnimator> legs = new List<LegAnimator>();
    public float maxDistance = 20f;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        foreach (var leg in legs)
        {
            Vector3 origin    = transform.position
                                + (leg.Transform.position - transform.position);
            Vector3 direction = Vector3.down;

            // Draw full test ray
            Gizmos.DrawLine(origin, origin + direction * maxDistance);

            // Show hit point
            if (Physics.Raycast(origin, direction, out var hit, maxDistance, LayerMask.GetMask("Ground")))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(origin, hit.point);
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.color = Color.yellow;  // reset for next leg
            }
        }
    }
}
