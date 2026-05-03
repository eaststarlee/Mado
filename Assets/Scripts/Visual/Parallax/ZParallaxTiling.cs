using UnityEngine;

namespace Mado.Visual.Parallax
{
    /// <summary>
    /// Perspective 카메라 + Z축 깊이 방식의 패럴랙스 환경에서
    /// 무한 반복 배경 텍스처(하늘, 구름 등)의 UV를 스크롤하는 컴포넌트입니다.
    ///
    /// [사용 대상]
    ///   - Background_Far, Background_Mid 레이어처럼 수평으로 무한 반복이 필요한 스프라이트
    ///   - 크기가 한정된 배경이나 전경 오브젝트는 사용하지 않습니다.
    ///
    /// [원리]
    ///   Perspective 카메라가 이동할 때, 오브젝트는 Z축 원근법에 의해 이미 패럴랙스됩니다.
    ///   이 스크립트는 그 이동속도에 맞게 UV를 함께 스크롤하여 텍스처가 밀려 보이는 현상을 방지합니다.
    ///
    /// [주의]
    ///   - Sprite Renderer가 아닌 Quad Mesh + Material(Shader) 방식에서 작동합니다.
    ///   - Sprite Renderer를 사용 중이라면 MaterialPropertyBlock 대신 UV 방식이 아닌
    ///     오브젝트 오프셋 방식을 고려하세요.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [DefaultExecutionOrder(110)] // 카메라 업데이트보다 늦게, LateUpdate 이후 실행
    public class ZParallaxTiling : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        // 인스펙터 필드
        // ─────────────────────────────────────────────────────────────────────

        [Header("카메라 설정")]
        [Tooltip("Perspective 카메라의 Z 거리 (Position Z의 절대값). Main Camera Z = -30이면 30을 입력)")]
        public float cameraDistance = 30f;

        [Header("UV 스크롤 설정")]
        [Tooltip("활성화 시 오브젝트의 Z 위치에서 UV 멀티플라이어를 자동 계산합니다.\n" +
                 "비활성화 시 아래의 수동 Multiplier 값을 사용합니다.")]
        public bool autoCalcFromZ = true;

        [Tooltip("[autoCalcFromZ = false 일 때 사용]\n" +
                 "0.0 = UV 고정, 1.0 = 카메라와 동일 속도로 스크롤")]
        [Range(0f, 1f)]
        public float manualMultiplierX = 0.5f;

        [Tooltip("Y축 UV 스크롤 (보통 0으로 고정)")]
        [Range(0f, 1f)]
        public float manualMultiplierY = 0f;

        [Header("텍스처 설정")]
        [Tooltip("셰이더의 텍스처 프로퍼티 이름 (기본: _MainTex)")]
        public string texturePropertyName = "_MainTex";

        // ─────────────────────────────────────────────────────────────────────
        // 내부 변수
        // ─────────────────────────────────────────────────────────────────────

        private Renderer   _renderer;
        private MaterialPropertyBlock _propBlock;
        private Camera     _mainCamera;
        private Vector3    _camStartPosition;
        private int        _texSTPropertyID; // 셰이더 _MainTex_ST 프로퍼티 ID

        // 자동 계산된 Multiplier 캐시 (Z값 변경 시 재계산)
        private float _cachedMultiplierX;
        private float _cachedMultiplierY;
        private float _lastObjectZ;

        // ─────────────────────────────────────────────────────────────────────
        // Unity 생명주기
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            _renderer  = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            // 셰이더 텍스처 Tiling/Offset 프로퍼티: 이름 뒤에 _ST가 붙습니다.
            _texSTPropertyID = Shader.PropertyToID(texturePropertyName + "_ST");
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[ZParallaxTiling] Main Camera를 찾을 수 없습니다. Camera에 MainCamera 태그를 설정하세요.", this);
                enabled = false;
                return;
            }

            _camStartPosition = _mainCamera.transform.position;
            RecalcMultiplier();
        }

        private void LateUpdate()
        {
            if (_mainCamera == null) return;

            // Z값이 런타임에 변경되었을 경우 Multiplier 재계산 (에디터 튜닝 대응)
            if (!Mathf.Approximately(transform.position.z, _lastObjectZ))
            {
                RecalcMultiplier();
            }

            // 카메라 이동량 계산
            Vector3 camDelta = _mainCamera.transform.position - _camStartPosition;

            // UV 오프셋 계산 (멀티플라이어 적용)
            float uOffset = camDelta.x * _cachedMultiplierX;
            float vOffset = camDelta.y * _cachedMultiplierY;

            // MaterialPropertyBlock으로 UV 오프셋 적용 (SRP Batcher / GPU Instancing 호환)
            _renderer.GetPropertyBlock(_propBlock);

            // 기존 텍스처 Scale(Tiling) 보존
            Material mat = _renderer.sharedMaterial;
            Vector2 texScale = (mat != null)
                ? mat.GetTextureScale(texturePropertyName)
                : Vector2.one;

            // Vector4(ScaleX, ScaleY, OffsetX, OffsetY)
            _propBlock.SetVector(_texSTPropertyID, new Vector4(texScale.x, texScale.y, uOffset, vOffset));
            _renderer.SetPropertyBlock(_propBlock);
        }

        // ─────────────────────────────────────────────────────────────────────
        // 내부 유틸리티
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// 오브젝트의 Z 위치와 카메라 거리를 기반으로 UV Multiplier를 계산합니다.
        ///
        /// [원리]
        ///   Perspective 카메라가 1 유닛 이동할 때, Z=objectZ에 있는 물체는
        ///   화면에서 (cameraDistance / (cameraDistance + objectZ))만큼 이동합니다.
        ///   UV도 동일한 비율로 스크롤해야 텍스처 밀림이 없습니다.
        ///
        ///   예시 (cameraDistance=30):
        ///     Z = +40 (원경)  → 30/(30+40) ≈ 0.43  (느리게 스크롤)
        ///     Z = 0  (기준)   → 30/(30+ 0) = 1.00  (카메라와 동일)
        ///     Z = -8 (전경)   → 30/(30- 8) ≈ 1.36  (빠르게 스크롤)
        /// </summary>
        private void RecalcMultiplier()
        {
            _lastObjectZ = transform.position.z;

            if (autoCalcFromZ)
            {
                float denominator = cameraDistance + _lastObjectZ;

                // 분모가 0에 가까우면 카메라 바로 앞에 있는 것이므로 1.0으로 클램프
                if (Mathf.Approximately(denominator, 0f))
                {
                    _cachedMultiplierX = 1f;
                    _cachedMultiplierY = 0f;
                    return;
                }

                _cachedMultiplierX = cameraDistance / denominator;
                _cachedMultiplierY = 0f; // Y축은 기본 고정 (필요 시 manualMultiplierY 참조로 변경)
            }
            else
            {
                _cachedMultiplierX = manualMultiplierX;
                _cachedMultiplierY = manualMultiplierY;
            }
        }

        /// <summary>
        /// 씬 전환이나 순간이동 시, UV 오프셋 기준점을 리셋합니다.
        /// </summary>
        public void ResetAnchor()
        {
            if (_mainCamera != null)
            {
                _camStartPosition = _mainCamera.transform.position;
            }
        }

#if UNITY_EDITOR
        // 에디터에서 값을 변경하면 즉시 Multiplier를 재계산합니다.
        private void OnValidate()
        {
            RecalcMultiplier();
        }
#endif
    }
}
