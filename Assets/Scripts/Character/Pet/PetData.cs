using UnityEngine;

[CreateAssetMenu(fileName = "NewPetData", menuName = "Pet/Pet Data")]
public class PetData : ScriptableObject
{
    [Header("Elastic Follow Settings (탄성 추격 설정)")]
    [Tooltip("Safe Zone (R1): 펫이 자연스럽게 유영하는 내측 반지름")]
    public float safeZoneRadius = 3f;
    
    [Tooltip("Elastic Zone (R2): 가속이 최대로 도달하는 외측 반지름 (이 거리를 넘지 않으려 노력함)")]
    public float elasticZoneRadius = 7f;
    
    [Tooltip("Ghost Transition (R3): 이 거리 이상 멀어지면 유령 상태로 전환하여 즉시 추격")]
    public float ghostTransitionRadius = 15f;

    [Space(10)]
    [Tooltip("추격 시 최소 SmoothTime (낮을수록 반응 속도 빨라짐, R2 도달 시 적용)")]
    [Range(0.01f, 0.5f)]
    public float minCatchUpSmoothTime = 0.05f;

    [Tooltip("예측 이동 가중치 (플레이어 속도에 따라 목표 지점을 앞당김)")]
    [Range(0f, 0.5f)]
    public float predictiveLeadFactor = 0.2f;

    [Header("Hover Settings (배회 설정)")]
    [Tooltip("배회 시 SmoothTime (안전 영역 내에서 적용)")]
    [Range(0.1f, 2f)]
    public float hoverSmoothTime = 0.6f;
    
    [Tooltip("랜덤 오프셋 전환 속도 (높을수록 빠르게 전환, 급격한 이동 방지)")]
    [Range(1f, 10f)]
    public float randomOffsetBlendSpeed = 3f;
    
    [Tooltip("위치 변경 주기 최소값 (초)")]
    public float changePosTimeMin = 1.5f;
    
    [Tooltip("위치 변경 주기 최대값 (초)")]
    public float changePosTimeMax = 3.0f;

    [Header("Anchor Settings (앵커 설정)")]
    [Tooltip("주인공 등 뒤쪽 상공 기본 오프셋 (X: 등 뒤 거리, Y: 높이)")]
    public Vector2 anchorOffset = new Vector2(1.5f, 2.0f);
    
    [Tooltip("앵커 업데이트 속도 (높을수록 펫의 타겟 지점이 플레이어를 빠르게 따라옴)")]
    [Range(1f, 15f)]
    public float anchorUpdateSpeed = 8f;

    [Header("Floating (둥실거림)")]
    [Tooltip("둥실거림 진폭 (위아래 움직임 크기)")]
    public float floatingAmplitude = 0.05f;
    
    [Tooltip("둥실거림 주파수 (빠르기)")]
    public float floatingFrequency = 2f;
    
    [Header("State Transition (상태 전환)")]
    [Tooltip("Ghost → Follow 복귀 거리 (R1 내부로 들어와야 함)")]
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
    [Tooltip("Follow 상태 최대 이동 속도 (추격 시 보정됨)")]
    public float followMaxSpeed = 20f;
    
    [Tooltip("속도 폭주 하드 세이프티 임계값 (followMaxSpeed보다 높게 설정)")]
    public float runawayVelocityThreshold = 30f;
    
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

    [Header("Ghost Speed Settings")]
    [Tooltip("멀리서 소환될 때 속도 (빠름)")]
    public float ghostFastSpeed = 20f;
    
    [Tooltip("벽 끼임 탈출 시 속도 (느림/안정적)")]
    public float ghostStuckSpeed = 5f;

    [Tooltip("고속 합류 중 둥실거림 주파수 배수 (높을수록 빠르게 파르르 떨림)")]
    public float ghostFloatingFreqMultiplier = 3f;

    private void OnValidate()
    {
        // runawayVelocityThreshold는 followMaxSpeed보다 높아야 함
        if (runawayVelocityThreshold < followMaxSpeed)
        {
            runawayVelocityThreshold = followMaxSpeed * 1.5f;
        }

        // 영역 반지름 유효성 검사
        if (elasticZoneRadius <= safeZoneRadius)
            elasticZoneRadius = safeZoneRadius + 1f;
        if (ghostTransitionRadius <= elasticZoneRadius)
            ghostTransitionRadius = elasticZoneRadius + 5f;
    }
}
