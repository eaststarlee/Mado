using UnityEngine;
using System.Collections.Generic;

namespace Mado.Visual.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class BiomeZoneVolume : MonoBehaviour
    {
        [Tooltip("이 구역에 적용될 분위기 데이터 (조명 및 안개 색상)")]
        public BiomeAtmosphereProfile biomeProfile;

        
        private Collider2D _collider;
        public Collider2D VolumeCollider => _collider;

        private List<ParticleSystem> _particleInstances = new List<ParticleSystem>();

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            InitializeParticleSystem();
        }

        private void InitializeParticleSystem()
        {
            if (biomeProfile == null || biomeProfile.ambientParticlePrefabs == null || biomeProfile.ambientParticlePrefabs.Length == 0) return;

            foreach (GameObject prefab in biomeProfile.ambientParticlePrefabs)
            {
                if (prefab == null) continue;

                GameObject instance = Instantiate(prefab, transform);
                
                // 파티클을 Zone의 중앙에 배치 (회전값은 프리팹 설정을 그대로 존중함)
                instance.transform.position = new Vector3(_collider.bounds.center.x, _collider.bounds.center.y, -5f);
                
                ParticleSystem ps = instance.GetComponent<ParticleSystem>();
                ParticleSystemRenderer renderer = instance.GetComponent<ParticleSystemRenderer>();

                if (ps != null)
                {
                    _particleInstances.Add(ps);
                    var shape = ps.shape;
                    if (shape.enabled)
                    {
                        // 프리팹이 회전(-90도 등)되어 있더라도, 월드 기준의 X, Y 크기(Bounds)가 
                        // 파티클 Shape의 올바른 로컬 축에 들어가도록 자동 매핑합니다.
                        Transform pt = instance.transform;
                        float depth = shape.scale.z; // Z 깊이는 프리팹 원래 값 유지
                        Vector2 bSize = _collider.bounds.size;

                        float scaleX = Mathf.Abs(pt.right.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.right.y) > 0.5f ? bSize.y : depth);
                        float scaleY = Mathf.Abs(pt.up.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.up.y) > 0.5f ? bSize.y : depth);
                        float scaleZ = Mathf.Abs(pt.forward.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.forward.y) > 0.5f ? bSize.y : depth);

                        shape.scale = new Vector3(scaleX, scaleY, scaleZ);
                        
                        // Box 표면에서만 생성되어 벽면 기둥처럼 보이는 것을 방지 (볼륨 전체 생성)
                        shape.boxThickness = new Vector3(1f, 1f, 1f);
                    }

                    if (renderer != null)
                    {
                        renderer.sortingLayerName = "Player";
                    }

                    var trigger = ps.trigger;
                    trigger.enabled = false;
                    
                    // [최적화 & 버그픽스] (피드백 1번, 4번, 5번)
                    var main = ps.main;
                    
                    // 프리팹 원본에 자동 재생이 켜져있어 생기는 Awake 폭탄 연산을 방지합니다.
                    // (Shape.scale 세팅은 이미 위에서 끝났으므로, 이후 Play() 시 올바른 크기로 예열됩니다.)
                    main.playOnAwake = false;
                    
                    // 1번 피드백: Prewarm 작동 조건 강제 활성화 (loop 필수, startDelay 0 필수)
                    main.loop = true;
                    main.startDelay = 0f;
                    
                    // 5번 피드백: 우리가 스크립트에서 emission으로 직접 제어하므로, 
                    // 엔진 내부 Culling Mode와 충돌하지 않도록 Always Simulate로 강제 설정합니다.
                    main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                    
                    // 처음엔 Emission을 꺼둠. 매니저가 카메라 Frustum 체크 후 켜줄 예정
                    var emission = ps.emission;
                    emission.enabled = false;
                    
                    // 예전처럼 여기서 ps.Clear()나 ps.Play()를 호출하지 않고 대기합니다.
                }
            }
        }

        private void OnEnable()
        {
            AtmosphereManager.RegisterZone(this);
        }

        private void OnDisable()
        {
            AtmosphereManager.UnregisterZone(this);
            if (AtmosphereManager.Instance != null)
            {
                AtmosphereManager.Instance.RemoveOverlappedZone(this);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                AtmosphereManager.Instance?.AddOverlappedZone(this);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                AtmosphereManager.Instance?.RemoveOverlappedZone(this);
            }
        }

        public void SetCullingState(bool isVisible)
        {
            if (_particleInstances.Count == 0) return;

            foreach (var ps in _particleInstances)
            {
                if (ps == null) continue;
                
                var emission = ps.emission;
                
                if (isVisible)
                {
                    if (!emission.enabled)
                    {
                        emission.enabled = true;

                        // 2번 피드백: 오래 이탈해 파티클이 완전히 0이 된 상태에서 재진입 시
                        // 단순 emission.enabled = true 만 하면 텅 빈 채로 서서히 차오르게 됩니다.
                        // 이를 방지하기 위해 particleCount == 0 체크 후 리필(Prewarm) 수행.
                        if (ps.particleCount == 0)
                        {
                            // 3번 피드백: 프레임 스파이크 방지를 위해 매니저에게 스태거링(Staggering) Prewarm을 요청
                            if (AtmosphereManager.Instance != null)
                            {
                                AtmosphereManager.Instance.EnqueuePrewarm(ps);
                            }
                            else
                            {
                                // Fallback (매니저가 없을 시 직접 즉시 처리)
                                // 4번 피드백 준수: Stop -> prewarm = true -> Play 순서 엄수
                                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                                var main = ps.main;
                                main.prewarm = true;
                                ps.Play(true);
                            }
                        }
                        else if (!ps.isPlaying)
                        {
                            // 0이 아닐 경우(아직 잔여 파티클이 있을 경우) 그냥 다시 켜주기만 함
                            ps.Play(true);
                        }
                    }
                }
                else
                {
                    // 화면 밖으로 나가면 기존 파티클이 뚝 끊기지 않고 수명에 따라 자연 소멸하도록 Emission만 끔
                    emission.enabled = false;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
                Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            }
        }
#endif
    }
}
