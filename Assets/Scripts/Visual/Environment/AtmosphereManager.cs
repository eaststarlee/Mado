using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;

namespace Mado.Visual.Environment
{
    [ExecuteAlways]
    public class AtmosphereManager : MonoBehaviour
    {
        public static AtmosphereManager Instance { get; private set; }

        [Header("Editor Preview")]
        [Tooltip("에디터 모드에서 실시간으로 테스트할 분위기 프로필을 여기에 넣으세요. (플레이 시에는 무시됨)")]
        public BiomeAtmosphereProfile previewProfile;

        [Header("Scene References")]
        [Tooltip("씬의 메인 Directional Light (태양광/달빛)")]
        public Light globalDirectionalLight;
        
        [Header("Settings")]
        [Tooltip("구역 전환 시 색상/안개가 변하는 속도 (Lerp Speed)")]
        public float transitionSpeed = 2f;
        
        [Tooltip("카메라 Culling 검사 시 Frustum의 여유 공간 비율 (0.2 = 20% 확장) - 팝인 방지")]
        public float cullingMarginRatio = 0.2f;

        // 글로벌 안개 프로퍼티
        private readonly int fogColorID = Shader.PropertyToID("_GlobalFogColor");
        private readonly int fogStartID = Shader.PropertyToID("_GlobalFogStart");
        private readonly int fogEndID = Shader.PropertyToID("_GlobalFogEnd");
        private readonly int fogPowerID = Shader.PropertyToID("_GlobalFogPower"); // 추가된 파워

        // 현재 렌더링 값 (Lerp 용도)
        private Color currentDirColor;
        private float currentDirIntensity;
        private Color currentAmbientColor;
        private Color currentFogColor;
        private float currentFogStart;
        private float currentFogEnd;
        private float currentFogPower = 1f;

        // 등록된 존 관리
        private static List<BiomeZoneVolume> allZones = new List<BiomeZoneVolume>();
        private List<BiomeZoneVolume> overlappedZones = new List<BiomeZoneVolume>();

        // 최적화를 위한 참조 캐싱
        private Camera mainCamera;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            mainCamera = Camera.main;
            
            if (globalDirectionalLight != null)
            {
                currentDirColor = globalDirectionalLight.color;
                currentDirIntensity = globalDirectionalLight.intensity;
            }
            currentAmbientColor = RenderSettings.ambientLight;
            currentFogColor = Shader.GetGlobalColor(fogColorID);
            currentFogStart = Shader.GetGlobalFloat(fogStartID);
            currentFogEnd = Shader.GetGlobalFloat(fogEndID);
            currentFogPower = Shader.GetGlobalFloat(fogPowerID);

            // [초기화 버그 픽스] 게임 시작 시 플레이어가 이미 Zone 안에 있는지 강제 검사
            CheckInitialOverlap();
        }

#if UNITY_EDITOR
        public void ForceUpdateFromEditor()
        {
            if (Application.isPlaying) return;
            if (previewProfile != null)
            {
                Shader.SetGlobalColor(fogColorID, previewProfile.fogColor);
                Shader.SetGlobalFloat(fogStartID, previewProfile.fogStartDistance);
                Shader.SetGlobalFloat(fogEndID, previewProfile.fogEndDistance);
                Shader.SetGlobalFloat(fogPowerID, previewProfile.fogPower);
                
                if (globalDirectionalLight != null)
                {
                    globalDirectionalLight.color = previewProfile.directionalLightColor;
                    globalDirectionalLight.intensity = previewProfile.directionalLightIntensity;
                }
                RenderSettings.ambientLight = previewProfile.ambientColor;
            }
        }
#endif

        private void CheckInitialOverlap()
        {
            if (mainCamera == null) return;
            // 카메라 위치(일반적으로 플레이어와 일치)를 기준으로 겹친 존을 수동으로 찾습니다.
            Vector2 checkPos = mainCamera.transform.position; 
            
            // Physics2D로 특정 포인트에 겹친 모든 콜라이더를 찾을 수도 있지만,
            // allZones 리스트가 있으니 간단히 Bounds로 체크할 수 있습니다.
            foreach (var zone in allZones)
            {
                if (zone != null && zone.VolumeCollider != null)
                {
                    if (zone.VolumeCollider.OverlapPoint(checkPos))
                    {
                        AddOverlappedZone(zone);
                    }
                }
            }
        }

        public static void RegisterZone(BiomeZoneVolume zone)
        {
            if (!allZones.Contains(zone)) allZones.Add(zone);
        }

        public static void UnregisterZone(BiomeZoneVolume zone)
        {
            allZones.Remove(zone);
        }

        public void AddOverlappedZone(BiomeZoneVolume zone)
        {
            if (!overlappedZones.Contains(zone)) overlappedZones.Add(zone);
        }

