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
        
        // 콜라이더 완전 비활성화 (제미나이 개선: Trigger보다 안전)
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
        
        // [개선] 탈출 시간 기록 및 이유 초기화
        pet.LastGhostExitTime = Time.time;
        pet.CurrentGhostReason = GhostReason.None;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        if (pet.Player == null) return;
        
        float distanceToPlayer = Vector2.Distance(pet.transform.position, pet.Player.transform.position);
        float distanceToAnchor = Vector2.Distance(pet.transform.position, pet.CalculateDesiredAnchor());
        
        // Follow로 복귀 (거리/도달 + LOS 체크)
        // 앵커에 완전히 도달했거나, 기존 조건(플레이어와 충분히 가까워짐)을 만족했을 때
        if (distanceToAnchor <= 0.1f || distanceToPlayer < pet.PetData.ghostToFollowDistance)
        {
            // 앵커 도달 조건 충족 시, 시야도 확보되었는지 확인
            // 수직/수평 모두 뚫려있어야 복귀 가능
            if (pet.HasLineOfSightToPlayer(out float _))
            {
                stateMachine.ChangeState(pet.GetDefaultPetState());
                return;
            }
        }
        
        // 텔레포트는 PetController에서 공통 처리
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        if (pet.Player == null) return;
        
        // GhostReason에 따른 속도 설정
        // [수정] FarAway(일반) 일때는 Inspector의 'Ghost Speed'(ghostSpeed)를 사용
        // Stuck(끼임) 일때는 새로운 'Ghost Stuck Speed'(ghostStuckSpeed)를 사용
        float moveSpeed = pet.PetData.ghostSpeed; 
        
        if (pet.CurrentGhostReason == GhostReason.Stuck)
        {
            moveSpeed = pet.PetData.ghostStuckSpeed; // 벽 통과 시 천천히 (안정감)
        }
        else if (pet.CurrentGhostReason == GhostReason.FarAway)
        {
            moveSpeed = pet.PetData.ghostSpeed; // 멀리서 올 땐 빠르게 (기존 값 사용)
        }

        // [수정] 목표를 플레이어가 아닌 '앵커(등 뒤)'로 설정하되, 플로팅 적용
        Vector2 targetAnchor = pet.CalculateDesiredAnchor();
        
        float floatingY = Mathf.Sin(Time.time * pet.PetData.floatingFrequency) 
                         * pet.PetData.floatingAmplitude;
        targetAnchor.y += floatingY;

        Vector2 targetPos = Vector2.MoveTowards(
            pet.RB.position,
            targetAnchor, 
            moveSpeed * Time.fixedDeltaTime
        );
        
        // Dynamic용: linearVelocity 직접 설정 (MovePosition 대신)
        Vector2 desiredVelocity = (targetPos - pet.RB.position) / Time.fixedDeltaTime;
        if (pet.RB.bodyType != RigidbodyType2D.Static)
        {
            pet.RB.linearVelocity = desiredVelocity;
        }
        
        // 방향 전환
        Vector2 direction = ((Vector2)pet.Player.transform.position - pet.RB.position).normalized;
        pet.Flip(direction.x > 0);
    }
}
