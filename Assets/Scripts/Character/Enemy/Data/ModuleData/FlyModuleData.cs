using UnityEngine;

[CreateAssetMenu(fileName = "NewFlyModuleData", menuName = "Enemy/Module Data/Fly")]
public class FlyModuleData : ScriptableObject, IModuleData
{
    public string ModuleName => "FlyModule";

    public enum FlyPatrolType { IdleHover, CircleAnchor, RandomWander }

    [Header("Physics")]
    [Tooltip("최대 이동 속도")]
    public float maxSpeed = 5f;
    
    [Tooltip("추적 시 SmoothTime (멀 때/빠름)")]
    public float chaseSmoothTime = 0.15f;
    
    [Tooltip("배회 시 SmoothTime (가까울 때/느림)")]
    public float hoverSmoothTime = 0.7f;
    
    [Tooltip("Flip 최소 속도")]
    public float turnThreshold = 0.1f; 

    [Header("Hover (Anchor & Wander)")]
    [Tooltip("배회 반경 (앵커 중심 원형 범위)")]
    public float hoverRadius = 1.5f;
    
    [Tooltip("랜덤 오프셋 전환 속도 (높을수록 빠르게 전환)")]
    public float randomOffsetBlendSpeed = 2f;
    
    [Tooltip("위치 변경 주기 (최소/최대)")]
    public Vector2 changePosTimeRange = new Vector2(1.5f, 3.0f);

    [Header("Floating (Sine Wave)")]
    [Tooltip("둥실거림 진폭")]
    public float floatingAmplitude = 0.5f;
    [Tooltip("둥실거림 주파수")]
    public float floatingFrequency = 2.0f;

    [Header("Avoidance")]
    public float rayDistance = 1.5f;
    public LayerMask obstacleLayer; // Ground/Wall Only
    public float avoidForce = 5.0f;

    [Header("Behavior")]
    public FlyPatrolType patrolType;
    public float stuckThreshold = 2.0f; 
    
    [Header("Blending Distances")]
    [Tooltip("이 거리 이상이면 ChaseSmoothTime 적용 (추적 모드)")]
    public float hoverDistanceMax = 6f;
    [Tooltip("이 거리 이하면 HoverSmoothTime 적용 (배회 모드)")]
    public float hoverDistanceMin = 1f;
}
