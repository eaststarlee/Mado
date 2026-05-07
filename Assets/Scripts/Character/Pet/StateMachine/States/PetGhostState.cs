using UnityEngine;

public class PetGhostState : PetState
{
    public PetGhostState(PetController pet, PetStateMachine stateMachine) 
        : base(pet, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        // 콜라이더 완전 비활성화
        pet.Collider.enabled = false;
        
        // 투명도 감소
        pet.SetAlpha(pet.PetData.ghostAlpha);
        
        // 둥실거림 활성화 (Ghost 상태 시에도 유지하도록 변경)
        pet.SetFloating(true);
    }

    public override void Exit()
    {
        base.Exit();
        
        // 콜라이더 복원
        pet.Collider.enabled = true;
        
        // 투명도 복원
        pet.SetAlpha(1f);
        
        // 둥실거림 활성화
        pet.SetFloating(true);
        
        // 탈출 시간 기록 및 이유 초기화
        pet.LastGhostExitTime = Time.time;
        pet.CurrentGhostReason = GhostReason.None;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        if (pet.Player == null) return;
        
        float distanceToAnchor = Vector2.Distance(pet.transform.position, pet.DesiredAnchor);
        
        // Follow로 복귀 (캐시된 데이터 사용)
        if (distanceToAnchor <= 0.1f || pet.DistanceToPlayer < pet.PetData.ghostToFollowDistance)
        {
            if (pet.HasLOS)
            {
                stateMachine.ChangeState(pet.GetDefaultPetState());
                return;
            }
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (pet.Player == null) return;
        
        float moveSpeed = pet.PetData.ghostSpeed; 
        
        if (pet.CurrentGhostReason == GhostReason.Stuck)
        {
            moveSpeed = pet.PetData.ghostStuckSpeed; 
        }
        else if (pet.CurrentGhostReason == GhostReason.FarAway)
        {
            moveSpeed = pet.PetData.ghostFastSpeed; 
        }

        // 목표를 캐시된 앵커로 설정
        Vector2 targetAnchor = pet.DesiredAnchor;
        
        // 고속 이동 시 둥실거림 속도 증폭
        float freqMultiplier = (pet.CurrentGhostReason == GhostReason.FarAway) ? pet.PetData.ghostFloatingFreqMultiplier : 1.0f;
        float floatingY = Mathf.Sin(Time.time * pet.PetData.floatingFrequency * freqMultiplier) 
                         * pet.PetData.floatingAmplitude;
        targetAnchor.y += floatingY;

        Vector2 targetPos = Vector2.MoveTowards(pet.RB.position, targetAnchor, moveSpeed * Time.fixedDeltaTime);
        
        if (pet.RB.bodyType != RigidbodyType2D.Static)
        {
            pet.RB.linearVelocity = (targetPos - pet.RB.position) / Time.fixedDeltaTime;
        }
        
        // 방향 전환 (컨트롤러 통합 로직 호출)
        pet.HandleFacing();
    }
}
