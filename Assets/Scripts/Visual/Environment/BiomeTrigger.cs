using UnityEngine;

namespace Mado.Visual.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class BiomeTrigger : MonoBehaviour
    {
        [Tooltip("이 구역(콜라이더)에 진입할 때 적용할 분위기 데이터")]
        public BiomeAtmosphereProfile biomeProfile;

        [Tooltip("체크 시, 이 구역을 빠져나가면 씬의 원래 기본(Default) 분위기로 돌아갑니다.")]
        public bool revertOnExit = true;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 플레이어가 구역에 진입했을 때 매니저에게 프로필 교체를 요청
            if (other.CompareTag("Player"))
            {
                AtmosphereManager.Instance?.TransitionToBiome(biomeProfile);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // 플레이어가 구역을 빠져나갔을 때 원래 씬의 기본 프로필로 복구
            if (revertOnExit && other.CompareTag("Player"))
            {
                AtmosphereManager.Instance?.RevertToDefaultProfile();
            }
        }
    }
}
