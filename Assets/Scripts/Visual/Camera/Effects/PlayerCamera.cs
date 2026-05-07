using UnityEngine;
using Unity.Cinemachine;

public class PlayerCamera : MonoBehaviour
{
    [System.Serializable]
    public struct CameraStateSettings
    {
        [Tooltip("카메라의 Damping 값 (X: 수평, Y: 수직)")]
        public Vector2 Damping;
        [Tooltip("플레이어가 바라보는 방향으로 카메라가 얼마나 앞을 더 비춰줄지 결정하는 값")]
        public float LookOffset;
    }

    [Header("필수 컴포넌트")]
    [Tooltip("플레이어 컨트롤러를 여기에 연결하세요.")]
    public PlayerController playerController;

    [Header("전환 속도")]
    [Tooltip("Damping 값이 변경될 때의 전환 속도")]
    public float dampingChangeSpeed = 10f;
    [Tooltip("좌우 시선(Offset)이 변경될 때의 전환 속도")]
    public float lookOffsetChangeSpeed = 5f;
    [Tooltip("역동적인 상태(대시 등)가 끝난 후, 역동적인 Damping 설정을 유지할 시간(초)")]
    public float dynamicStateCooldown = 0.2f;

    [Header("상태별 카메라 설정")]
    public CameraStateSettings IdleSettings = new CameraStateSettings { Damping = new Vector2(1f, 0.5f), LookOffset = 3f };
    public CameraStateSettings MoveSettings = new CameraStateSettings { Damping = new Vector2(1f, 0.5f), LookOffset = 4f };
    public CameraStateSettings RisingSettings = new CameraStateSettings { Damping = new Vector2(1f, 0.5f), LookOffset = 4f };
    public CameraStateSettings FallingSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 4f };
    public CameraStateSettings DashSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 5f };
    public CameraStateSettings SprintSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 5f };
    public CameraStateSettings WallSlideSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 3f };
    public CameraStateSettings WallJumpSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 4f };
    public CameraStateSettings SprintJumpSettings = new CameraStateSettings { Damping = new Vector2(0f, 0.1f), LookOffset = 5f };
    
    [Header("수직 시선 설정 (고정 값)")]
    [Tooltip("카메라가 상하로 얼마나 치우쳐서 보여줄지 결정합니다.")]
    public float verticalLookOffset = 2f;
    [Tooltip("상하 시선 이동이 발동되기까지 얼마나 오래 입력을 유지해야 하는지 (초)")]
    public float verticalLookActivateDelay = 0.5f;

    private CinemachinePositionComposer m_PositionComposer;
    private Vector3 m_TargetTrackedOffset;
    private float m_VerticalLookTimer;
    private float m_DynamicStateCooldownTimer;
    private CameraStateSettings m_LastDynamicSettings;

    void Start()
    {
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerCamera: PlayerController를 찾을 수 없습니다. 스크립트가 비활성화됩니다.");
                enabled = false;
                return;
            }
        }

        var cinemachineCam = GetComponent<CinemachineCamera>();
        if (cinemachineCam != null)
        {
            m_PositionComposer = cinemachineCam.GetCinemachineComponent(CinemachineCore.Stage.Body) as CinemachinePositionComposer;
        }

        if (m_PositionComposer != null)
        {
            m_TargetTrackedOffset = m_PositionComposer.TargetOffset;
        }
        else
        {
            Debug.LogError("PlayerCamera: CinemachineCamera에 Position Composer가 설정되어 있어야 합니다. 스크립트가 비활성화됩니다.");
            enabled = false;
        }
    }

    void Update()
    {
        if (playerController == null || m_PositionComposer == null)
            return;
        
        m_DynamicStateCooldownTimer -= Time.deltaTime;

        // 1. 현재 플레이어 상태에 맞는 목표 카메라 설정(Damping, Look Offset)을 결정합니다.
        CameraStateSettings targetSettings = GetTargetCameraState();

        // 2. 결정된 목표 설정을 향해 카메라 속성들을 부드럽게 변경합니다.
        ApplyCameraSettings(targetSettings);
    }

    private CameraStateSettings GetTargetCameraState()
    {
        var currentState = playerController.StateMachine.CurrentState;
        
        bool isDynamic;
        CameraStateSettings settings = GetSettingsForState(currentState, out isDynamic);

        if (isDynamic)
        {
            m_DynamicStateCooldownTimer = dynamicStateCooldown;
            m_LastDynamicSettings = settings; // 마지막으로 사용된 역동적 설정 저장
        }
        
        // 쿨다운이 활성화 중이면, 플레이어가 잠시 안정 상태가 되더라도 마지막 역동 상태의 설정을 유지
        if (m_DynamicStateCooldownTimer > 0)
        {
            return m_LastDynamicSettings;
        }

        return settings;
    }

    private CameraStateSettings GetSettingsForState(PlayerState state, out bool isDynamic)
    {
        isDynamic = true; // 기본적으로 역동으로 가정하고, 안정 상태에서만 false로 변경

        if (state == playerController.IdleState) { isDynamic = false; return IdleSettings; }
        if (state == playerController.MoveState) { isDynamic = false; return MoveSettings; }
        
        if (state == playerController.DashState) return DashSettings;
        if (state == playerController.SprintState) return SprintSettings;
        if (state == playerController.SprintStopState) return SprintSettings;
        if (state == playerController.SprintTurnState) return SprintSettings;
        if (state == playerController.WallSlideState) return WallSlideSettings;
        if (state == playerController.WallJumpState || state == playerController.WallClimbState) return WallJumpSettings;
        
        if (state == playerController.InAirState)
        {
            if (playerController.IsSprintJumping) return SprintJumpSettings;
            if (playerController.RB.linearVelocity.y > 0.1f) { isDynamic = false; return RisingSettings; }
            return FallingSettings;
        }
        
        isDynamic = false;
        return IdleSettings;
    }
    
    private float m_ShakeTimer;
    private float m_ShakeIntensity;

    public void Shake(float intensity, float duration)
    {
        m_ShakeIntensity = intensity;
        m_ShakeTimer = duration;
    }

    private void ApplyCameraSettings(CameraStateSettings settings)
    {
        // --- Damping 처리 ---
        Vector3 currentDamping = m_PositionComposer.Damping;
        Vector2 targetDamping = settings.Damping;

        if (Mathf.Approximately(targetDamping.x, 0)) currentDamping.x = 0;
        else currentDamping.x = Mathf.Lerp(currentDamping.x, targetDamping.x, Time.deltaTime * dampingChangeSpeed);

        if (Mathf.Approximately(targetDamping.y, 0)) currentDamping.y = 0;
        else currentDamping.y = Mathf.Lerp(currentDamping.y, targetDamping.y, Time.deltaTime * dampingChangeSpeed);
        
        // 룸 데이터에서 Y축 고정을 요청한 경우, 수직 추적을 차단하기 위해 댐핑을 높게 유지
        if (CameraManager.Instance != null && CameraManager.Instance.CurrentRoomData != null && CameraManager.Instance.CurrentRoomData.lockCameraY)
        {
            currentDamping.y = 20f;
        }

        m_PositionComposer.Damping = currentDamping;

        // --- Offset 처리 ---
        float targetLookOffsetX = playerController.IsFacingRight ? settings.LookOffset : -settings.LookOffset;
        m_TargetTrackedOffset.x = Mathf.Lerp(m_TargetTrackedOffset.x, targetLookOffsetX, Time.deltaTime * lookOffsetChangeSpeed);
        
        HandleVerticalLook();

        // Screen Shake 적용
        Vector3 finalOffset = m_TargetTrackedOffset;
        if (m_ShakeTimer > 0)
        {
            m_ShakeTimer -= Time.deltaTime;
            Vector2 shake = Random.insideUnitCircle * m_ShakeIntensity;
            finalOffset += (Vector3)shake;
        }

        m_PositionComposer.TargetOffset = finalOffset;
    }

    private void HandleVerticalLook()
    {
        // 1. 기본 상태 체크 (반드시 Idle 상태여야 합니다)
        if (playerController.StateMachine.CurrentState != playerController.IdleState)
        {
            ResetVerticalLook();
            return;
        }

        // 2. 입력 및 행동 체크 (엄격한 조건)
        // - 수평 이동 중이면 안 됨 (InputX 체크)
        // - 공격 중이면 안 됨 (Combat 상태)
        // - 공격 키를 누르고 있으면 안 됨 (X키 홀드 대응)
        bool isMovingHorizontally = Mathf.Abs(playerController.InputX) > 0.01f;
        // Combat이 null일 수 있으므로 null 체크 필수
        bool isAttacking = (playerController.Combat != null && playerController.Combat.IsAttacking);
        // 키 설정이 하드코딩 되어 있다면 Input Manager나 KeyCode 변수를 사용하는 것이 좋지만, 현재는 X키로 가정
        bool isAttackInput = playerController.IsAttackHeld; // Use IsAttackHeld

        // 위 조건 중 하나라도 해당되면 시선 이동 취소
        if (isMovingHorizontally || isAttacking || isAttackInput)
        {
            ResetVerticalLook();
            return;
        }

        // 3. 수직 입력 체크 (Vertical Input)
        float verticalInput = playerController.InputY; // Use InputY

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            m_VerticalLookTimer += Time.deltaTime;

            // 설정된 지연 시간(Activate Delay)이 지나면 시선 이동 적용
            if (m_VerticalLookTimer >= verticalLookActivateDelay)
            {
                float targetY = verticalInput > 0 ? verticalLookOffset : -verticalLookOffset;
                m_TargetTrackedOffset.y = Mathf.Lerp(m_TargetTrackedOffset.y, targetY, Time.deltaTime * lookOffsetChangeSpeed);
            }
        }
        else
        {
            ResetVerticalLook();
        }
    }

    // 시선 이동 초기화 헬퍼 메서드
    private void ResetVerticalLook()
    {
        m_VerticalLookTimer = 0;
        // 부드럽게 원위치로 복귀
        m_TargetTrackedOffset.y = Mathf.Lerp(m_TargetTrackedOffset.y, 0, Time.deltaTime * lookOffsetChangeSpeed);
    }
}