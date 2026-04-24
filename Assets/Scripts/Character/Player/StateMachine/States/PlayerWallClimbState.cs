 using UnityEngine;

public class PlayerWallClimbState : PlayerState
{
    private float climbTimer;
    private float minClimbTimer;
    private float wallLossGraceTimer; // 물리 밀어내기로 인한 미세 이탈 방지용 은혜 시간
    private bool isCeilingHit;
    private bool isOffWall;

    public PlayerWallClimbState(PlayerController player, PlayerStateMachine stateMachine) : base(player, stateMachine)
    {
    }

    public override void Enter()
    {
        base.Enter();
        player.OnWallJump(); // 벽 점프 쿨타임 설정
        player.IsSprintJumping = false;
        
        // 플래그 및 타이머 초기화
        isCeilingHit = false;
        isOffWall = false;
        climbTimer = player.ActiveFormData.wall.climbDuration;
        minClimbTimer = 0.1f; // 최소 보장 시간 (애니메이션/로직 안정성)
        wallLossGraceTimer = 0.15f; // 최대 0.15초 동안은 벽에서 떨어져도 등반 유지

        // 초기 방향 설정
        int direction = player.LastWallDirection;
        player.CheckDirectionToFace(direction > 0);
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        climbTimer -= Time.deltaTime;
        if (minClimbTimer > 0) minClimbTimer -= Time.deltaTime;

        // 1. 벽 이탈 시 공중 상태로 전환
        if (isOffWall)
        {
            stateMachine.ChangeState(player.InAirState);
            return;
        }

        // 2. 천장 충돌 시 벽 슬라이드로 전환 (즉시 멈춤)
        if (isCeilingHit)
        {
            stateMachine.ChangeState(player.WallSlideState);
            return;
        }
        
        // [Legacy Support] GroundClimb 감지 및 LedgeClimb 전환
        if (ShouldCheckLedgeClimb() && CheckForGroundClimbLedge())
        {
            return; 
        }

        // 3. 등반 시간 종료 시 벽 슬라이드로 전환
        if (climbTimer <= 0)
        {
            stateMachine.ChangeState(player.WallSlideState);
            return;
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // 1. 벽 이탈 체크 (물리 업데이트에서 정확한 판단)
        // 유니티 물리엔진 겹침 보정(Depenetration)으로 인한 1프레임 미세 이탈을 방지하기 위해 유예 시간 적용
        if (!player.IsTouchingWall())
        {
            wallLossGraceTimer -= Time.fixedDeltaTime;
            if (wallLossGraceTimer <= 0)
            {
                isOffWall = true;
                return; // 완전히 이탈했으면 속도 적용 중단
            }
        }
        else
        {
            // 벽에 다시 확실히 닿았으면 은혜 시간 리셋
            wallLossGraceTimer = 0.15f;
        }

        // 2. 천장 충돌 체크 (최소 시간 이후부터 체크)
        if (minClimbTimer <= 0 && player.IsCeilinged())
        {
            isCeilingHit = true;
            return; // 천장에 닿았으면 상승 중단
        }

        // 3. 속도 적용 (정상 상태일 때만)
        if (!isCeilingHit && !isOffWall)
        {
            int direction = player.LastWallDirection;
            float baseClimbSpeed = player.ActiveFormData.wall.climbForce;
            
            // --- [Smooth Stop] 부드러운 감속 로직 ---
            float speedMultiplier = 1f;
            float slowdownDuration = player.ActiveFormData.wall.climbStopSmoothingDuration;

            // 남은 시간이 감속 구간보다 적으면 속도를 줄임 (Ease-Out)
            if (climbTimer < slowdownDuration)
            {
                // 비율 계산 (0 ~ 1)
                float ratio = climbTimer / slowdownDuration;
                // Mathf.SmoothStep: 부드러운 S자 곡선으로 감속 (1 -> 0)
                speedMultiplier = Mathf.SmoothStep(0f, 1f, ratio); 
            }
            // ------------------------------------------
            
            // X축: 벽 밀착 유지 (물리 반발을 이겨내기 위해 강하게 밀착)
            // Y축: 등반 속도 강제 (중력/외부 힘 무시하고 상승 보장) + 감속 적용
            Vector2 newVel = player.RB.linearVelocity;
            newVel.x = direction * 2.5f; // 기존 0.5f보다 강하게 벽에 눌러붙어 이탈 방지
            newVel.y = baseClimbSpeed * speedMultiplier;
            
            player.RB.linearVelocity = newVel;
        }
    }
    
    /// <summary>
    /// LedgeClimb 체크 조건 (유저 의도 + 상승 중)
    /// </summary>
    private bool ShouldCheckLedgeClimb()
    {
        // 1. 상승 중인가?
        // (PhysicsUpdate에서 강제로 올리므로 보통 true겠지만, 안전장치)
        if (player.RB.linearVelocity.y <= 0)
            return false;
        
        // 2. 벽 방향으로 입력하고 있는가? (유저 의도 확인)
        bool inputTowardsWall = player.InputX != 0 && Mathf.Sign(player.InputX) == player.WallDirection;
        
        return inputTowardsWall;
    }
    
    /// <summary>
    /// WallClimb 중 GroundClimb 레이어를 만났는지 확인하고 LedgeClimb 시도
    /// </summary>
    private bool CheckForGroundClimbLedge()
    {
        Vector2? climbTarget = player.LedgeDetector.ScanLedgeTarget();
        
        if (climbTarget.HasValue)
        {
            player.LedgeClimbState.SetTarget(climbTarget.Value);
            stateMachine.ChangeState(player.LedgeClimbState);
            return true;
        }
        
        return false;
    }
}
