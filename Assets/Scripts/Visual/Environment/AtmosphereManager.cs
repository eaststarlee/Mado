using UnityEngine;
using UnityEngine.Rendering;

namespace Mado.Visual.Environment
{
    public class AtmosphereManager : MonoBehaviour
    {
        public static AtmosphereManager Instance { get; private set; }

        [Header("Scene References")]
        [Tooltip("씬의 메인 Directional Light (태양광/달빛)")]
        public Light globalDirectionalLight;
        
        [Header("Current State")]
        public BiomeAtmosphereProfile currentProfile;
        private BiomeAtmosphereProfile sceneDefaultProfile;
        
        
        [Header("Settings")]
        [Tooltip("구역 전환 시 색상/안개가 변하는 속도")]
        public float transitionSpeed = 2f;

        // Internal blending targets
        private Color targetDirColor;
        private float targetDirIntensity;
        private Color targetAmbientColor;
        
        private Color currentFogColor;
        private Color targetFogColor;
        
        private float currentFogStart;
        private float targetFogStart;
        
        private float currentFogEnd;
        private float targetFogEnd;

        // Shader property IDs (Global Properties in Shader Graph)
        private readonly int fogColorID = Shader.PropertyToID("_GlobalFogColor");
        private readonly int fogStartID = Shader.PropertyToID("_GlobalFogStart");
        private readonly int fogEndID = Shader.PropertyToID("_GlobalFogEnd");

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // 초기값 세팅
            currentFogColor = Shader.GetGlobalColor(fogColorID);
            currentFogStart = Shader.GetGlobalFloat(fogStartID);
            currentFogEnd = Shader.GetGlobalFloat(fogEndID);

            if (currentProfile != null)
            {
                TransitionToBiome(currentProfile, true);
            }
        }

        private void OnEnable()
        {
            GameEvents.OnRoomEntered += HandleRoomEntered;
        }

        private void OnDisable()
        {
            GameEvents.OnRoomEntered -= HandleRoomEntered;
        }

        private void HandleRoomEntered(RoomData room)
        {
            if (room.defaultBiomeProfile != null)
            {
                sceneDefaultProfile = room.defaultBiomeProfile;
                TransitionToBiome(sceneDefaultProfile, true);
            }
        }

        public void RevertToDefaultProfile()
        {
            if (sceneDefaultProfile != null)
            {
                TransitionToBiome(sceneDefaultProfile, false);
            }
        }

        public void TransitionToBiome(BiomeAtmosphereProfile profile, bool instant = false)
        {
            if (profile == null) return;
            currentProfile = profile;

            targetDirColor = profile.directionalLightColor;
            targetDirIntensity = profile.directionalLightIntensity;
            targetAmbientColor = profile.ambientColor;
            targetFogColor = profile.fogColor;
            targetFogStart = profile.fogStartDistance;
            targetFogEnd = profile.fogEndDistance;

            // TODO: Particle System 교체 로직은 Phase 3.3에서 Object Pool과 함께 연동

            if (instant)
            {
                if (globalDirectionalLight != null)
                {
                    globalDirectionalLight.color = targetDirColor;
                    globalDirectionalLight.intensity = targetDirIntensity;
                }
                RenderSettings.ambientLight = targetAmbientColor;
                
                currentFogColor = targetFogColor;
                currentFogStart = targetFogStart;
                currentFogEnd = targetFogEnd;

                ApplyFogToShader();
            }
        }

        private void Update()
        {
            if (currentProfile == null) return;

            float dt = Time.deltaTime * transitionSpeed;

            // 1. Light Lerp
            if (globalDirectionalLight != null)
            {
                globalDirectionalLight.color = Color.Lerp(globalDirectionalLight.color, targetDirColor, dt);
                globalDirectionalLight.intensity = Mathf.Lerp(globalDirectionalLight.intensity, targetDirIntensity, dt);
            }

            // 2. Ambient Lerp
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbientColor, dt);
            
            // 3. Fog Lerp
            currentFogColor = Color.Lerp(currentFogColor, targetFogColor, dt);
            currentFogStart = Mathf.Lerp(currentFogStart, targetFogStart, dt);
            currentFogEnd = Mathf.Lerp(currentFogEnd, targetFogEnd, dt);

            ApplyFogToShader();
        }

        private void ApplyFogToShader()
        {
            Shader.SetGlobalColor(fogColorID, currentFogColor);
            Shader.SetGlobalFloat(fogStartID, currentFogStart);
            Shader.SetGlobalFloat(fogEndID, currentFogEnd);
        }
    }
}
