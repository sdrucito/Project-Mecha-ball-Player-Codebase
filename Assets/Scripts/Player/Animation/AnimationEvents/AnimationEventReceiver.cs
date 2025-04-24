using System.Collections.Generic;
using UnityEngine;

public class AnimationEventReceiver : MonoBehaviour
{
    [SerializeField] List<AnimationEvent> animationEvents = new();

    public void OnAnimationEventTriggered(string eventName)
    {
        AnimationEvent matchingEvent = animationEvents.Find(x => x.eventName == eventName);
        matchingEvent?.OnAnimationEvent?.Invoke();
    }
}
