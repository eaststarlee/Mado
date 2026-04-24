using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 애니메이션 이벤트 → Module 콜백 중계 시스템.
/// Animator에서 발생하는 이벤트를 문자열 키로 중계.
/// Module.Enter() 시 구독, Module.Exit() 시 자동 정리.
/// 
/// 사용 예: Animator 클립에서 AnimationEvent 추가 →
/// Function: "OnAnimEvent", String: "HitboxOn"
/// </summary>
public class AnimationEventRouter : MonoBehaviour
{
    /// <summary>
    /// 이벤트 이름 → 콜백 목록.
    /// </summary>
    private Dictionary<string, List<EventEntry>> subscribers = new Dictionary<string, List<EventEntry>>();
    
    /// <summary>
    /// 구독 등록. Module.Enter()에서 호출.
    /// </summary>
    /// <param name="eventName">이벤트 이름 (히트박스 활성 등)</param>
    /// <param name="callback">실행할 콜백</param>
    /// <param name="owner">소유 모듈 (Exit 시 자동 해제용)</param>
    public void Subscribe(string eventName, Action callback, object owner)
    {
        if (!subscribers.ContainsKey(eventName))
        {
            subscribers[eventName] = new List<EventEntry>();
        }
        
        subscribers[eventName].Add(new EventEntry
        {
            callback = callback,
            owner = owner
        });
    }
    
    /// <summary>
    /// 특정 소유자의 모든 구독 해제. Module.Exit()에서 호출.
    /// </summary>
    public void UnsubscribeAll(object owner)
    {
        foreach (var kvp in subscribers)
        {
            kvp.Value.RemoveAll(entry => entry.owner == owner);
        }
    }
    
    /// <summary>
    /// 특정 이벤트의 모든 구독 해제.
    /// </summary>
    public void UnsubscribeEvent(string eventName)
    {
        if (subscribers.ContainsKey(eventName))
        {
            subscribers[eventName].Clear();
        }
    }
    
    /// <summary>
    /// Animator에서 호출하는 이벤트 핸들러.
    /// AnimationEvent의 Function에 "OnAnimEvent", 
    /// stringParameter에 이벤트 이름을 넣으면 된다.
    /// </summary>
    public void OnAnimEvent(string eventName)
    {
        if (!subscribers.ContainsKey(eventName)) return;
        
        var list = subscribers[eventName];
        for (int i = list.Count - 1; i >= 0; i--)
        {
            list[i].callback?.Invoke();
        }
    }
    
    /// <summary>
    /// 히트박스 ON 전용 단축 메서드. Animator에서 직접 호출 가능.
    /// </summary>
    public void HitboxOn()
    {
        OnAnimEvent("HitboxOn");
    }
    
    /// <summary>
    /// 히트박스 OFF 전용 단축 메서드. Animator에서 직접 호출 가능.
    /// </summary>
    public void HitboxOff()
    {
        OnAnimEvent("HitboxOff");
    }
    
    /// <summary>
    /// 모든 구독 해제 (안전장치).
    /// </summary>
    public void ClearAll()
    {
        subscribers.Clear();
    }
    
    private struct EventEntry
    {
        public Action callback;
        public object owner;
    }
}
