using UnityEngine;

[CreateAssetMenu(fileName = "NewPogoAttack", menuName = "Player/Pogo Attack Data")]
public class PogoAttackData : AttackData
{
    [Header("Pogo Settings")]
    [Tooltip("Pogo 적중 시 Y축 강제 설정 속도")]
    public float pogoBounceVelocity = 14f;
}
