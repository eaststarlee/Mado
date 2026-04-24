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
    private bool wasRunawayLastFrame; // Debug 스팸 방지

    public PetFollowState(PetController pet, PetStateMachine stateMachine) 
        : base(pet, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        // velocity 강제 초기화 (Ghost 복귀 시 관성 제거)
        velocity = Vector2.zero;
        
        stuckTimer = 0f;
        wasRunawayLastFrame = false;
        justEnteredFollow = true; // 1프레임 보호 플래그
        
        // 초기 앵커 설정
        anchorPosition = pet.CalculateDesiredAnchor();
        
        // 초기 랜덤 오프셋 생성 및 동기화
        UpdateRandomOffset();
        currentRandomOffset = targetRandomOffset; // 초기에는 즉시 적용
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

        // 0. 쿨다운 체크 (Ghost 탈출 직후 재진입 방지)
        // 아주 드문 경합 상태 방지
        if (Time.time < pet.LastGhostExitTime + pet.PetData.ghostExitCooldown)
        {
            stuckTimer = 0f;
            return;
        }

        // 1. FarAway 체크 (시야 무관, 거리만으로 판단) - 빠른 합류용
        if (distanceToPlayer > pet.PetData.followToGhostDistance)
        {
            pet.CurrentGhostReason = GhostReason.FarAway;
            stateMachine.ChangeState(pet.GhostState);
            return;
        }

        // 2. Stuck 체크 (시야 기반)
        float hitDist;
        // Stuck 판정용: 수직 체크 포함 (ignoreY 없음) -> 천장 감지 가능
        bool hasLOS = pet.HasLineOfSightToPlayer(out hitDist);

        if (!hasLOS)
        {
            // [개선] 플레이어 안정성 체크 (속도 + 입력)
            // 플레이어가 의도적으로 이동 중(입력 존재)이거나, 빠른 속도로 이동 중이면 '잠시 가려진 것'으로 간주
            bool isPlayerMoving = pet.Player.RB.linearVelocity.magnitude > pet.PetData.playerStabilityThreshold 
                                  && Mathf.Abs(pet.Player.InputX) > 0.01f;

            if (!isPlayerMoving)
            {
                // 플레이어가 안정 상태(정지 or 미세 움직임/미끄러짐)인데 시야가 막힘 -> 진짜 끼임 가능성 높음
                float dt = Time.fixedDeltaTime; // LogicUpdate라면 Time.deltaTime
                
                // 가중치 (초근접 시 느리게)
                if (distanceToPlayer < pet.PetData.closeRangeThreshold)
                    stuckTimer += dt * pet.PetData.closeRangeTimerWeight;
                else
                    stuckTimer += dt;
            }
            else
            {
                // 플레이어가 이동 중이면 타이머 유보 (잔존물 제거 위해 서서히 감소)
                stuckTimer = Mathf.Max(0, stuckTimer - Time.deltaTime);
            }
        }
        else
        {
            // [개선] 시야 확보 시 즉시 리셋 (폭탄 제거)
            stuckTimer = 0f;
        }

        // 상태 전환
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
        
        // 1. 앵커 위치 갱신
        UpdateAnchor();
        
        // 2. 목표 위치 계산 (배회/추적)
        Vector2 finalTarget = CalculateTargetPosition(out float distance);
        
        // 3. 이동 속도(SmoothTime) 계산
        float smoothTime = CalculateSmoothTime(distance);
        
        // 4. 실제 이동 처리 (충돌 처리 포함)
        MovePet(finalTarget, smoothTime);
        
        // 5. 정체 감지 (LogicUpdate에서 처리하므로 여기서는 제거하거나 체크만)
        // CheckStuckAndTransition(distance, finalTarget); -> LogicUpdate로 이동됨
        
        // 6. 방향 전환
        UpdateFacing();
    }

    private void UpdateAnchor()
    {
        Vector2 desiredAnchor = pet.CalculateDesiredAnchor();
        anchorPosition = Vector2.Lerp(anchorPosition, desiredAnchor, pet.PetData.anchorUpdateSpeed * Time.fixedDeltaTime);
    }

    private Vector2 CalculateTargetPosition(out float distance)
    {
        // 거리 계산
        distance = Vector2.Distance(pet.transform.position, pet.Player.transform.position);

        // 랜덤 오프셋 부드럽게 전환
        currentRandomOffset = Vector2.Lerp(
            currentRandomOffset, 
            targetRandomOffset, 
            pet.PetData.randomOffsetBlendSpeed * Time.fixedDeltaTime
        );
        
        // 목표: 항상 앵커 + 랜덤 오프셋 (배회 기반)
        Vector2 target = anchorPosition + currentRandomOffset;

        // 둥실거림 추가
        if (pet.IsFloating)
        {
            float floatingY = Mathf.Sin(Time.time * pet.PetData.floatingFrequency) 
                             * pet.PetData.floatingAmplitude;
            target.y += floatingY;
        }

        return target;
    }

    private float CalculateSmoothTime(float distance)
    {
        // 거리에 따른 속도 블렌딩 (멀면 빠르고, 가까우면 느리게)
        float blendFactor = Mathf.InverseLerp(
            pet.PetData.hoverDistanceMin, 
            pet.PetData.hoverDistanceMax, 
            distance
        );
        
        return Mathf.Lerp(
            pet.PetData.hoverSmoothTime, 
            pet.PetData.chaseSmoothTime, 
            blendFactor
        );
    }

    private void MovePet(Vector2 target, float smoothTime)
    {
        // [개선 1] 도착 판정 (떨림 방지)
        float sqrDistance = (target - pet.RB.position).sqrMagnitude;
        float arrivalSqr = pet.PetData.arrivalThreshold * pet.PetData.arrivalThreshold;
        
        if (sqrDistance < arrivalSqr)
        {
            velocity = Vector2.zero;
            // Dynamic용: linearVelocity를 0으로 설정 (관성 제거)
            if (pet.RB.bodyType != RigidbodyType2D.Static)
                pet.RB.linearVelocity = Vector2.zero;
            return;
        }
        
        // [개선 2] 최대 속도 제한 (1차 안전장치)
        Vector2 newPosition = Vector2.SmoothDamp(
            pet.RB.position,
            target,
            ref velocity,
            smoothTime,
            pet.PetData.followMaxSpeed, // Infinity → 제한값
            Time.fixedDeltaTime
        );
        
        // [개선 3] 속도 폭주 하드 세이프티 (2차 안전장치)
        float runawayThresholdSqr = pet.PetData.runawayVelocityThreshold * pet.PetData.runawayVelocityThreshold;
        if (velocity.sqrMagnitude > runawayThresholdSqr)
        {
            velocity = velocity.normalized * pet.PetData.runawayVelocityThreshold;
            
            // Debug 스팸 방지
            if (!wasRunawayLastFrame)
            {
                Debug.LogWarning($"[PetFollowState] 속도 폭주 감지! velocity 강제 제한: {velocity.magnitude:F2}");
                wasRunawayLastFrame = true;
            }
        }
        else
        {
            wasRunawayLastFrame = false;
        }
        
        // Manual Wall Penetration Check Removed (Physics Engine handles it)
        
        // Dynamic용: linearVelocity 직접 설정 (MovePosition 대신)
        // SmoothDamp가 계산한 목표 위치로 이동하기 위한 속도 계산
        Vector2 desiredVelocity = (newPosition - pet.RB.position) / Time.fixedDeltaTime;
        
        if (pet.RB.bodyType != RigidbodyType2D.Static)
            pet.RB.linearVelocity = desiredVelocity;
    }

    private void UpdateFacing()
    {
        float distX = pet.Player.transform.position.x - pet.transform.position.x;
        if (Mathf.Abs(distX) > 0.5f) // 데드존
        {
            pet.Flip(distX > 0);
        }
    }
    
    private void UpdateRandomOffset()
    {
        // 원형 범위 내 랜덤 좌표 (목표만 설정, 실제 적용은 Lerp로)
        targetRandomOffset = Random.insideUnitCircle * pet.PetData.hoverRadius;
        
        // 다음 갱신 시간
        randomOffsetTimer = Random.Range(
            pet.PetData.changePosTimeMin, 
            pet.PetData.changePosTimeMax
        );
    }
}
