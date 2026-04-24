using UnityEngine;

[CreateAssetMenu(fileName = "NewPetData", menuName = "Pet/Pet Data")]
public class PetData : ScriptableObject
{
    [Header("Anchor Settings (앵커 설정)")]
    [Tooltip("주인공 등 뒤쪽 상공 오프셋 (X: 등 뒤 거리, Y: 높이)")]
    public Vector2 anchorOffset = new Vector2(1.5f, 2.0f);
    
    [Tooltip("앵커 업데이트 속도 (높을수록 빠르게 따라감)")]
    [Range(1f, 10f)]
    public float anchorUpdateSpeed = 5f;
    
    [Header("Hover Settings (배회 설정)")]
    [Tooltip("배회 반경 (앵커 중심 원형 범위)")]
    public float hoverRadius = 1.5f;
    
    [Tooltip("배회 시 SmoothTime (느리게)")]
    [Range(0.1f, 2f)]
    public float hoverSmoothTime = 0.7f;
    
    [Tooltip("랜덤 오프셋 전환 속도 (높을수록 빠르게 전환, 급격한 이동 방지)")]
    [Range(1f, 10f)]
    public float randomOffsetBlendSpeed = 3f;
    
    [Tooltip("위치 변경 주기 최소값 (초)")]
    public float changePosTimeMin = 1.5f;
    
    [Tooltip("위치 변경 주기 최대값 (초)")]
    public float changePosTimeMax = 3.0f;
    
    [Header("Chase Settings (추적 설정)")]
    [Tooltip("추적 시 SmoothTime (빠르게)")]
    [Range(0.01f, 1f)]
    public float chaseSmoothTime = 0.15f;
    
    [Header("Blending Settings (블렌딩 설정)")]
    [Tooltip("배회 시작 거리 (이 거리 미만에서 배회)")]
    public float hoverDistanceMin = 0f;
    
    [Tooltip("추적 시작 거리 (이 거리 이상에서 추적)")]
    public float hoverDistanceMax = 6f;
    
    [Header("Floating (둥실거림)")]
    [Tooltip("둥실거림 진폭 (위아래 움직임 크기)")]
    public float floatingAmplitude = 0.05f;
    
    [Tooltip("둥실거림 주파수 (빠르기)")]
    public float floatingFrequency = 2f;
    
    [Header("Teleport")]
    [Tooltip("이 거리 이상 멀어지면 즉시 텔레포트")]
    public float teleportDistance = 30f;
    
    [Tooltip("텔레포트 위치 오프셋 (플레이어 기준)")]
    public Vector2 teleportOffset = new Vector2(-1.5f, 2f);
    
    [Tooltip("텔레포트 쿨다운 (초)")]
    public float teleportCooldown = 0.5f;
    
    [Header("State Transition (상태 전환)")]
    [Tooltip("Follow → Ghost 전환 거리")]
    public float followToGhostDistance = 10f;
    
    [Tooltip("Ghost → Follow 복귀 거리")]
    public float ghostToFollowDistance = 1.5f;
    
    
    [Header("Stuck Detection (정체 감지)")]
    [Tooltip("정체 판정 시간 (초)")]
    public float stuckTimeThreshold = 0.5f;
    
    [Header("Ghost State (유령 상태)")]
    [Tooltip("Ghost 상태 이동 속도")]
    public float ghostSpeed = 15f;
    
    [Tooltip("Ghost 상태 투명도 (0~1)")]
    [Range(0f, 1f)]
    public float ghostAlpha = 0.5f;
    
    [Header("Follow Movement (이동 제어)")]
    [Tooltip("Follow 상태 최대 이동 속도 (SmoothDamp 1차 제한)")]
    public float followMaxSpeed = 10f;
    
    [Tooltip("속도 폭주 하드 세이프티 임계값 (followMaxSpeed보다 높게 설정, 2차 안전장치)")]
    public float runawayVelocityThreshold = 15f;
    
    [Tooltip("목표 지점 도착 판정 거리 (이하면 정지, 떨림 방지)")]
    public float arrivalThreshold = 0.05f;
    

    
    [Header("Advanced Stuck Detection")]
    [Tooltip("벽 감지 레이어 (Wall, Ground 등) - Stuck 판정 Raycast용")]
    public LayerMask wallLayer = -1; // Everything

    [Tooltip("이 거리보다 먼 벽은 시야를 가려도 '끼임'으로 치지 않음")]
    public float maxBlockDistance = 3.0f;
    
    [Tooltip("플레이어가 이 속도 이상으로 움직이면 끼임 판정 유보")]
    public float playerStabilityThreshold = 0.5f;
    
    [Tooltip("초근접 상태 거리 (이 거리 안에서는 타이머 30%만 적용)")]
    public float closeRangeThreshold = 1.0f;
    
    [Tooltip("초근접 시 타이머 가중치")]
    [Range(0.1f, 1.0f)] 
    public float closeRangeTimerWeight = 0.3f;
    
    [Tooltip("Ghost 모드 종료 후 재진입 방지 쿨다운")]
    public float ghostExitCooldown = 0.5f;

    [Header("Rush Attack Settings")]
    [Tooltip("러쉬(돌진) 속도")]
    public float rushSpeed = 30f;
    [Tooltip("러쉬 총 체인 공격 횟수")]
    public int rushMaxCharge = 5;
    [Tooltip("러쉬 타격당 기초 데미지")]
    public int rushDamage = 10;
    [Tooltip("러쉬 타겟 탐색 반경 (원형)")]
    public float rushDetectRadius = 12f;
    [Tooltip("러쉬 공격 전역 쿨타임")]
    public float rushGlobalCooldown = 2.0f;
    [Tooltip("러쉬 중 단일 타격 역경직 시간(정지 후 재돌진 대기 시간)")]
    public float rushHitStopDuration = 0.15f;
    [Tooltip("러쉬 타격 시 튕겨져 나오는(Recoil) 거리/강도")]
    public float rushRecoilForce = 5f;
    [Tooltip("러쉬 공격 시 타겟 감지용 레이어마스크")]
    public LayerMask targetLayer;

    [Header("Ghost Speed Settings")]
    [Tooltip("멀리서 소환될 때 속도 (빠름)")]
    public float ghostFastSpeed = 15f;
    
    [Tooltip("벽 끼임 탈출 시 속도 (느림/안정적)")]
    public float ghostStuckSpeed = 5f;
    private void OnValidate()
    {
        // runawayVelocityThreshold는 followMaxSpeed보다 높아야 함 (하드 세이프티)
        if (runawayVelocityThreshold < followMaxSpeed)
        {
            Debug.LogWarning($"[PetData] runawayVelocityThreshold({runawayVelocityThreshold})는 " +
                            $"followMaxSpeed({followMaxSpeed})보다 높아야 합니다. 자동 조정됨.");
            runawayVelocityThreshold = followMaxSpeed * 1.5f;
        }
    }
}
