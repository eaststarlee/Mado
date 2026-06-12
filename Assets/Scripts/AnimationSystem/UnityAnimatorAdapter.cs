using UnityEngine;

namespace Mado.AnimationSystem
{
    /// <summary>
    /// 유니티 빌트인 Animator를 ICharacterAnimator 인터페이스로 래핑하는 어댑터 클래스입니다.
    /// 에너미(Enemy) 등 기존에 Animator를 사용하던 개체들이 이 컴포넌트를 사용하게 됩니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class UnityAnimatorAdapter : MonoBehaviour, ICharacterAnimator
    {
        private Animator _animator;
        private string _currentStateName;

        public Animator Animator => _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void Play(string stateName, bool forceRestart = false)
        {
            if (_animator == null) return;

            if (!forceRestart && _currentStateName == stateName)
            {
                return; // 이미 동일한 애니메이션을 재생 중이면 무시
            }

            _currentStateName = stateName;
            
            // forceRestart가 true일 경우 애니메이션의 진행도를 0으로 초기화 (보간 시간 0)
            if (forceRestart)
            {
                _animator.Play(stateName, -1, 0f);
            }
            else
            {
                _animator.Play(stateName);
            }
        }

        public void SetSpeed(float speed)
        {
            if (_animator != null)
            {
                _animator.speed = speed;
            }
        }
        
        // 향후 추가될 수 있는 RuntimeAnimatorController 교체 등의 기능도 이곳에 모을 수 있습니다.
        public void SetRuntimeAnimatorController(RuntimeAnimatorController controller)
        {
            if (_animator != null)
            {
                _animator.runtimeAnimatorController = controller;
            }
        }
    }
}
