using UnityEngine;

namespace Mado.AnimationSystem
{
    /// <summary>
    /// 캐릭터의 애니메이션 제어를 위한 추상화 인터페이스입니다.
    /// 유니티 기본 Animator, 커스텀 Sprite Animator 등 모든 애니메이터가 이 인터페이스를 구현합니다.
    /// </summary>
    public interface ICharacterAnimator
    {
        /// <summary>
        /// 지정된 상태(이름)의 애니메이션을 재생합니다.
        /// </summary>
        /// <param name="stateName">재생할 상태 이름 (예: "Idle", "Walk", "Attack")</param>
        /// <param name="forceRestart">이미 재생 중이더라도 강제로 다시 시작할지 여부</param>
        void Play(string stateName, bool forceRestart = false);

        /// <summary>
        /// 애니메이션의 재생 속도를 설정합니다.
        /// </summary>
        /// <param name="speed">재생 속도 (기본값: 1f)</param>
        void SetSpeed(float speed);
    }
}
