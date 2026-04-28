using UnityEngine;

public class PlayerInAirState : PlayerState
{
    // 이 상태 내에서 점프 관련 상태를 추적
    private bool isJumping;
    private bool jumpCut;
    private bool _doubleJumpRequested;
    
    // 더블 점프 지연 관련 변수
    private bool isDoubleJumpDelaying;
    private float doubleJumpDelayTimer;

    // 그래플 종료 후 관성 보존 타이머
    private float postGrappleTimer;


    public PlayerInAirState(PlayerController player, PlayerStateMachine stateMachine, Mado.Character.Animation.PlayerAnimType animType) : base(player, stateMachine, animType)
    {
    }

    public override void Enter()
    {
        base.Enter();
        
        // 상태 진입 시 초기화
        isDoubleJumpDelaying = false;
        doubleJumpDelayTimer = 0f;

        // 그래플 종료 후 관성 보존 타이머 설정
        // GrapplingState → InAirState 전환인 경우에만 활성화
        postGrappleTimer = 0f;
        if (stateMachine.PreviousState == player.GrapplingState)
        {
            float delay = player.GrappleData != null ? player.GrappleData.postGrappleControlDelay : 0.15f;
            postGrappleTimer = delay;
        }
        
        // 중력 복구 안전장치 (Glide에서 전환 실패 시 대비)
        if (player.RB.gravityScale == 0f)
        {
            player.RB.gravityScale = player.ActiveFormData.gravity.scale;
        }

        // 긴 벽 점프로부터 이어졌는지 확인하고, 위로 솟구치는 효과(pop)를 줍니다.
        if (player.WasLongWallJump)
        {
            player.RB.AddForce(Vector2.up * player.ActiveFormData.wall.wallJumpUpwardPopForce, ForceMode2D.Impulse);
            player.WasLongWallJump = false;
        }
        
        // 공중 상태에 진입하면, 코요테 타임이 남아있거나 점프 버퍼가 있는 경우 점프를 시도
        // 그렇지 않으면 일반적인 공중 상태(낙하 등)로 시작
        if (player.LastPressedJumpTime > 0 && player.LastOnGroundTime > 0)
        {
            // SprintJumpPrepareState를 거쳐서 왔다면 SprintJump를 실행
            if (stateMachine.PreviousState == player.SprintJumpPrepareState)
            {

                SprintJump();
            }
            else
            {
                Jump();
            }
            player.LastPressedJumpTime = 0; // 초기 점프에 사용된 버퍼를 소모
        }
        else
        {
            // 점프가 아닌, 그냥 절벽에서 떨어졌을 때
            isJumping = false;
        }
    }

    public override void Exit()
    {
        base.Exit();
        // 공중 상태를 나갈 때, 중력을 기본값으로 리셋
        player.SetGravityScale(player.ActiveFormData.gravity.scale);
        
        // [Safety] 상태 탈출 시 지연 취소 (중요)
        isDoubleJumpDelaying = false;
    }

