using System;
using FMOD.Studio;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    private EventInstance walkingEvent;

    private void Start()
    {
        walkingEvent = AudioManager.Instance.CreateEventInstance(FMODEvents.Instance.robotWalkSound.eventReference);

    }

    public void StartWalking()
    {
        Debug.Log("StartWalking sound fired");
        PLAYBACK_STATE playbackState;
        walkingEvent.getPlaybackState(out playbackState);
        if (playbackState.Equals(PLAYBACK_STATE.STOPPED))
        {
            walkingEvent.start();
        }
    }

    public void StopWalking()
    {
        Debug.Log("StopWalking sound fired");

        walkingEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
    }

    public void TakeDamage()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotDamage.eventReference, transform.position);
    }

    public void SingleStep()
    {
        AudioManager.Instance.PlayOneShot(FMODEvents.Instance.robotStep.eventReference, transform.position);
    }
}
