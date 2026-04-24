using UnityEngine;

namespace Modules.Movement
{
    public enum FlyState
    {
        Patrol,     // 앵커 주변 배회 / 호버링
        Chase,      // 플레이어 추격 (Pet-like)
        SearchMove, // 놓친 위치로 이동
        SearchWait  // 제자리 탐색
    }

    /// <summary>
    /// 비행형 이동 모듈 (Fly). 
    /// PetController의 자연스러운 움직임(Anchor + RandomOffset + SineWave)을 차용.
    /// </summary>
    public class FlyModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Movement;
        public override int Priority => 1;
        public override string ModuleName => "Fly";

        private FlyModuleData data;

        // --- State Machine ---
        private FlyState currentState;
        private float stateTimer; 
        private float stuckTimer;
        private float chaseEntryTime;

        // --- Physics Variables (Pet Logic) ---
        private Vector2 currentVelocity; // SmoothDamp Velocity
        private float randomSeed;        // 비동기 둥실거림용
        
        // **Anchor System**
        private Vector2 anchorPosition;     // 현재 기준점 (타겟 또는 순찰 중심)
        private Vector2 targetRandomOffset; // 목표 랜덤 오프셋
        private Vector2 currentRandomOffset;// 현재 랜덤 오프셋 (Lerp)
        private float randomOffsetTimer;    // 오프셋 변경 타이머

        // Search
        private Vector2 searchDestination;

        public FlyModule(FlyModuleData data)
        {
            this.data = data;
        }