    public override void LogicUpdate()
    {
        base.LogicUpdate();

        if (player.JumpInputUp)
        {
            if (player.RB.linearVelocity.y > 0 && isJumping && !jumpCut) // 중복 방지
            {
                // [개선] Sprint Jump도 일반 점프처럼 중력 조절만 사용 (자연스러운 가변 높이)
                // 즉시 속도 감소 제거 → 중력으로 자연스럽게 감속
                
                // 점프 컷 상태 플래그 설정 (PhysicsUpdate에서 중력 조절에 사용)
                jumpCut = true;
            }
        }
        
        // 변신 입력 체크 (↑ + A) - 최우선
        if (stateMachine.CurrentState != player.TransformState 
            && player.InputY > 0.5f 
            && player.ButtonAInput)
        {
            FormType nextForm = player.CurrentForm == FormType.Normal 
                ? FormType.Devil 
                : FormType.Normal;
            
            player.TransformState.SetTransform(nextForm, this);
            stateMachine.ChangeState(player.TransformState);
            return;
        }

        // 더블 점프 및 활공 로직 (우선순위 결정)
        
        // 1. 더블 점프 (최우선: 생존기)
        // 반응성을 위해 GetKeyDown 사용 + 버퍼 로직과 연계
        if (player.JumpInputDown && player.CanDoubleJump)
        {
            // 선입력 방지 및 버퍼 소모
            if (!isDoubleJumpDelaying) 
            {
                StartDoubleJumpAnticipation();
                return; // 더블 점프 처리했으므로 활공 체크 없이 리턴
            }
        }

        // 2. 활공 (차선: 의도된 비행) - GetKeyDown (재입력 필수!)
        // ⭐⭐⭐ 핵심: GetKeyDown - 더블 점프 후 키를 뗐다가 다시 눌러야만 활공
        // 이렇게 하면 "Z 꾹" 상태에서 자동으로 활공이 켜지지 않음
        if (player.JumpInputDown  // GetKey → GetKeyDown 변경!
            && player.RB.linearVelocity.y < -player.ActiveFormData.ability.glideFallThreshold
            && !player.IsTouchingWall()
            && !player.CanDoubleJump  // 더블 점프를 먼저 써야만 활공 가능
            && stateMachine.CurrentState != player.GlideState)
        {
            stateMachine.ChangeState(player.GlideState);
            return;
        }

        // ========== 렛지 클라임 체크 (Snap & Tween) ==========
        // [중요] 공중(Air)에 있을 때만 가능
        // [중요] 벽 방향으로 입력 중일 때만 체크
        if (!player.IsGrounded())
        {
            bool inputTowardsWall = (player.InputX > 0 && player.IsFacingRight) 
                                 || (player.InputX < 0 && !player.IsFacingRight);
            
            if (inputTowardsWall)
            {
                // 스캔 시도 (목표 좌표 계산)
                Vector2? climbTarget = player.LedgeDetector.ScanLedgeTarget();

                if (climbTarget.HasValue)
                {
                    // 목표 전달 및 상태 전환
                    player.LedgeClimbState.SetTarget(climbTarget.Value);
                    stateMachine.ChangeState(player.LedgeClimbState);
                    return;
                }
            }
        }
        
        // Wall Interaction Logic (Wall Coyote Time 적용) - 복구됨
        if (player.IsTouchingWall() || player.LastOnWallTime > 0) // Coyote Time 추가
        {
            // [개선] 점프 입력이 있다면, 벽점프를 최우선으로 시도 (Try 패턴)
            if (player.TryWallJump())
            {
                return; // 벽점프 성공 시 즉시 종료
            }
            
            // 실제로 벽에 접촉 중일 때만 WallSlideState로 전환
            if (player.IsTouchingWall())
            {
                // 점프 입력이 없거나 벽 점프가 실패했을 때만 벽 슬라이드를 고려합니다.
                // Wall Climb 후 또는 하강 중일 때 벽에 붙습니다.
                // 단, 벽 반대쪽으로 입력하고 있으면 벽에서 떨어지도록 합니다.
                if (player.LastPressedJumpTime <= 0) // 점프 입력이 없을 때만
                {
                    // Wall Climb에서 왔거나 하강 중일 때
                    bool shouldStick = (stateMachine.PreviousState == player.WallClimbState) || (player.RB.linearVelocity.y < 0);
                    
                    if (shouldStick)
                    {
                        // 벽 반대 방향으로 입력하고 있지 않으면 벽에 붙습니다 (벽 기준 판정)
                        if (player.InputX == 0 || Mathf.Sign(player.InputX) == player.WallDirection)
                        {
                            stateMachine.ChangeState(player.WallSlideState);
                            return;
                        }
                    }
                }
            }
        }
        
        // Dash transition - 복구됨
        if (player.LastPressedDashTime > 0 && player.CanDash())
        {
            player.LastPressedDashTime = 0f;
            stateMachine.ChangeState(player.DashState);
            return;
        }
        
        // Landing transition
        if (player.IsGrounded() && player.RB.linearVelocity.y < 0.01f)
        {
            // [ActionSystem] 착지 확정 이벤트 발생 (SlamAction 등에서 구독)
            player.RaiseGroundedConfirmed();
            
            if (player.SprintInputHeld)
            {
                stateMachine.ChangeState(player.SprintState);
            }
            else if (player.InputX != 0)
            {
                stateMachine.ChangeState(player.MoveState);
            }
            else
            {
                stateMachine.ChangeState(player.IdleState);
            }
        }
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();
        
        // [Fix] 반동(Recoil) 중이면 물리 엔진에 맡기고 이동/중력 로직을 스킵하여 X 넉백 보존
        if (player.IsRecoiling)
        {
            return;
        }
        
        // 1. 더블 점프 지연 (Anticipation) 처리
        if (isDoubleJumpDelaying)
        {
            // [Safety] 상태 확인 (현재 상태가 아니면 취소)
            if (stateMachine.CurrentState != this) 
            {
                isDoubleJumpDelaying = false;
                return;
            }
            
            // [Safety] 지연 중 착지했다면 취소 (LogicUpdate보다 늦게 돌 수 있음)
            if (player.IsGrounded())
            {
                isDoubleJumpDelaying = false;
                return;
            }

            doubleJumpDelayTimer -= Time.fixedDeltaTime;

            // [Game Feel] 상승 억제 (Braking) & 수평 감속
            // 프레임 독립적인 Damping 적용
            Vector2 vel = player.RB.linearVelocity;
            
            // X축: 약간의 저항감 (0.95 정도)
            float xDamping = Mathf.Pow(0.95f, Time.fixedDeltaTime * 60f);
            vel.x *= xDamping;

            // Y축: 상승 중이라면 급격히 제동 ("턱" 하고 걸리는 느낌)
            // 너무 강력하지 않게 0.3 ~ 0.5 정도의 base 사용
            if (vel.y > 0)
            {
                // 상승 관성을 부드럽게 죽임
                float yDamping = Mathf.Pow(0.5f, Time.fixedDeltaTime * 60f); 
                vel.y *= yDamping;
            }
            
            player.RB.linearVelocity = vel;

            // [Trigger] 시간 종료 시 발사
            if (doubleJumpDelayTimer <= 0f)
            {
                PerformDoubleJump();
            }
            // 지연 중에는 일반 공중 이동 로직(HandleAirMove)을 덮어쓸 필요 없음 (base.PhysicsUpdate에서 실행됨)
            // 다만 의도적인 감속을 위해 PhysicsUpdate 하단에 배치
            return;
        }

        // [New] 특수 행동 중에는 중력/이동 로직 스킵 (Slam 등)
        if (player.Combat != null && player.Combat.IsSpecialActionActive)
        {
            return;
        }
        


        // 스프린트 점프 상태 처리
        if (player.IsSprintJumping)
        {
            // 수평 속도 강제 유지
            player.RB.linearVelocity = new Vector2(player.SprintJumpVelocityX, player.RB.linearVelocity.y);
        }
        else if (postGrappleTimer > 0f)
        {
            // 그래플 종료 후 관성 서서히 죽이기 (Damping)
            postGrappleTimer -= Time.fixedDeltaTime;
            
            float damping = player.GrappleData != null ? player.GrappleData.postGrappleDamping : 0.85f;
            // 프레임 독립적인 감속 (1.0 = 유지, 0.0 = 급정지)
            float frameDamping = Mathf.Pow(damping, Time.fixedDeltaTime * 60f);
            
            player.RB.linearVelocity *= frameDamping;
        }
        else // 일반 공중 상태
        {
            // 공통 공중 이동 로직 사용
            HandleAirMove();
        }

        // 중력 조절
        if (postGrappleTimer > 0f)
        {
            // 그래플 직후 관성 보존 구간: Y 방향 관성을 짓누르는 무거운 중력 배율(fall) 적용을 막고 기본 중력만 적용
            player.SetGravityScale(player.ActiveFormData.gravity.scale);
        }
        else if (Mathf.Abs(player.RB.linearVelocity.y) < player.ActiveFormData.jump.jumpHangTimeThreshold)
        {
            player.SetGravityScale(player.ActiveFormData.gravity.scale * player.ActiveFormData.jump.jumpHangGravityMult);
        }
        else if (jumpCut && player.RB.linearVelocity.y > 0)
        {
            player.SetGravityScale(player.ActiveFormData.gravity.scale * player.ActiveFormData.jump.jumpCutGravityMult);
        }
        else if (isJumping && player.RB.linearVelocity.y > 0)
        {
             player.SetGravityScale(player.ActiveFormData.gravity.scale);
        }
        else
        {
            player.SetGravityScale(player.ActiveFormData.gravity.scale * player.ActiveFormData.gravity.fallGravityMult);
        }
        
        // 낙하 속도 제한 (관성 보존 중일 때는 20~30 가량의 돌진 속도 허용)
        if (postGrappleTimer <= 0f && player.RB.linearVelocity.y < -player.ActiveFormData.gravity.maxFallSpeed)
        {
            player.RB.linearVelocity = new Vector2(player.RB.linearVelocity.x, -player.ActiveFormData.gravity.maxFallSpeed);
        }
    }
    
