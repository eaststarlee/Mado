using UnityEngine;

/// <summary>
/// 라이징 공격 (위 화살표 + 공격) 데이터
/// 더블 점프 매커니즘과 유사하게 체공(Anticipation) 후 발사(Fire)하는 형태입니다.
/// </summary>
[CreateAssetMenu(fileName = "NewRisingAttack", menuName = "Player/Rising Attack Data")]
public class RisingAttackData : AttackData
{
    [Header("Rising Attack Dynamics")]
    [Tooltip("상승 전 공중 제어(선딜레이) 시간 (더블점프와 유사한 역경직)")]
    public float risingAnticipationDelay = 0.1f;
    
    [Tooltip("상승할 때 가해지는 Y축 힘 (Force)")]
    public float risingForce = 15f;
}
