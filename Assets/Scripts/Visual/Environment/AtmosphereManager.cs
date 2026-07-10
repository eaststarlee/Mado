using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; // URP 2D 조명 시스템 접근용
using System.Collections;
using System.Collections.Generic;

namespace Mado.Visual.Environment
{
    [ExecuteAlways]
    public class AtmosphereManager : MonoBehaviour
    {
        public static AtmosphereManager Instance { get; private set; }

        [Header("Scene References")]
        [Tooltip("씬의 메인 2D 글로벌 조명 (태양광/달빛 - URP Global Light 2D)")]
        public Light2D globalLight2D;
        
        [Tooltip("씬 전체를 덮는 Global Post-Processing Volume (구역 진입 시 이 프로필을 교체)")]
        public Volume globalPostProcessVolume;
        
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
        private Color currentFogColor;
        private float currentFogStart;
        private float currentFogEnd;
        private float currentFogPower = 1f;

        // 등록된 존 관리
        private static List<BiomeZoneVolume> allZones = new List<BiomeZoneVolume>();
        private List<BiomeZoneVolume> overlappedZones = new List<BiomeZoneVolume>();

        // 3번 피드백: 여러 파티클이 동시에 Prewarm 되어 프레임이 튀는 것을 막기 위한 스태거링(Staggering) 큐
        private Queue<ParticleSystem> prewarmQueue = new Queue<ParticleSystem>();

        public void EnqueuePrewarm(ParticleSystem ps)
        {
            // 중복 큐잉 방지
            foreach (var item in prewarmQueue)
            {
                if (item == ps) return;
            }
            prewarmQueue.Enqueue(ps);
        }

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
            
            if (globalLight2D != null)
            {
                currentDirColor = globalLight2D.color;
                currentDirIntensity = globalLight2D.intensity;
            }
            currentFogColor = Shader.GetGlobalColor(fogColorID);
            currentFogStart = Shader.GetGlobalFloat(fogStartID);
            currentFogEnd = Shader.GetGlobalFloat(fogEndID);
            currentFogPower = Shader.GetGlobalFloat(fogPowerID);

            // [초기화 버그 픽스] 게임 시작 시 플레이어가 이미 Zone 안에 있는지 강제 검사
            CheckInitialOverlap();
            
            // 씬 진입/리스폰 시 어색한 Lerp 없이 즉시 환경을 덮어씌움 (Snap)
            SnapToCurrentZone();
        }

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

        /// <summary>
        /// 텔레포트, 리스폰, 씬 최초 진입 시 목표 조명/안개/포스트프로세싱으로 즉시 전환합니다. (Lerp 생략)
        /// </summary>
        public void SnapToCurrentZone()
        {
            if (overlappedZones.Count == 0) return;
            
            float targetDirI = 0f, targetFogS = 0f, targetFogE = 0f, targetFogP = 0f;
            float rDir = 0f, gDir = 0f, bDir = 0f, aDir = 0f;
            float rFog = 0f, gFog = 0f, bFog = 0f, aFog = 0f;
            
            VolumeProfile latestVolumeProfile = null;
            int count = 0;

            foreach (var zone in overlappedZones)
            {
                var profile = zone.biomeProfile;
                if (profile == null) continue;
                
                rDir += profile.directionalLightColor.r; gDir += profile.directionalLightColor.g; bDir += profile.directionalLightColor.b; aDir += profile.directionalLightColor.a;
                rFog += profile.fogColor.r; gFog += profile.fogColor.g; bFog += profile.fogColor.b; aFog += profile.fogColor.a;
                
                targetDirI += profile.directionalLightIntensity;
                targetFogS += profile.fogStartDistance;
                targetFogE += profile.fogEndDistance;
                targetFogP += profile.fogPower;
                
                // 포스트 프로세싱 프로필은 섞을 수 없으므로, 가장 늦게 진입한(배열 마지막) Zone의 프로필을 우선시합니다.
                if (profile.biomeVolumeProfile != null) latestVolumeProfile = profile.biomeVolumeProfile;
                count++;
            }

            if (count > 0)
            {
                float invCount = 1f / count;
                currentDirColor = new Color(rDir * invCount, gDir * invCount, bDir * invCount, aDir * invCount);
                currentFogColor = new Color(rFog * invCount, gFog * invCount, bFog * invCount, aFog * invCount);
                
                currentDirIntensity = targetDirI * invCount;
                currentFogStart = targetFogS * invCount;
                currentFogEnd = targetFogE * invCount;
                currentFogPower = targetFogP * invCount;
                
                ApplyCurrentRenderValues();
                
                if (globalPostProcessVolume != null && latestVolumeProfile != null)
                {
                    globalPostProcessVolume.profile = latestVolumeProfile;
                }
            }
        }