    private void Jump()
    {
        isJumping = true;
        jumpCut = false;
        player.IsSprintJumping = false;
        player.RB.linearVelocity = new Vector2(player.RB.linearVelocity.x, player.ActiveFormData.jump.jumpForce);
    }

    /// <summary>
    /// 상승 공격(Rising Attack) 시 호출
    /// </summary>
    public void OnRisingAttack()
    {
        isJumping = true;
        jumpCut = false; // 강제 하강(JumpCut) 방지
        player.IsSprintJumping = false;
    }

    /// <summary>
    /// Pogo 반동 시 호출 (PlayerController에서 호출)
    /// </summary>
    public void OnPogoJump()
    {
        // 1. 점프 상태로 인식시킴 (중력 계산 로직에서 사용)
        isJumping = true;
        
        // 2. 점프 컷(Jump Cut) 방지
        // Pogo 직후 상승 관성을 유지하기 위해 초기화
        jumpCut = false;
        
        // 3. 스프린트 점프 해제 (순수 수직 반동)
        player.IsSprintJumping = false; 
    }

    private void SprintJump()
    {
        isJumping = true;
        jumpCut = false;
        player.IsSprintJumping = true;
        // SprintState에서 미리 저장해둔 속도를 사용하여 점프
        player.RB.linearVelocity = new Vector2(player.SprintJumpVelocityX, player.ActiveFormData.jump.sprintJumpForce);
    }
    
