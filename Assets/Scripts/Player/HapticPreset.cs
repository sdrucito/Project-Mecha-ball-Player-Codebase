using UnityEngine;

[CreateAssetMenu(menuName = "Haptics/HapticPreset")]
public class HapticPreset : ScriptableObject
{
    [Tooltip("Unique identifier for preset. Used to reference it from code.")]
    public string IdName;
    [Tooltip("Low frequency vibration")][Range(0,1)]
    public float LowFrequency = 0.1f;
    [Tooltip("High frequency vibration")][Range(0,1)]
    public float HighFrequency = 0.1f;
    [Tooltip("Total duration of the vibration effect in seconds.")]
    public float Duration = 0.2f;
    [Tooltip("Vibration priority. Higher priority vibration will override lower ones.")]
    public int Priority = 0;
    
}