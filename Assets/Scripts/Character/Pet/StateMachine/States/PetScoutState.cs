using UnityEngine;

public class PetScoutState : PetState
{
    public bool IsAtTarget { get; private set; }

    public PetScoutState(PetController pet, PetStateMachine stateMachine) 
        : base(pet, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        IsAtTarget = false;
        
        // 콜라이더 비활성화만 유지하고 반투명 유령화는 해제 (투명도 1 유지)
        pet.Collider.enabled = false;
        pet.SetAlpha(1f);
        pet.SetFloating(true);
    }

    public override void Exit()
    {
        base.Exit();
        
        IsAtTarget = false;
        
        // 콜라이더 복원 및 투명도 복원은 GhostState나 FollowState에서 처리될 수 있으나 
        // 여기서 안전장치로 복원해줍니다. 만약 이어지는 상태가 GhostState라면 
        // 그 상태의 Enter에서 다시 꺼질 것입니다.
        pet.Collider.enabled = true;
        pet.SetAlpha(1f);
        pet.SetFloating(true);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (pet.Player == null) return;

        // 플레이어의 의도가 사라지면 복귀
        if (pet.Player.VerticalLookIntention == 0)
        {
            // 벽 안에 있을 수 있으므로 우선 GhostState로 전환하여 안전하게 Follow 상태로 복귀 유도
            pet.CurrentGhostReason = GhostReason.ScoutReturn; 
            stateMachine.ChangeState(pet.GhostState);
            return;
        }

        float distanceToTarget = Vector2.Distance(pet.transform.position, GetTargetPosition());
        if (distanceToTarget <= pet.PetData.scoutArrivalThreshold)
        {
            IsAtTarget = true;
        }
        else
        {
            IsAtTarget = false;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (pet.Player == null) return;

        Vector2 targetPos = GetTargetPosition();
        
        // 둥실거림 추가
        float floatingY = Mathf.Sin(Time.time * pet.PetData.floatingFrequency) * pet.PetData.floatingAmplitude;
        targetPos.y += floatingY;

        Vector2 nextPos = Vector2.MoveTowards(pet.RB.position, targetPos, pet.PetData.scoutSpeed * Time.fixedDeltaTime);

        if (pet.RB.bodyType != RigidbodyType2D.Static)
        {
            pet.RB.linearVelocity = (nextPos - pet.RB.position) / Time.fixedDeltaTime;
        }

        // 항상 목표 지점 방향을 바라봄 (정찰 방향)
        float distX = targetPos.x - pet.transform.position.x;
        if (Mathf.Abs(distX) > 0.1f)
        {
            pet.Flip(distX > 0);
        }
    }

    private Vector2 GetTargetPosition()
    {
        float dirX = pet.Player.IsFacingRight ? 1f : -1f;
        int intentY = pet.Player.VerticalLookIntention; // 1 (Up) or -1 (Down)
        
        Vector2 targetOffset = intentY > 0 ? pet.PetData.scoutOffsetUp : pet.PetData.scoutOffsetDown;
        Vector2 offset = new Vector2(dirX * targetOffset.x, targetOffset.y);
        
        return (Vector2)pet.Player.transform.position + offset;
    }
}