        private void ApplyCurrentRenderValues()
        {
            if (globalLight2D != null)
            {
                globalLight2D.color = currentDirColor;
                globalLight2D.intensity = currentDirIntensity;
            }

            Shader.SetGlobalColor(fogColorID, currentFogColor);
            Shader.SetGlobalFloat(fogStartID, currentFogStart);
            Shader.SetGlobalFloat(fogEndID, currentFogEnd);
            Shader.SetGlobalFloat(fogPowerID, currentFogPower);
        }

        private void Update()
        {
            UpdateFrustumCulling();
            UpdateLightingBlending();
            ProcessPrewarmQueue();
        }

        private void ProcessPrewarmQueue()
        {
            // 3번 피드백: 한 프레임에 최대 1개의 파티클 시스템만 Native Prewarm 처리하여 프레임 스파이크 방지
            if (prewarmQueue.Count > 0)
            {
                ParticleSystem ps = prewarmQueue.Dequeue();
                if (ps != null)
                {
                    // 4번 피드백 준수: 반드시 명시적 순서를 지킵니다. (Shape는 생성 시점에 이미 맞췄음)
                    // 1. 초기화 및 리셋
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    // 2. Prewarm 플래그 켜기
                    var main = ps.main;
                    main.prewarm = true;
                    // 3. 실행 (이 순간 C++ 백엔드에서 1주기치 사전 연산을 동기적으로 수행함)
                    ps.Play(true);
                }
            }
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
            float rFog = 0f, gFog = 0f, bFog = 0f, aFog = 0f;

            // 겹쳐있는 활성 존(들)의 프로필 값을 평균냅니다 (Equal Weight Blending)
            int count = 0;
            foreach (var zone in overlappedZones)
            {
                var profile = zone.biomeProfile;
                if (profile == null) continue;
                
                rDir += profile.directionalLightColor.r; gDir += profile.directionalLightColor.g; bDir += profile.directionalLightColor.b; aDir += profile.directionalLightColor.a;
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
                Color targetFogC = new Color(rFog * invCount, gFog * invCount, bFog * invCount, aFog * invCount);
                
                targetDirI *= invCount;
                targetFogS *= invCount;
                targetFogE *= invCount;
                targetFogP *= invCount;

                // 시간에 따른 부드러운 전환(Lerp)
                float dt = Time.deltaTime * transitionSpeed;

                currentDirColor = Color.Lerp(currentDirColor, targetDirC, dt);
                currentDirIntensity = Mathf.Lerp(currentDirIntensity, targetDirI, dt);
                currentFogColor = Color.Lerp(currentFogColor, targetFogC, dt);
                currentFogStart = Mathf.Lerp(currentFogStart, targetFogS, dt);
                currentFogEnd = Mathf.Lerp(currentFogEnd, targetFogE, dt);
                currentFogPower = Mathf.Lerp(currentFogPower, targetFogP, dt);

                ApplyCurrentRenderValues();
                
                // 포스트 프로세싱은 점진적 섞기가 불가능하므로 볼륨 자체를 덮어씌움
                VolumeProfile latestVolumeProfile = null;
                foreach (var zone in overlappedZones)
                {
                    if (zone.biomeProfile != null && zone.biomeProfile.biomeVolumeProfile != null)
                    {
                        latestVolumeProfile = zone.biomeProfile.biomeVolumeProfile;
                    }
                }
                
                // 현재 할당된 프로필과 다를 때만 교체 (GC 방지)
                if (globalPostProcessVolume != null && latestVolumeProfile != null && globalPostProcessVolume.profile != latestVolumeProfile)
                {
                    globalPostProcessVolume.profile = latestVolumeProfile;
                }
            }
        }
    }
}