    private void StartDoubleJumpAnticipation()
    {
        // 상태 설정
        // player.CanDoubleJump = false; // [변경] 사용 시점에서 소모하도록 변경 (안전성)
        player.LastPressedJumpTime = 0; // 버퍼 초기화 (중복 방지)
        
        isDoubleJumpDelaying = true;
        
        // [변경] 인스펙터 값 사용
        doubleJumpDelayTimer = player.ActiveFormData.jump.doubleJumpAnticipationDelay;

        // [Game Feel] 시각적 피드백 (시작)
        // 예: "흡!" 하는 소리나 이펙트
        // player.ActionSystem.OnDoubleJumpCharge?.Invoke();
    }

    private void PerformDoubleJump()
    {
        // [Safety] 최종 확인 (상태 변경 등)
        if (!player.CanDoubleJump) 
        {
            isDoubleJumpDelaying = false;
            return;
        }

        isDoubleJumpDelaying = false;
        player.CanDoubleJump = false; // [Consume] 실제 발동 시 소모

        // 1. 물리적 발사
        // X축 속도 유지 (플레이어 의도 존중)
        Vector2 vel = player.RB.linearVelocity;
        vel.y = player.ActiveFormData.jump.doubleJumpForce;
        player.RB.linearVelocity = vel;
        
        // 2. 더블 점프 시 스프린트 점프의 속도 고정을 해제하여 자유로운 공중 제어를 허용합니다.
        player.IsSprintJumping = false;
        
        // 3. 상태 동기화
        isJumping = true;
        jumpCut = false; 
        
        // [Game Feel] 발사 이펙트
        // player.ActionSystem.OnDoubleJumpFire?.Invoke();
    }
}