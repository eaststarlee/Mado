using UnityEngine;

namespace Mado.Character.Animation
{
    public enum AnimPriority
    {
        None = 0,
        Locomotion = 1, // Idle, Move, Fall
        Action = 2,     // Jump, Dash, Turn, Sub-Phasing
        Attack = 3,     // Attacks
        Hit = 4,        // Hit, Parry, Transform
        Death = 5       // Death
    }

    public enum PlayerAnimType
    {
        None = 0,
        
        // Locomotion
        Idle,
        Move,
        Jump,
        InAir, // Fall
        Dash,
        Sprint,
        SprintStop,
        SprintTurn,
        
        // Sub-Phasing (Advanced Movement)
        Turn,
        JumpReady,
        JumpRise,
        JumpApex,
        JumpFall,
        
        // Wall & Ledge
        WallSlide,
        WallClimb,
        LedgeClimb,
        
        // Special Movement
        Glide,
        GrappleAim,
        Grappling,
        
        // Combat & Status
        Hit,
        Death,
        Parry,
        Transform,
        
        // Attacks
        AttackNormal,
        AttackUp,
        AttackDown
    }

    [CreateAssetMenu(fileName = "PlayerAnimationData", menuName = "Anime/Player Data")]
    public class PlayerAnimationData : ScriptableObject
    {
        [Header("Locomotion")]
        public SpriteAnimationClip idle;
        public SpriteAnimationClip move;
        public SpriteAnimationClip jump;
        public SpriteAnimationClip inAir;
        public SpriteAnimationClip dash;
        public SpriteAnimationClip sprint;
        public SpriteAnimationClip sprintStop;
        public SpriteAnimationClip sprintTurn;

        [Header("Sub-Phasing")]
        public SpriteAnimationClip turn;
        public SpriteAnimationClip jumpReady;
        public SpriteAnimationClip jumpRise;
        public SpriteAnimationClip jumpApex;
        public SpriteAnimationClip jumpFall;

        [Header("Wall & Ledge")]
        public SpriteAnimationClip wallSlide;
        public SpriteAnimationClip wallClimb;
        public SpriteAnimationClip ledgeClimb;

        [Header("Special Movement")]
        public SpriteAnimationClip glide;
        public SpriteAnimationClip grappleAim;
        public SpriteAnimationClip grappling;

        [Header("Combat & Status")]
        public SpriteAnimationClip hit;
        public SpriteAnimationClip death;
        public SpriteAnimationClip parry;
        public SpriteAnimationClip transformForm;

        [Header("Attacks")]
        public SpriteAnimationClip attackNormal;
        public SpriteAnimationClip attackUp;
        public SpriteAnimationClip attackDown;

        /// <summary>
        /// Enum 타입에 맞는 애니메이션 클립을 반환합니다.
        /// </summary>
        public SpriteAnimationClip GetClip(PlayerAnimType type)
        {
            return type switch
            {
                PlayerAnimType.Idle => idle,
                PlayerAnimType.Move => move,
                PlayerAnimType.Jump => jump,
                PlayerAnimType.InAir => inAir,
                PlayerAnimType.Dash => dash,
                PlayerAnimType.Sprint => sprint,
                PlayerAnimType.SprintStop => sprintStop,
                PlayerAnimType.SprintTurn => sprintTurn,
                PlayerAnimType.Turn => turn,
                PlayerAnimType.JumpReady => jumpReady,
                PlayerAnimType.JumpRise => jumpRise,
                PlayerAnimType.JumpApex => jumpApex,
                PlayerAnimType.JumpFall => jumpFall,
                PlayerAnimType.WallSlide => wallSlide,
                PlayerAnimType.WallClimb => wallClimb,
                PlayerAnimType.LedgeClimb => ledgeClimb,
                PlayerAnimType.Glide => glide,
                PlayerAnimType.GrappleAim => grappleAim,
                PlayerAnimType.Grappling => grappling,
                PlayerAnimType.Hit => hit,
                PlayerAnimType.Death => death,
                PlayerAnimType.Parry => parry,
                PlayerAnimType.Transform => transformForm,
                PlayerAnimType.AttackNormal => attackNormal,
                PlayerAnimType.AttackUp => attackUp,
                PlayerAnimType.AttackDown => attackDown,
                _ => null
            };
        }

        /// <summary>
        /// 해당 애니메이션의 우선순위(Priority)를 반환합니다.
        /// </summary>
        public static AnimPriority GetPriority(PlayerAnimType type)
        {
            switch (type)
            {
                case PlayerAnimType.Idle:
                case PlayerAnimType.Move:
                case PlayerAnimType.InAir:
                case PlayerAnimType.JumpFall:
                case PlayerAnimType.WallSlide:
                case PlayerAnimType.Glide:
                    return AnimPriority.Locomotion;

                case PlayerAnimType.Jump:
                case PlayerAnimType.Dash:
                case PlayerAnimType.Sprint:
                case PlayerAnimType.SprintStop:
                case PlayerAnimType.SprintTurn:
                case PlayerAnimType.Turn:
                case PlayerAnimType.JumpReady:
                case PlayerAnimType.JumpRise:
                case PlayerAnimType.JumpApex:
                case PlayerAnimType.WallClimb:
                case PlayerAnimType.LedgeClimb:
                case PlayerAnimType.GrappleAim:
                case PlayerAnimType.Grappling:
                    return AnimPriority.Action;

                case PlayerAnimType.AttackNormal:
                case PlayerAnimType.AttackUp:
                case PlayerAnimType.AttackDown:
                    return AnimPriority.Attack;

                case PlayerAnimType.Hit:
                case PlayerAnimType.Parry:
                case PlayerAnimType.Transform:
                    return AnimPriority.Hit;

                case PlayerAnimType.Death:
                    return AnimPriority.Death;

                default:
                    return AnimPriority.None;
            }
        }
    }
}
