using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Mado.Visual.Environment
{
    public enum WaveformType
    {
        Sine,           // 규칙적인 엇박자 숨쉬기 (주인공, 펫 전용)
        Perlin,         // 부드럽고 불규칙한 일렁임 (햇살, 달빛 등 환경광 전용)
        TorchFlicker    // 가끔씩 값이 확 튀는(Spike) 거친 깜빡임 (횃불, 번개 전용)
    }

    public class LightAnimator2D : MonoBehaviour
    {
        [Header("Targets (적용 대상)")]
        [Tooltip("URP 2D 조명을 일렁이게 할 경우 할당 (없으면 자동 탐색)")]
        public Light2D targetLight;
        [Tooltip("일반 텍스처(발광 버섯 등)의 투명도를 일렁이게 할 경우 할당")]
        public SpriteRenderer targetSprite;

        [Header("Animation Type")]
        [Tooltip("조명이 일렁이는 방식을 선택합니다.")]
        public WaveformType waveType = WaveformType.Sine;

        [Header("Timing Settings")]
        [Tooltip("파형이 변하는 속도 (주파수)")]
        public float frequency = 2f;
        
        [Header("Intensity (밝기)")]
        [Tooltip("기본 밝기 강도")]
        public float baseIntensity = 1f;
        [Tooltip("위아래로 일렁일 밝기 변화량(진폭)")]
        public float amplitude = 0.3f;

        [Header("Radius (반경 - 성능 주의)")]
        [Tooltip("반경(크기)도 같이 애니메이션 할 것인가요? (성능을 위해 기본 Off)")]
        public bool animateRadius = false;
        [Tooltip("기본 빛 반경 크기")]
        public float baseRadius = 3f;
        [Tooltip("위아래로 일렁일 반경 변화량")]
        public float radiusAmplitude = 0.5f;

        // 고유 시드 오프셋 (여러 조명이 동시에 똑같은 박자로 깜빡이는 것 방지)
        private float uniqueOffset;
        
        // TorchFlicker용 누적 타이머
        private float noiseTimer = 0f;

        private void Awake()
        {
            if (targetLight == null) targetLight = GetComponent<Light2D>();
            if (targetSprite == null) targetSprite = GetComponent<SpriteRenderer>();

            // 맵에 횃불 100개를 복사해도 전부 다른 타이밍에 일렁이도록 고유 시드 부여
            uniqueOffset = Random.Range(0f, 1000f);
        }

        private void Start()
        {
            if (targetLight != null)
            {
                targetLight.intensity = baseIntensity;
                if (!animateRadius) targetLight.pointLightOuterRadius = baseRadius;
            }
            if (targetSprite != null)
            {
                Color c = targetSprite.color;
                c.a = baseIntensity; // 스프라이트는 투명도로 밝기 조절
                targetSprite.color = c;
            }
        }

        private void Update()
        {
            if (targetLight == null && targetSprite == null) return;

            float animationValue = 0f;

            switch (waveType)
            {
                case WaveformType.Sine:
                    // -1 ~ 1 사이를 규칙적으로 왕복 (Time.time 사용으로 프레임 드랍 씹힘 방지)
                    animationValue = Mathf.Sin(Time.time * frequency + uniqueOffset);
                    break;

                case WaveformType.Perlin:
                    // 0 ~ 1 사이를 부드럽고 불규칙하게 왕복하는 값을 -1 ~ 1로 매핑
                    float perlin = Mathf.PerlinNoise(Time.time * frequency + uniqueOffset, 0f);
                    animationValue = (perlin * 2f) - 1f;
                    break;

                case WaveformType.TorchFlicker:
                    // Perlin 노이즈를 기본으로 하되, 아주 가끔씩 크게 튀는 스파이크 생성
                    noiseTimer += Time.deltaTime * frequency;
                    float baseNoise = Mathf.PerlinNoise(noiseTimer + uniqueOffset, 0f);
                    
                    // 가끔 발생하는 스파이크 (특정 주파수 대역에서 값이 클 때만)
                    float spike = 0f;
                    if (Mathf.PerlinNoise(noiseTimer * 0.5f + uniqueOffset, 10f) > 0.85f)
                    {
                        spike = Random.Range(0.5f, 1.2f);
                    }
                    
                    animationValue = ((baseNoise * 2f) - 1f) + spike;
                    break;
            }

            float finalIntensity = baseIntensity + (animationValue * amplitude);
            if (finalIntensity < 0f) finalIntensity = 0f;

            // 1. Light2D 적용
            if (targetLight != null)
            {
                targetLight.intensity = finalIntensity;

                if (animateRadius)
                {
                    targetLight.pointLightOuterRadius = baseRadius + (animationValue * radiusAmplitude);
                    if (targetLight.pointLightOuterRadius < 0.1f) targetLight.pointLightOuterRadius = 0.1f;
                }
            }

            // 2. SpriteRenderer 적용 (투명도로 발광 흉내)
            if (targetSprite != null)
            {
                Color c = targetSprite.color;
                c.a = Mathf.Clamp01(finalIntensity); // 알파값은 0~1 사이로 제한
                targetSprite.color = c;
            }
        }

        /// <summary>
        /// 외부에서 빛의 기본 밝기를 변경할 때 사용
        /// </summary>
        public void SetBaseIntensity(float newIntensity) { baseIntensity = newIntensity; }

        /// <summary>
        /// 외부에서 색상을 변경할 때 사용
        /// </summary>
        public void SetColor(Color newColor) 
        { 
            if (targetLight != null) targetLight.color = newColor; 
            if (targetSprite != null) targetSprite.color = new Color(newColor.r, newColor.g, newColor.b, targetSprite.color.a);
        }
    }
}