        public void RemoveOverlappedZone(BiomeZoneVolume zone)
        {
            overlappedZones.Remove(zone);
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (Instance == null) Instance = this;
                ForceUpdateFromEditor();
                return;
            }
#endif
            UpdateFrustumCulling();
            UpdateLightingBlending();
        }

        private void UpdateFrustumCulling()
        {
            if (mainCamera == null) return;

            // 카메라의 뷰포트 Bounds 계산 (Margin 포함)
            float orthoSize = mainCamera.orthographicSize;
            float aspect = mainCamera.aspect;
            float height = orthoSize * 2f;
            float width = height * aspect;
            
            // 팝인 현상을 막기 위해 Bounds 크기를 마진만큼 확장
            float marginMultiplier = 1f + cullingMarginRatio;
            Vector3 cameraSize = new Vector3(width * marginMultiplier, height * marginMultiplier, 100f);
            Bounds cameraBounds = new Bounds(mainCamera.transform.position, cameraSize);

            // 씬에 등록된 모든 Zone에 대해 Frustum Culling 검사
            foreach (var zone in allZones)
            {
                if (zone == null || zone.VolumeCollider == null) continue;
                
                bool isVisible = cameraBounds.Intersects(zone.VolumeCollider.bounds);
                zone.SetCullingState(isVisible);
            }
        }

        private void UpdateLightingBlending()
        {
            if (overlappedZones.Count == 0) return;

            float targetDirI = 0f;
            float targetFogS = 0f;
            float targetFogE = 0f;
            float targetFogP = 0f;
            
            float rDir = 0f, gDir = 0f, bDir = 0f, aDir = 0f;
            float rAmb = 0f, gAmb = 0f, bAmb = 0f, aAmb = 0f;
            float rFog = 0f, gFog = 0f, bFog = 0f, aFog = 0f;

            // 겹쳐있는 활성 존(들)의 프로필 값을 평균냅니다 (Equal Weight Blending)
            int count = 0;
            foreach (var zone in overlappedZones)
            {
                var profile = zone.biomeProfile;
                if (profile == null) continue;
                
                rDir += profile.directionalLightColor.r; gDir += profile.directionalLightColor.g; bDir += profile.directionalLightColor.b; aDir += profile.directionalLightColor.a;
                rAmb += profile.ambientColor.r; gAmb += profile.ambientColor.g; bAmb += profile.ambientColor.b; aAmb += profile.ambientColor.a;
                rFog += profile.fogColor.r; gFog += profile.fogColor.g; bFog += profile.fogColor.b; aFog += profile.fogColor.a;
                
                targetDirI += profile.directionalLightIntensity;
                targetFogS += profile.fogStartDistance;
                targetFogE += profile.fogEndDistance;
                targetFogP += profile.fogPower;
                count++;
            }

            if (count > 0)
            {
                float invCount = 1f / count;
                Color targetDirC = new Color(rDir * invCount, gDir * invCount, bDir * invCount, aDir * invCount);
                Color targetAmbC = new Color(rAmb * invCount, gAmb * invCount, bAmb * invCount, aAmb * invCount);
                Color targetFogC = new Color(rFog * invCount, gFog * invCount, bFog * invCount, aFog * invCount);
                
                targetDirI *= invCount;
                targetFogS *= invCount;
                targetFogE *= invCount;
                targetFogP *= invCount;

                // 시간에 따른 부드러운 전환(Lerp)
                float dt = Time.deltaTime * transitionSpeed;

                currentDirColor = Color.Lerp(currentDirColor, targetDirC, dt);
                currentDirIntensity = Mathf.Lerp(currentDirIntensity, targetDirI, dt);
                currentAmbientColor = Color.Lerp(currentAmbientColor, targetAmbC, dt);
                currentFogColor = Color.Lerp(currentFogColor, targetFogC, dt);
                currentFogStart = Mathf.Lerp(currentFogStart, targetFogS, dt);
                currentFogEnd = Mathf.Lerp(currentFogEnd, targetFogE, dt);
                currentFogPower = Mathf.Lerp(currentFogPower, targetFogP, dt);

                // 실제 렌더링에 적용
                if (globalDirectionalLight != null)
                {
                    globalDirectionalLight.color = currentDirColor;
                    globalDirectionalLight.intensity = currentDirIntensity;
                }
                RenderSettings.ambientLight = currentAmbientColor;

                Shader.SetGlobalColor(fogColorID, currentFogColor);
                Shader.SetGlobalFloat(fogStartID, currentFogStart);
                Shader.SetGlobalFloat(fogEndID, currentFogEnd);
                Shader.SetGlobalFloat(fogPowerID, currentFogPower);
            }
        }
    }
}
