using UnityEngine;
using System.Collections.Generic;

namespace Mado.Visual.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public class BiomeZoneVolume : MonoBehaviour
    {
        [Tooltip("이 구역에 적용될 분위기 데이터 (조명 및 안개 색상)")]
        public BiomeAtmosphereProfile biomeProfile;
        
        [Tooltip("이 구역에서 흩날릴 파티클 프리팹들 (여러 개 동시 배치 가능)")]
        public GameObject[] ambientParticlePrefabs;
        
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
            if (ambientParticlePrefabs == null || ambientParticlePrefabs.Length == 0) return;

            foreach (GameObject prefab in ambientParticlePrefabs)
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
                    }

                    if (renderer != null)
                    {
                        renderer.sortingLayerName = "Player";
                    }
                    
                    // 처음엔 Emission을 꺼둠. 매니저가 카메라 Frustum 체크 후 켜줄 예정
                    var emission = ps.emission;
                    emission.enabled = false;
                    ps.Play(true);
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
                        // 오랫동안 화면 밖에 있어서 파티클이 모두 소멸한 상태에서 화면에 들어오면
                        // 빈 공간에서 서서히 차오르는 것을 방지하기 위해 강제 예열(Prewarm)
                        if (ps.particleCount == 0)
                        {
                            emission.enabled = true;
                            ps.Simulate(15f, true, true, false);
                            ps.Play(true);
                        }
                        else
                        {
                            emission.enabled = true;
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
