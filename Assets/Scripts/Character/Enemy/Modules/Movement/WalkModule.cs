using UnityEngine;

namespace Modules.Movement
{
    public enum WalkState
    {
        Patrol,
        Chase,
        SearchMove, // 마지막 위치로 이동
        SearchWait  // 두리번거리기
    }

    /// <summary>
    /// 걷기 모듈 (Walk). Channel: Movement.
    /// 상태 머신: Patrol <-> Chase <-> Search(Move/Wait).
    /// </summary>
    public class WalkModule : BehaviorModule
    {
        public override ModuleChannel Channel => ModuleChannel.Movement;
        public override int Priority => 1;
        public override string ModuleName => "Walk";
        
        private WalkModuleData data;
        
        // --- State Machine ---
        private WalkState currentState;
        private float stateTimer; // SearchWait, Stuck 등 범용 타이머
        
        // --- Chase / Search Variables ---
        private Vector2 lastStuckPos;
        private float stuckTimer;
        private Vector2 searchDestination;
        private float searchMoveTimer; // 이동 타임아웃용

        public WalkModule(WalkModuleData data)
        {
            this.data = data;
        }
        
        public override bool CanExecute(EnemyBlackboard bb)
        {
            return bb.Movement.isGrounded;
        }
        
        public override void Enter(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            base.Enter(entity, bb, context);
            
            // 초기화
            currentState = WalkState.Patrol;
            
            // Patrol 초기화
            context.PatrolDirection = bb.Movement.facingDirection;
            if (context.PatrolDirection == 0) context.PatrolDirection = 1;
            context.IsWaiting = false;
            context.WaitTimer = 0f;
            context.ObstacleDetectionTimer = 0f;
            
            // Chase/Search 초기화
            stuckTimer = 0f;
            stateTimer = 0f;
            
            entity.Motor.Unfreeze();
            if (entity.Animator != null) entity.Animator.Play("Walk");
        }
        
        public override void Execute(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            if (data == null)
            {
                State = ModuleState.Complete;
                return;
            }
            
            // 전역 전환 조건: 감지 시 어디서든 Chase로 (Search 중 복귀 등)
            // 단, 이미 Chase인 경우는 내부 로직에 맡김 (Stuck 처리 등을 위해)
            if (currentState != WalkState.Chase && bb.Target.IsDetected)
            {
                TransitionTo(WalkState.Chase, entity);
            }

            switch (currentState)
            {
                case WalkState.Patrol:
                    ExecutePatrol(deltaTime, entity, bb, context);
                    break;
                case WalkState.Chase:
                    ExecuteChase(deltaTime, entity, bb, context);
                    break;
                case WalkState.SearchMove:
                    ExecuteSearchMove(deltaTime, entity, bb, context);
                    break;
                case WalkState.SearchWait:
                    ExecuteSearchWait(deltaTime, entity, bb, context);
                    break;
            }
        }

        private void TransitionTo(WalkState newState, EnemyEntity entity)
        {
            currentState = newState;
            stateTimer = 0f;
            stuckTimer = 0f;
            
            // 상태 진입 시 애니메이션/초기화
            switch (newState)
            {
                case WalkState.Patrol:
                    if (entity.Animator != null) entity.Animator.Play("Walk");
                    break;
                case WalkState.Chase:
                    if (entity.Animator != null) entity.Animator.Play("Walk"); // Run이 있다면 Run
                    lastStuckPos = entity.transform.position;
                    break;
                case WalkState.SearchMove:
                    if (entity.Animator != null) entity.Animator.Play("Walk");
                    searchMoveTimer = 0f;
                    break;
                case WalkState.SearchWait:
                    if (entity.Animator != null) entity.Animator.Play("Idle"); // 두리번 애니메이션
                    entity.Motor.Stop();
                    break;
            }
        }
        
        // ========================================================
        // 1. Patrol (순찰)
        // ========================================================
        private void ExecutePatrol(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 감지되면 Chase로 전환 (Execute 최상단에서 처리됨)
            
            // 대기 중
            if (context.IsWaiting)
            {
                context.WaitTimer -= deltaTime;
                entity.Motor.Stop();
                if (entity.Animator != null) entity.Animator.Play("Idle");
                
                if (context.WaitTimer <= 0f)
                {
                    context.IsWaiting = false;
                    context.PatrolDirection *= -1;
                    entity.Motor.SetFacing(context.PatrolDirection);
                    bb.Movement.facingDirection = context.PatrolDirection;
                    if (entity.Animator != null) entity.Animator.Play("Walk");
                }
                return;
            }
            
            // 장애물 감지
            if (bb.Movement.wallAhead || bb.Movement.ledgeAhead)
            {
                entity.Motor.Stop();
                context.ObstacleDetectionTimer += deltaTime;
                
                if (context.ObstacleDetectionTimer > 0.1f)
                {
                    context.IsWaiting = true;
                    context.WaitTimer = data.patrolWaitTime;
                    context.ObstacleDetectionTimer = 0f;
                }
                return;
            }
            else
            {
                context.ObstacleDetectionTimer = 0f;
            }
            
            // 이동
            float velocityX = context.PatrolDirection * data.patrolSpeed;
            entity.Motor.SetVelocityX(velocityX);
            entity.Motor.SetFacing(context.PatrolDirection);
            bb.Movement.facingDirection = context.PatrolDirection;
        }
        
