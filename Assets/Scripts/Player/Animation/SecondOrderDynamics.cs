using Unity.Mathematics.Geometry;
using UnityEngine;

/// <summary>
/// Implements a second-order dynamic filter for smoothing input signals,
/// used to compute smoothed target positions for procedural animations.
/// </summary>
public class SecondOrderDynamics : MonoBehaviour
{
    #region State Variables
    private Vector3 xp;    // previous input position
    private Vector3 y;     // output position
    private Vector3 yd;    // output velocity
    private float k1;      // damping coefficient
    private float k2;      // stiffness coefficient
    private float k3;      // gain coefficient
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the dynamic constants and starting state for the filter.
    /// </summary>
    /// <param name="f">Natural frequency.</param>
    /// <param name="z">Damping ratio.</param>
    /// <param name="r">Response gain.</param>
    /// <param name="x0">Initial input/output position.</param>
    public void Initialize(float f, float z, float r, Vector3 x0)
    {
        // Compute filter coefficients based on frequency, damping, and gain
        k1 = z / (Mathf.PI * f);
        k2 = 1f / ((2f * Mathf.PI * f) * (2f * Mathf.PI * f));
        k3 = r * z / (2f * Mathf.PI * f);
        
        // Set initial state
        xp = x0;
        y = x0;
        yd = Vector3.zero;
    }
    #endregion

    #region Update
    /// <summary>
    /// Advances the filter by one timestep, returning the smoothed position output.
    /// </summary>
    /// <param name="T">Delta time since last update.</param>
    /// <param name="x">Current input target position.</param>
    /// <param name="xd">Optional input velocity; if null, estimated from input change.</param>
    /// <returns>Filtered output position.</returns>
    public Vector3 UpdatePosition(float T, Vector3 x, Vector3? xd = null)
    {
        // Estimate input velocity if not provided
        if (xd == null)
        {
            xd = (x - xp) / T;
            xp = x;
        }
        // Ensure numerical stability of stiffness term
        float k2Stable = Mathf.Max(k2, 1.1f * (T * T / 4f + T * k1 / 2f));
        
        // Integrate output position and velocity
        y += T * yd;
        yd += T * (x + k3 * xd.Value - y - k1 * yd) / k2Stable;
        
        return y;
    }
    #endregion
}
