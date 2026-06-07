using UnityEngine;

namespace Mado.AnimationSystem
{
    public interface IAnimationEventListener
    {
        void OnAnimationEvent(string eventName);
    }
}
