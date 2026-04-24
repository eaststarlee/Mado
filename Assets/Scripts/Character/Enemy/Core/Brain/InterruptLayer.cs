using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인터럽트 계층. 피격/스턴/사망 이벤트를 큐로 수신하고,
/// 우선순위 판정 + 중복 필터링 + 실행을 전담.
/// 
/// Brain과 순환 참조 없음:
/// - Brain → InterruptLayer.ProcessQueue() 호출
/// - InterruptLayer → NeedsForceStop 플래그 반환
/// - Brain이 ForceStopAllChannels() 직접 실행
/// </summary>
public class InterruptLayer
{
    // --- 인터럽트 이벤트 큐 ---
    private Queue<InterruptEvent> queue = new Queue<InterruptEvent>();
    
    // --- 현재 실행 중인 인터럽트 모듈 ---
    private BehaviorModule currentInterrupt;
    private ModuleRuntimeContext interruptContext = new ModuleRuntimeContext();
    
    // --- Reaction 모듈 참조 ---
    private BehaviorModule hitReaction;
    private BehaviorModule stunReaction;
    private BehaviorModule deathModule;
    
    // --- Brain에 전달할 명령 플래그 (순환 참조 방지) ---
    
    /// <summary>
    /// true이면 Brain이 모든 Movement/Action 채널을 중단해야 함.
    /// Brain.Tick()에서 확인 후 직접 처리하고 ClearForceStop() 호출.
    /// </summary>
    public bool NeedsForceStop { get; private set; }
    
    /// <summary>
    /// 현재 인터럽트 실행 중 여부.
    /// </summary>
    public bool IsInterrupted => currentInterrupt != null && !currentInterrupt.IsComplete();
    
    /// <summary>
    /// 현재 실행 중인 인터럽트 모듈.
    /// </summary>
    public BehaviorModule Current => currentInterrupt;
    
    // ========================================================
    // 초기화
    // ========================================================
    
    public void Initialize(BehaviorModule hitReaction, BehaviorModule stunReaction, BehaviorModule death)
    {
        this.hitReaction = hitReaction;
        this.stunReaction = stunReaction;
        this.deathModule = death;
    }
    
    // ========================================================
    // 이벤트 수신
    // ========================================================
    
    /// <summary>
    /// 인터럽트 이벤트 큐에 추가.
    /// EnemyEntity.HandleHit → Brain.EnqueueInterrupt → 여기로 전달.
    /// </summary>
    public void Enqueue(InterruptType type, DamageInfo? info = null)
    {
        queue.Enqueue(new InterruptEvent { type = type, damageInfo = info });
    }
    
    // ========================================================
    // 큐 처리 (Brain.Tick()에서 호출)
    // ========================================================
    
    /// <summary>
    /// 큐에 쌓인 이벤트를 우선순위 순으로 처리.
    /// Brain 역참조 없이 NeedsForceStop 플래그만 설정.
    /// </summary>
    public void ProcessQueue(EnemyEntity entity, EnemyBlackboard bb)
    {
        while (queue.Count > 0)
        {
            var evt = queue.Dequeue();
            
            switch (evt.type)
            {
                case InterruptType.Death:
                    // 사망: 최고 우선순위, 무조건 중단
                    NeedsForceStop = true;
                    ForceModule(deathModule, entity, bb, evt.damageInfo);
                    bb.SetFlag(StatusFlag.IsDead);
                    return; // 사망 후 더 이상 이벤트 처리 불필요
                    
                case InterruptType.Stun:
                    NeedsForceStop = true;
                    ForceModule(stunReaction, entity, bb, null);
                    bb.SetFlag(StatusFlag.IsStunned);
                    break;
                    
                case InterruptType.Hit:
                    // 피격 중 재피격 → RefreshHit
                    if (currentInterrupt == hitReaction && 
                        currentInterrupt is Modules.Reaction.HitReactionModule hitModule)
                    {
                        if (evt.damageInfo.HasValue)
                        {
                            hitModule.RefreshHit(evt.damageInfo.Value, entity, bb, interruptContext);
                        }
                        break;
                    }
                    
                    // 현재 인터럽트가 중단 불허 → 무시 (슈퍼아머 등)
                    if (currentInterrupt != null && !currentInterrupt.CanBeInterrupted())
                    {
                        Debug.Log($"[InterruptLayer] Hit ignored: current {currentInterrupt.ModuleName} cannot be interrupted.");
                        break;
                    }
                    
                    // 일반 피격
                    Debug.Log("[InterruptLayer] Processing Hit Interrupt -> ForceModule");
                    NeedsForceStop = true;
                    ForceModule(hitReaction, entity, bb, evt.damageInfo);
                    bb.SetFlag(StatusFlag.IsHit);
                    break;
            }
        }
    }
    
    /// <summary>
    /// Brain이 ForceStop 처리 후 플래그 클리어.
    /// </summary>
    public void ClearForceStop()
    {
        NeedsForceStop = false;
    }
    
    // ========================================================
    // 인터럽트 실행 (Brain.Tick()에서 호출)
    // ========================================================
    
    /// <summary>
    /// 현재 인터럽트 모듈 실행. 완료 시 자동 Exit + 플래그 정리.
    /// </summary>
    public void ExecuteCurrent(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
    {
        if (currentInterrupt == null) return;
        
        interruptContext.ElapsedTime += deltaTime;
        currentInterrupt.Execute(deltaTime, entity, bb, interruptContext);
        
        if (currentInterrupt.IsComplete())
        {
            currentInterrupt.Exit(entity, bb, interruptContext);
            
            // 상태 플래그 정리
            bb.ClearFlag(StatusFlag.IsHit);
            bb.ClearFlag(StatusFlag.IsStunned);
            
            currentInterrupt = null;
        }
    }
    
    // ========================================================
    // 내부: 강제 모듈 교체
    // ========================================================
    
    private void ForceModule(BehaviorModule module, EnemyEntity entity, EnemyBlackboard bb, DamageInfo? damageInfo)
    {
        // 기존 인터럽트 종료
        if (currentInterrupt != null && !currentInterrupt.IsComplete())
        {
            currentInterrupt.Exit(entity, bb, interruptContext);
        }
        
        currentInterrupt = module;
        interruptContext.Reset();
        
        // DamageInfo를 Enter() 전에 설정 (모듈 Enter에서 읽으므로)
        if (damageInfo.HasValue)
        {
            interruptContext.StoredDamageInfo = damageInfo.Value;
        }
        
        currentInterrupt?.Enter(entity, bb, interruptContext);
    }
}

// ========================================================
// 인터럽트 관련 구조체
// ========================================================

/// <summary>
/// 인터럽트 이벤트 종류.
/// </summary>
public enum InterruptType
{
    Hit,
    Stun,
    Death
}

/// <summary>
/// 인터럽트 이벤트 데이터.
/// </summary>
public struct InterruptEvent
{
    public InterruptType type;
    public DamageInfo? damageInfo;
}
