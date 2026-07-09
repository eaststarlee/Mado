using UnityEngine;
using UnityEngine.Rendering;

namespace Mado.Visual.Environment
{
    [CreateAssetMenu(fileName = "NewBiomeAtmosphere", menuName = "Visual/Biome Atmosphere Profile", order = 1)]
    public class BiomeAtmosphereProfile : ScriptableObject
    {
        [Header("Global Lighting (2D)")]
        [Tooltip("구역 전체를 덮는 Global Light 2D의 색상 (전체적인 톤/분위기)")]
        public Color directionalLightColor = Color.white;
        [Tooltip("구역 전체를 덮는 Global Light 2D의 밝기 강도")]
        public float directionalLightIntensity = 1f;
        
        [Header("Distance Fog (대기 원근법)")]
        [Tooltip("멀어질수록 덮일 안개(배경) 색상")]
        public Color fogColor = new Color(0.1f, 0.15f, 0.2f, 1f);
        [Tooltip("안개가 시작되는 Z 거리 (카메라 기준)")]
        public float fogStartDistance = 10f;
        [Tooltip("안개가 완전히 덮이는 Z 거리")]
        public float fogEndDistance = 50f;
        [Tooltip("안개가 짙어지는 곡선 강도 (1=선형, 2=제곱(부드럽게 짙어짐))")]
        public float fogPower = 1f;

        [Header("Post-Processing (옵션)")]
        [Tooltip("이 구역에 진입했을 때 덮어씌울 로컬 포스트 프로세싱 볼륨 프로필")]
        public VolumeProfile biomeVolumeProfile;

        [Header("Ambient Particles (환경 효과)")]
        [Tooltip("이 바이옴에 진입 시 흩날릴 파티클 프리팹들 (예: 낙엽, 먼지, 눈)")]
        public GameObject[] ambientParticlePrefabs;

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 값을 수정했을 때, 씬 뷰에 즉시 반영되도록 매니저에게 알림
            if (!Application.isPlaying && AtmosphereManager.Instance != null)
            {
                AtmosphereManager.Instance.ForceUpdateFromEditor();
            }
        }
#endif
    }
}
