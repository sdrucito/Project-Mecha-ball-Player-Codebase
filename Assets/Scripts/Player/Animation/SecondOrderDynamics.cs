using Unity.Mathematics.Geometry;
using UnityEngine;

/*
 * Math component that calculates the position of the feet raycast
 * for the player movement
 */
public class SecondOrderDynamics : MonoBehaviour
{
    private Vector3 xp;
    private Vector3 y, yd;
    private float k1, k2, k3;
    
    public void Initialize(float f, float z, float r, Vector3 x0)
    {
        // Compute constants
        k1 = z / (Mathf.PI * f);
        k2 = 1/ ((2 * Mathf.PI * f) * (2 * Mathf.PI * f));
        k3 = r * z / (2 * Mathf.PI * f);
        
        // Initialize variables
        xp = x0;
        y = x0;
        yd = Vector3.zero;
    }

    // Update is called once per frame
    public Vector3 UpdatePosition(float T, Vector3 x, Vector3? xd = null)
    {
        if (xd == null)
        {
            xd = (x - xp) / T;
            xp = x;
        }
        float k2_stable = Mathf.Max(k2, 1.1f * (T * T / 4 + T * k1 / 2)); // clamp k2 to guarantee stability
        y = y + T * yd;
        yd = yd + T * (x + k3 * xd.Value - y - k1*yd)/k2_stable;
        return y;
    }
}