        public override bool CanExecute(EnemyBlackboard bb)
        {
            return true; // Always fly
        }

        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);

            // 초기화
            currentState = FlyState.Patrol;
            stateTimer = 0f;
            stuckTimer = 0f;
            currentVelocity = Vector2.zero; // 진입 시 관성 제거
            
            // 엇박자
            randomSeed = Random.Range(0f, 100f);
            
            // 초기 앵커
            anchorPosition = entity.transform.position;
            UpdateRandomOffset();
            currentRandomOffset = targetRandomOffset;

            // 물리 설정
            entity.Motor.SetGravityScale(0f);
            entity.Motor.Unfreeze();

            if (entity.Animator != null) entity.Animator.Play("Fly");
        }

        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            if (data == null)
            {
                State = ModuleState.Complete;
                return;
            }

            // 0. 타겟 감지 시 Chase 전환 (전역)
            if (currentState != FlyState.Chase && bb.Target.IsDetected)
            {
                TransitionTo(FlyState.Chase, entity);
            }

            // 1. 랜덤 오프셋 갱신 (공통 로직)
            randomOffsetTimer -= deltaTime;
            if (randomOffsetTimer <= 0)
            {
                UpdateRandomOffset();
            }

            // 2. 상태 실행
            switch (currentState)
            {
                case FlyState.Patrol:
                    ExecutePatrol(deltaTime, entity, bb);
                    break;
                case FlyState.Chase:
                    ExecuteChase(deltaTime, entity, bb);
                    break;
                case FlyState.SearchMove:
                    ExecuteSearchMove(deltaTime, entity, bb);
                    break;
                case FlyState.SearchWait:
                    ExecuteSearchWait(deltaTime, entity, bb);
                    break;
            }
        }

        private void TransitionTo(FlyState newState, EnemyEntity entity)
        {
            currentState = newState;
            stateTimer = 0f;
            stuckTimer = 0f;

            switch (newState)
            {
                case FlyState.Patrol:
                    if (entity.Animator != null) entity.Animator.Play("Fly");
                    // 앵커는 현재 위치에서 시작
                    anchorPosition = entity.transform.position;
                    break;

                case FlyState.Chase:
                    chaseEntryTime = Time.time;
                    if (entity.Animator != null) entity.Animator.Play("FlyChase");
                    // 앵커는 타겟 위치로 즉시 설정
                    break;

                case FlyState.SearchMove:
                    if (entity.Animator != null) entity.Animator.Play("Fly");
                    break;

                case FlyState.SearchWait:
                    if (entity.Animator != null) entity.Animator.Play("FlyIdle");
                    break;
            }
        }

        // ========================================================
        // 1. Patrol
        // ========================================================
        private void ExecutePatrol(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
        {
            // 앵커 위치 결정
            if (data.patrolType == FlyModuleData.FlyPatrolType.RandomWander)
            {
                // 여기서는 앵커 자체가 천천히 이동하거나 고정일 수 있음. 
                // 일단은 제자리 앵커(Spawn Point)를 유지한다고 가정하거나,
                // 현재 구현은 'AnchorPosition' 변수가 변하지 않으므로 제자리 배회.
            }
            
            // 펫 로직 이동 적용
            MovePetLike(entity, anchorPosition, deltaTime);
        }

        // ========================================================
        // 2. Chase
        // ========================================================
        private void ExecuteChase(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
        {
            // 타겟 놓침
            if (!bb.Target.IsDetected)
            {
                searchDestination = GetRandomSearchPoint(bb.Target.lastKnownPosition, 1.5f);
                TransitionTo(FlyState.SearchMove, entity);
                return;
            }

            // 1. 앵커 = 타겟 위치 (계속 갱신)
            anchorPosition = bb.Target.target.position;

            // 2. 펫 로직 이동
            MovePetLike(entity, anchorPosition, deltaTime);

            // 3. Stuck Check (벽 밀기 방지)
            // (펫 로직이므로 부드럽게 돌아가겠지만, 완전히 막힌 경우 처리)
            if (Time.time - chaseEntryTime > 1.0f)
            {
                CheckStuck(deltaTime, entity);
            }
        }

        // ========================================================
        // 3. SearchMove
        // ========================================================
        private void ExecuteSearchMove(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
        {
            stateTimer += deltaTime;
            if (stateTimer > 3.0f)
            {
                TransitionTo(FlyState.SearchWait, entity);
                return;
            }

            float dist = Vector2.Distance(entity.transform.position, searchDestination);
            if (dist < 0.5f)
            {
                TransitionTo(FlyState.SearchWait, entity);
                return;
            }

            // 앵커 = 수색 지점
            anchorPosition = searchDestination;
            MovePetLike(entity, anchorPosition, deltaTime);
        }

        // ========================================================
        // 4. SearchWait
        // ========================================================
        private void ExecuteSearchWait(float deltaTime, EnemyEntity entity, EnemyBlackboard bb)
        {
            stateTimer += deltaTime;
            
            // 앵커 = 제자리 (진입 시 위치 유지)
            // MovePetLike을 계속 호출하여 둥실거림 유지
            MovePetLike(entity, anchorPosition, deltaTime);

            if (stateTimer > 2.0f) // SearchWaitTime (하드코딩 or Data 추가)
            {
                TransitionTo(FlyState.Patrol, entity);
            }
        }

        // ========================================================
        // Core Logic: Pet-like Movement
        // ========================================================
        private void MovePetLike(EnemyEntity entity, Vector2 baseAnchor, float deltaTime)
        {
            // 1. 랜덤 오프셋 블렌딩
            currentRandomOffset = Vector2.Lerp(
                currentRandomOffset,
                targetRandomOffset,
                data.randomOffsetBlendSpeed * deltaTime
            );

            // 2. 목표 위치 계산: Anchor + RandomOffset + SineWave
            Vector2 finalTarget = baseAnchor + currentRandomOffset;

            // 둥실거림 (Sine Wave)
            float floatingY = Mathf.Sin((Time.time * data.floatingFrequency) + randomSeed) * data.floatingAmplitude;
            finalTarget.y += floatingY;

            // 3. SmoothTime 계산 (가변 반응성)
            float dist = Vector2.Distance(entity.transform.position, baseAnchor);
            float blendFactor = Mathf.InverseLerp(data.hoverDistanceMin, data.hoverDistanceMax, dist);
            float currentSmoothTime = Mathf.Lerp(data.hoverSmoothTime, data.chaseSmoothTime, blendFactor);

            // 4. SmoothDamp (Position -> Velocity)
            Vector2 nextPos = Vector2.SmoothDamp(
                entity.transform.position,
                finalTarget,
                ref currentVelocity,
                currentSmoothTime,
                data.maxSpeed,
                deltaTime
            );

            // 5. 회피 (Avoidance) - 부드럽게 추가
            Vector2 avoidVector = CalculateAvoidance(entity.transform.position, (finalTarget - (Vector2)entity.transform.position).normalized);
            
            // Velocity로 변환하여 적용 (Rigidbody 조작)
            Vector2 desiredVelocity = Vector2.zero;
            if (deltaTime > 0.0001f)
            {
                desiredVelocity = (nextPos - (Vector2)entity.transform.position) / deltaTime;
            }
            
            // 회피 벡터 합성 (NaN 방지)
            if (!float.IsNaN(avoidVector.x) && !float.IsNaN(avoidVector.y))
            {
                desiredVelocity += avoidVector;
            }

            // 최종 보호 (NaN 방지)
            if (!float.IsNaN(desiredVelocity.x) && !float.IsNaN(desiredVelocity.y))
            {
                entity.Rigidbody.linearVelocity = desiredVelocity;
            }

            // 6. Flip
            if (Mathf.Abs(desiredVelocity.x) > data.turnThreshold)
            {
                int facing = desiredVelocity.x > 0 ? 1 : -1;
                entity.Motor.SetFacing(facing);
                entity.Blackboard.Movement.facingDirection = facing;
            }
        }

        private void UpdateRandomOffset()
        {
            // 원형 범위 내 랜덤 좌표
            targetRandomOffset = Random.insideUnitCircle * data.hoverRadius;
            
            // 다음 갱신 시간 랜덤 설정
            randomOffsetTimer = Random.Range(data.changePosTimeRange.x, data.changePosTimeRange.y);
        }

        private Vector2 CalculateAvoidance(Vector2 origin, Vector2 dir)
        {
            if (dir.sqrMagnitude < 0.001f) return Vector2.zero; // 방향이 거의 0일 때 NaN 방지

            // Whiskers Avoidance (기존 유지)
            Vector2[] rays = new Vector2[] {
                dir,
                Quaternion.Euler(0, 0, 30) * dir,
                Quaternion.Euler(0, 0, -30) * dir
            };

            Vector2 avoidance = Vector2.zero;
            int hitCount = 0;

            foreach (var rayDir in rays)
            {
                RaycastHit2D hit = Physics2D.Raycast(origin, rayDir, data.rayDistance, data.obstacleLayer);
                if (hit.collider != null)
                {
                    avoidance += hit.normal;
                    hitCount++;
                }
            }
            
            if (hitCount > 0)
            {
                avoidance /= hitCount;
                return avoidance.normalized * data.avoidForce;
            }
            return Vector2.zero;
        }
        
        private void CheckStuck(float deltaTime, EnemyEntity entity)
        {
            // 속도는 있는데(이동 의지 O) 실제 이동 거리가 짧거나, 앞이 막힌 경우
            bool isBlocked = Physics2D.Raycast(entity.transform.position, entity.Motor.GetVelocity().normalized, 0.6f, data.obstacleLayer);
            
            if (isBlocked && entity.Rigidbody.linearVelocity.magnitude < data.maxSpeed * 0.1f)
            {
                stuckTimer += deltaTime;
                if (stuckTimer > data.stuckThreshold)
                {
                    // SearchMove로 탈출 시도
                    searchDestination = entity.transform.position + (Vector3)Random.insideUnitCircle * 2f;
                    TransitionTo(FlyState.SearchMove, entity);
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        private Vector2 GetRandomSearchPoint(Vector2 center, float radius)
        {
             for (int i = 0; i < 10; i++)
            {
                Vector2 randomPoint = center + Random.insideUnitCircle * radius;
                if (!Physics2D.OverlapCircle(randomPoint, 0.3f, data.obstacleLayer))
                {
                    return randomPoint;
                }
            }
            return center;
        }

        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            entity.Motor.Stop(true);
            base.Exit(entity, bb, context);
        }

        public override bool IsComplete() => false;
        public override string GetStatus() => $"{currentState}";
    }
}
