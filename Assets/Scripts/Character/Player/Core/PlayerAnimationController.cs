using UnityEngine;
using System.Collections.Generic;

public class PlayerAnimationController : MonoBehaviour
{
    private Mado.AnimationSystem.ICharacterAnimator animator;
    
    public enum AnimPriority
    {
        Base = 1,
        Transition = 2,
        Action = 3,
        Reaction = 4,
        System = 5
    }

    private class ActiveAnimation
    {
        public string stateName;
        public AnimPriority priority;
        public float expireTime; 
        public bool hasExpiration;
    }

    private Dictionary<AnimPriority, ActiveAnimation> activeAnimations = new Dictionary<AnimPriority, ActiveAnimation>();
    
    public string CurrentPlayingAnimation { get; private set; }

    public void Initialize(Mado.AnimationSystem.ICharacterAnimator characterAnimator)
    {
        this.animator = characterAnimator;
    }

    public void SetBaseState(string stateName)
    {
        Play(stateName, AnimPriority.Base);
    }

    public void PlayAction(string stateName, AnimPriority priority, float duration = 0f, bool forceRestart = false)
    {
        Play(stateName, priority, duration, forceRestart);
    }

    public void ClearAction(AnimPriority priority)
    {
        if (activeAnimations.ContainsKey(priority))
        {
            activeAnimations.Remove(priority);
        }
    }

    public void ClearAllAboveBase()
    {
        activeAnimations.Remove(AnimPriority.Transition);
        activeAnimations.Remove(AnimPriority.Action);
        activeAnimations.Remove(AnimPriority.Reaction);
        activeAnimations.Remove(AnimPriority.System);
    }

    private void Play(string stateName, AnimPriority priority, float duration = 0f, bool forceRestart = false)
    {
        if (!activeAnimations.ContainsKey(priority))
        {
            activeAnimations[priority] = new ActiveAnimation();
        }

        var anim = activeAnimations[priority];
        anim.stateName = stateName;
        anim.priority = priority;
        anim.hasExpiration = duration > 0f;
        anim.expireTime = duration > 0f ? Time.time + duration : 0f;

        if (forceRestart && CurrentPlayingAnimation == stateName && GetHighestPriority() == priority)
        {
            if (animator != null)
            {
                animator.Play(stateName, true);
            }
        }
    }

    private AnimPriority GetHighestPriority()
    {
        for (int p = 5; p >= 1; p--)
        {
            AnimPriority prio = (AnimPriority)p;
            if (activeAnimations.ContainsKey(prio))
            {
                return prio;
            }
        }
        return AnimPriority.Base;
    }

    private void Update()
    {
        if (animator == null) return;

        // 만료된 애니메이션 정리
        List<AnimPriority> keysToRemove = new List<AnimPriority>();
        foreach (var kvp in activeAnimations)
        {
            if (kvp.Value.hasExpiration && Time.time >= kvp.Value.expireTime)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            activeAnimations.Remove(key);
        }

        // 최고 우선순위 애니메이션 찾기
        AnimPriority highestPriority = AnimPriority.Base;
        string nextAnimation = "";
        
        for (int p = 5; p >= 1; p--)
        {
            AnimPriority prio = (AnimPriority)p;
            if (activeAnimations.TryGetValue(prio, out ActiveAnimation activeAnim))
            {
                nextAnimation = activeAnim.stateName;
                highestPriority = prio;
                break;
            }
        }

        if (!string.IsNullOrEmpty(nextAnimation))
        {
            if (CurrentPlayingAnimation != nextAnimation)
            {
                animator.Play(nextAnimation);
                CurrentPlayingAnimation = nextAnimation;
            }
            else
            {
                // CharacterSpriteAnimator는 이미 같은 상태면 알아서 무시함
                animator.Play(nextAnimation);
            }
        }
    }
}
