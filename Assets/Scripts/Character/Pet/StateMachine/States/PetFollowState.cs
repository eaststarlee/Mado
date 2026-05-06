using UnityEngine;

public class PetFollowState : PetState
{
    private Vector2 velocity; // SmoothDamp용
    private Vector2 anchorPosition; // 앵커 (부드럽게 업데이트)
    private Vector2 targetRandomOffset; // 목표 랜덤 오프셋
    private Vector2 currentRandomOffset; // 현재 랜덤 오프셋 (부드럽게 변화)
    private float randomOffsetTimer; // 랜덤 오프셋 갱신 타이머
    private float stuckTimer; // 정체 시간 측정
    
    // 급발진 방지 플래그
    private bool justEnteredFollow; // Enter 직후 1프레임 보호

    public PetFollowState(PetController pet, PetStateMachine stateMachine) 
        : base(pet, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        velocity = Vector2.zero;
        stuckTimer = 0f;
        justEnteredFollow = true; 
        
        // 초기 앵커 설정 (캐시된 데이터 사용)
        anchorPosition = pet.DesiredAnchor;
        
        UpdateRandomOffset();
        currentRandomOffset = targetRandomOffset;
    }

    // PetFollowState.cs Logic Update for Stuck Detection

    public override void LogicUpdate()
    {
        base.LogicUpdate();
        
        if (pet.Player == null) return;
        
        // Enter 직후 1프레임은 물리 판정 스킵
        if (justEnteredFollow)
        {
            justEnteredFollow = false;
        }
        
        // 거리 계산 (실제 유클리드 거리)
        float distance = Vector2.Distance(pet.transform.position, pet.Player.transform.position);
        
        // 여기서 Stuck 체크를 수행 (LogicUpdate에서 수행)
        CheckStuckAndTransition(distance);
        
        // 랜덤 오프셋 갱신 타이머
        randomOffsetTimer -= Time.deltaTime;
        if (randomOffsetTimer <= 0)
        {
            UpdateRandomOffset();
        }
    }
    
    // PhysicsUpdate에서는 이동만 처리

    private void CheckStuckAndTransition(float distanceToPlayer)
    {
        if (justEnteredFollow) { stuckTimer = 0f; return; }

        // 0. 쿨다운 체크
        if (Time.time < pet.LastGhostExitTime + pet.PetData.ghostExitCooldown)
        {
            stuckTimer = 0f;
            return;
        }

        // 1. R3 (Ghost Transition) 체크 (캐시된 거리 사용)
        if (pet.DistanceToPlayer > pet.PetData.ghostTransitionRadius)
        {
            pet.CurrentGhostReason = GhostReason.FarAway;
            stateMachine.ChangeState(pet.GhostState);
            return;
        }

        // 2. Stuck 체크 (캐시된 시야 정보 사용)
        if (!pet.HasLOS)
        {
            bool isPlayerMoving = pet.Player.RB.linearVelocity.magnitude > pet.PetData.playerStabilityThreshold 
                                  && Mathf.Abs(pet.Player.InputX) > 0.01f;

            if (!isPlayerMoving)
            {
                float dt = Time.fixedDeltaTime;
                if (pet.DistanceToPlayer < pet.PetData.closeRangeThreshold)
                    stuckTimer += dt * pet.PetData.closeRangeTimerWeight;
                else
                    stuckTimer += dt;
            }
            else
            {
                stuckTimer = Mathf.Max(0, stuckTimer - Time.deltaTime);
            }
        }
        else
        {
            stuckTimer = 0f;
        }

        if (stuckTimer > pet.PetData.stuckTimeThreshold)
        {
            pet.CurrentGhostReason = GhostReason.Stuck;
            stateMachine.ChangeState(pet.GhostState);
            stuckTimer = 0f;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        if (pet.Player == null) return;
        
        // 1. 앵커 위치 갱신 (캐시된 DesiredAnchor 사용)
        UpdateAnchor();
        
        // 2. 목표 위치 계산
        Vector2 finalTarget = CalculateElasticTarget();
        
        // 3. 거리에 따른 동적 SmoothTime 계산
        float smoothTime = CalculateDynamicSmoothTime(pet.DistanceToPlayer);
        
        // 4. 이동 처리
        MovePet(finalTarget, smoothTime);
        
        // 5. 방향 전환 (컨트롤러 통합 로직 호출)
        pet.HandleFacing();
    }

    private void UpdateAnchor()
    {
        anchorPosition = Vector2.Lerp(anchorPosition, pet.DesiredAnchor, pet.PetData.anchorUpdateSpeed * Time.fixedDeltaTime);
    }

    private Vector2 CalculateElasticTarget()
    {
        // 예측 타겟 계산
        Vector2 playerVelocity = pet.Player.RB.linearVelocity;
        Vector2 leadOffset = playerVelocity * pet.PetData.predictiveLeadFactor;

        // Predictive Target Clamp
        if (leadOffset.magnitude > pet.PetData.safeZoneRadius)
        {
            leadOffset = leadOffset.normalized * pet.PetData.safeZoneRadius;
        }

        // 랜덤 오프셋 보간
        currentRandomOffset = Vector2.Lerp(currentRandomOffset, targetRandomOffset, pet.PetData.randomOffsetBlendSpeed * Time.fixedDeltaTime);
        
        // 최종 목표
        Vector2 target = anchorPosition + leadOffset + currentRandomOffset;

        if (pet.IsFloating)
        {
            target.y += Mathf.Sin(Time.time * pet.PetData.floatingFrequency) * pet.PetData.floatingAmplitude;
        }

        return target;
    }

    private float CalculateDynamicSmoothTime(float distance)
    {
        if (distance <= pet.PetData.safeZoneRadius) return pet.PetData.hoverSmoothTime;
        
        float t = Mathf.InverseLerp(pet.PetData.safeZoneRadius, pet.PetData.elasticZoneRadius, distance);
        t = t * t; 

        return Mathf.Lerp(pet.PetData.hoverSmoothTime, pet.PetData.minCatchUpSmoothTime, t);
    }

    private void MovePet(Vector2 target, float smoothTime)
    {
        float sqrDistance = (target - pet.RB.position).sqrMagnitude;
        if (sqrDistance < pet.PetData.arrivalThreshold * pet.PetData.arrivalThreshold)
        {
            velocity = Vector2.zero;
            if (pet.RB.bodyType != RigidbodyType2D.Static) pet.RB.linearVelocity = Vector2.zero;
            return;
        }
        
        float currentMaxSpeed = Mathf.Max(pet.PetData.followMaxSpeed, pet.Player.RB.linearVelocity.magnitude + 5f);

        Vector2 newPosition = Vector2.SmoothDamp(pet.RB.position, target, ref velocity, smoothTime, currentMaxSpeed, Time.fixedDeltaTime);
        
        // 하드 세이프티
        float runawayThresholdSqr = pet.PetData.runawayVelocityThreshold * pet.PetData.runawayVelocityThreshold;
        if (velocity.sqrMagnitude > runawayThresholdSqr)
        {
            velocity = velocity.normalized * pet.PetData.runawayVelocityThreshold;
        }
        
        if (pet.RB.bodyType != RigidbodyType2D.Static)
            pet.RB.linearVelocity = (newPosition - pet.RB.position) / Time.fixedDeltaTime;
    }
    
    private void UpdateRandomOffset()
    {
        targetRandomOffset = Random.insideUnitCircle * (pet.PetData.safeZoneRadius * 0.5f);
        randomOffsetTimer = Random.Range(pet.PetData.changePosTimeMin, pet.PetData.changePosTimeMax);
    }
}