        // ========================================================
        // 2. Chase (추격)
        // ========================================================
        private void ExecuteChase(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 1. 타겟 놓침? -> SearchMove
            if (!bb.Target.IsDetected)
            {
                // 마지막 위치를 목적지로 설정
                searchDestination = bb.Target.lastKnownPosition;
                TransitionTo(WalkState.SearchMove, entity);
                return;
            }
            
            // 2. Stuck 체크 (제자리 걸음)
            if (Vector2.Distance(entity.transform.position, lastStuckPos) < 0.05f)
            {
                stuckTimer += deltaTime;
                if (stuckTimer > data.stuckThreshold)
                {
                    // 막힘! -> SearchMove(마지막 위치) 또는 바로 SearchWait
                    // 타겟이 있는데 못 가는 상황이므로, 일단 마지막 위치(현재 타겟 위치)로 수색 시도
                    searchDestination = bb.Target.target.position; // 현재 위치
                    TransitionTo(WalkState.SearchMove, entity); // 또는 SearchWait
                    return;
                }
            }
            else
            {
                stuckTimer = 0f;
                lastStuckPos = entity.transform.position;
            }
            
            // 3. 거리 체크 (너무 멀어짐)
            // (옵션: ChaseGiveUpDistance가 Search로 이어지도록)
            if (bb.Target.distance > data.chaseGiveUpDistance)
            {
                 // 너무 멀면 바로 포기하고 순찰? 아니면 수색? 
                 // 일단 Patrol로 복귀 (너무 머니까)
                 TransitionTo(WalkState.Patrol, entity);
                 return;
            }
            
            // 4. 이동
            int chaseDir = bb.Target.direction.x > 0 ? 1 : -1;
            
            // 구덩이/벽 체크 (Chase 중에는 멈추되, StuckTimer가 돌아서 결국 포기하게 됨)
            if (bb.Movement.wallAhead || bb.Movement.ledgeAhead)
            {
                entity.Motor.Stop();
                if (entity.Animator != null) entity.Animator.Play("Idle");
                return;
            }
            
            // 이동
            float velocityX = chaseDir * data.chaseSpeed;
            entity.Motor.SetVelocityX(velocityX);
            entity.Motor.SetFacing(chaseDir);
            bb.Movement.facingDirection = chaseDir;
             if (entity.Animator != null) entity.Animator.Play("Walk");
        }
        
        // ========================================================
        // 3. SearchMove (수색 이동)
        // ========================================================
        private void ExecuteSearchMove(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            // 타임아웃
            searchMoveTimer += deltaTime;
            if (searchMoveTimer > data.searchMoveTimeout)
            {
                TransitionTo(WalkState.SearchWait, entity);
                return;
            }
            
            // 목적지 도착 확인
            float dist = Mathf.Abs(entity.transform.position.x - searchDestination.x); // X축 거리만 중요할 수 있음
            if (dist < data.arrivalDistance)
            {
                TransitionTo(WalkState.SearchWait, entity);
                return;
            }
            
            // 이동
            int moveDir = (searchDestination.x > entity.transform.position.x) ? 1 : -1;
            
            // 장애물 체크
             if (bb.Movement.wallAhead || bb.Movement.ledgeAhead)
            {
                entity.Motor.Stop();
                // 못 가면 바로 Wait으로 전환
                TransitionTo(WalkState.SearchWait, entity);
                return;
            }
            
            float velocityX = moveDir * data.searchSpeed;
            entity.Motor.SetVelocityX(velocityX);
            entity.Motor.SetFacing(moveDir);
            bb.Movement.facingDirection = moveDir;
        }
        
        // ========================================================
        // 4. SearchWait (수색 대기)
        // ========================================================
        private void ExecuteSearchWait(float deltaTime, EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            stateTimer += deltaTime;
            entity.Motor.Stop();
            
            // 일정 시간 후 Patrol 복귀
            if (stateTimer > data.searchDuration)
            {
                TransitionTo(WalkState.Patrol, entity);
                return;
            }
            
            // (옵션) 뒤를 돌아보는 연출 등을 추가할 수 있음
        }
        
        public override void Exit(EnemyEntity entity, EnemyBlackboard bb, ModuleRuntimeContext context)
        {
            entity.Motor.Stop();
            base.Exit(entity, bb, context);
        }
        
        public override bool IsComplete() => false;

        public override string GetStatus() => currentState.ToString();
    }
}
