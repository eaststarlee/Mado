using UnityEngine;

/// <summary>
/// 사망 시 플레이어 상태
/// </summary>
public class PlayerDeathState : PlayerState
{
    public PlayerDeathState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }
    
    public override void Enter()
    {
        base.Enter();
        
        // 물리 정지
        player.RB.linearVelocity = Vector2.zero;
        player.RB.bodyType = RigidbodyType2D.Kinematic;
        
        // Collider 비활성화 (충돌 방지)
        var collider = player.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 사망 이벤트 발생 (GameManager가 구독)
        GameEvents.RaisePlayerDeath();
        
        // TODO: 사망 애니메이션
    }
    
    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        // 이 State에서는 아무 전환도 하지 않음
        // GameManager가 리스폰 처리할 때까지 대기
        // GameManager에서 player.StateMachine.ChangeState(player.IdleState)로 강제 전환
    }
    
    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // 물리 업데이트 없음 (완전 정지 상태 유지)
        player.RB.linearVelocity = Vector2.zero;
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Rigidbody 복구 (GameManager에서 처리하므로 여기서는 중력만)
        if (player.ActiveFormData != null)
        {
            player.RB.gravityScale = player.ActiveFormData.gravity.scale;
        }
    }
}
