using UnityEngine;
using System;

namespace Mado.Character.Animation
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("스프라이트를 렌더링할 대상 컴포넌트. 비워두면 자식 객체에서 자동 검색합니다.")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Debug / State")]
        [SerializeField] private SpriteAnimationClip currentClip;
        [SerializeField] private int currentFrameIndex = 0;
        [SerializeField] private float frameTimer = 0f;
        [SerializeField] private bool isPlaying = false;
        
        [SerializeField] private AnimPriority currentPriority = AnimPriority.None;
        
        // 현재 애니메이션의 진행 속도 배율 (1.0 = 정상, 0.5 = 절반, 0 = 완전 정지)
        private float speedMultiplier = 1f;
        // 히트스탑 플래그 (true면 애니메이션 시간이 흐르지 않음)
        private bool isHitStop = false;

        /// <summary>
        /// 특정 프레임에 도달하여 이벤트가 발생했을 때 호출됩니다.
        /// (예: 발소리 출력, 공격 판정 활성화)
        /// </summary>
        public event Action<string> OnAnimationEvent;

        /// <summary>
        /// 루프가 아닌 애니메이션의 재생이 끝났을 때 호출됩니다.
        /// FSM에서 상태 전환 타이밍을 잡을 때 유용합니다.
        /// </summary>
        public event Action OnAnimationComplete;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    spriteRenderer = GetComponentInChildren<SpriteRenderer>();
                }

                if (spriteRenderer == null)
                {
                    Debug.LogError("[CharacterAnimationController] SpriteRenderer를 찾을 수 없습니다! 부모 또는 자식에 할당해주세요.", this);
                }
            }
        }

        /// <summary>
        /// 클립을 재생합니다.
        /// </summary>
        /// <param name="clip">재생할 SpriteAnimationClip 데이터</param>
        /// <param name="priority">애니메이션의 우선순위 (하위 우선순위는 상위를 덮어쓸 수 없음)</param>
        /// <param name="forceRestart">true면 현재 재생 중인 클립과 동일해도 강제로 처음부터 다시 재생합니다.</param>
        public void Play(SpriteAnimationClip clip, AnimPriority priority = AnimPriority.None, bool forceRestart = false)
        {
            if (clip == null) return;

            // [인터럽트 규칙 1] 동일한 클립이 이미 재생 중이고 강제 재시작이 아니면 무시
            if (!forceRestart && currentClip == clip && isPlaying) return;

            // [인터럽트 규칙 2] 새로 요청된 우선순위가 현재 재생 중인 애니메이션의 우선순위보다 낮으면 무시
            // (단, 현재 애니메이션이 끝났거나 루프가 아니라면 덮어쓸 수 있음)
            if (isPlaying && currentClip != null && currentPriority > priority)
            {
                // 루프 애니메이션이거나, 아직 마지막 프레임에 도달하지 않은 논-루프 애니메이션이라면 보호
                return;
            }

            currentClip = clip;
            currentPriority = priority;
            currentFrameIndex = 0;
            frameTimer = 0f;
            isPlaying = true;
            isHitStop = false;
            speedMultiplier = 1f;

            if (currentClip.frames != null && currentClip.frames.Length > 0)
            {
                ApplyFrame(0);
            }
        }

        /// <summary>
        /// 재생을 완전히 중단합니다.
        /// </summary>
        public void Stop()
        {
            isPlaying = false;
        }

        /// <summary>
        /// 애니메이션을 일시 정지(역경직)시킵니다. 
        /// </summary>
        public void PauseForHitStop()
        {
            isHitStop = true;
        }

        /// <summary>
        /// 일시 정지(역경직)를 해제하고 재생을 재개합니다.
        /// </summary>
        public void ResumeFromHitStop()
        {
            isHitStop = false;
        }

        /// <summary>
        /// 애니메이션 재생 속도를 조절합니다. (기본값: 1f)
        /// </summary>
        public void SetSpeed(float multiplier)
        {
            speedMultiplier = multiplier;
        }

        private void Update()
        {
            // 재생 중이 아니거나 클립이 없거나 히트스탑 중이면 타이머 갱신을 건너뜁니다.
            if (!isPlaying || currentClip == null || isHitStop) return;
            if (currentClip.frames == null || currentClip.frames.Length == 0) return;

            frameTimer += Time.deltaTime * speedMultiplier;

            float currentFrameDuration = currentClip.frames[currentFrameIndex].duration;

            // 남은 타이머가 프레임 지속 시간보다 큰 동안 계속 다음 프레임으로 넘깁니다. (낮은 프레임레이트 대응)
            while (frameTimer >= currentFrameDuration)
            {
                frameTimer -= currentFrameDuration;
                NextFrame();

                // 루프가 끝나 재생이 멈췄다면 루프 탈출
                if (!isPlaying) break;

                // 다음 프레임의 시간을 다시 가져옵니다.
                currentFrameDuration = currentClip.frames[currentFrameIndex].duration;
            }
        }

        private void NextFrame()
        {
            currentFrameIndex++;

            // 클립의 마지막 프레임을 넘어섰을 때
            if (currentFrameIndex >= currentClip.frames.Length)
            {
                if (currentClip.isLoop)
                {
                    currentFrameIndex = 0; // 처음으로 루프
                }
                else
                {
                    // 루프가 아니면 마지막 프레임에 머무르고 재생 종료
                    currentFrameIndex = currentClip.frames.Length - 1;
                    isPlaying = false;
                    currentPriority = AnimPriority.None;
                    
                    OnAnimationComplete?.Invoke();
                    return;
                }
            }

            ApplyFrame(currentFrameIndex);
        }

        private void ApplyFrame(int index)
        {
            AnimFrame frame = currentClip.frames[index];
            
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = frame.sprite;
            }

            // 해당 프레임에 연결된 이벤트 문자열이 있다면 브로드캐스팅
            if (!string.IsNullOrEmpty(frame.eventName))
            {
                OnAnimationEvent?.Invoke(frame.eventName);
            }
        }
    }
}
