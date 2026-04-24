using UnityEngine;

public static class PlayerAnimID
{
    // Animator State Names (대소문자 정확히 일치해야 함)
    public static readonly int Idle = Animator.StringToHash("Idle");
    
    // 공격 애니메이션
    public static readonly int Attack = Animator.StringToHash("Attack");
    public static readonly int AttackUp = Animator.StringToHash("AttackUp");
    public static readonly int AttackDown = Animator.StringToHash("AttackDown");
    
    // Future additions:
    // public static readonly int Move = Animator.StringToHash("Move");
    // public static readonly int Jump = Animator.StringToHash("Jump");
    // ...
}

