using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    
    public void TakeDamage()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotDamage.eventReference, transform.position);
    }

    public void Step()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotStep.eventReference, transform.position);
    }

    public void LegMove()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotLeg.eventReference, transform.position);
    }

    public void Open()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotOpen.eventReference, transform.position);
    }

    public void Close()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotClose.eventReference, transform.position);
    }

    public void HitGround(string surfaceTag, float velocity)
    {
        EventInstance instance = RuntimeManager.CreateInstance(FMODEvents.Instance.robotGroundHit.eventReference);
        RuntimeManager.AttachInstanceToGameObject(instance, transform);
        
        float volumeVelocity = RemapClamped(velocity, 0f, 15f, 0f, 1f);
        Debug.Log("Volume velocity: " + volumeVelocity + " and velocity: " + velocity);
        // For now it's only one sound, but potentially we can add a map of sounds to play based on the surface hit
        instance.setParameterByName("hit_intensity", volumeVelocity);
        instance.start();
        instance.release();
        //AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotGroundHit.eventReference, transform.position);
    }
    
    private float RemapClamped(float v, float inMin, float inMax, float outMin, float outMax)
    {
        if (Mathf.Approximately(inMax, inMin))
            return outMin;
        float t = (v - inMin) / (inMax - inMin);
        t = Mathf.Clamp01(t);
        return outMin + t * (outMax - outMin);
    }
}
