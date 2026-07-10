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
        private Dictionary<ParticleSystem, GameObject> _particlePrefabMap = new Dictionary<ParticleSystem, GameObject>();

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            // 지연 생성을 위해 Awake 단계의 파티클 인스턴스화 로직을 모두 제거했습니다.
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
            if (biomeProfile == null || biomeProfile.ambientParticlePrefabs == null || biomeProfile.ambientParticlePrefabs.Length == 0) return;

            if (isVisible)
            {
                // 화면에 보이기 시작함: 파티클이 없으면 풀에서 빌려옵니다.
                if (_particleInstances.Count == 0)
                {
                    foreach (GameObject prefab in biomeProfile.ambientParticlePrefabs)
                    {
                        if (prefab == null) continue;
                        
                        // 1. Request (풀에서 획득 또는 생성)
                        ParticleSystem ps = AtmosphereManager.Instance.RequestParticle(prefab, transform);
                        
                        // 위치 초기화
                        ps.transform.position = new Vector3(_collider.bounds.center.x, _collider.bounds.center.y, -5f);
                        
                        // 2. 크기(Shape) 맞춤
                        var shape = ps.shape;
                        if (shape.enabled)
                        {
                            Transform pt = ps.transform;
                            float depth = shape.scale.z;
                            Vector2 bSize = _collider.bounds.size;

                            float scaleX = Mathf.Abs(pt.right.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.right.y) > 0.5f ? bSize.y : depth);
                            float scaleY = Mathf.Abs(pt.up.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.up.y) > 0.5f ? bSize.y : depth);
                            float scaleZ = Mathf.Abs(pt.forward.x) > 0.5f ? bSize.x : (Mathf.Abs(pt.forward.y) > 0.5f ? bSize.y : depth);

                            shape.scale = new Vector3(scaleX, scaleY, scaleZ);
                            shape.boxThickness = new Vector3(1f, 1f, 1f);
                        }
                        
                        _particleInstances.Add(ps);
                        _particlePrefabMap[ps] = prefab;

                        // 3. emission.enabled = true 세팅
                        var emission = ps.emission;
                        emission.enabled = true;
                        
                        // 4. Prewarm 큐에 스태거링 요청
                        AtmosphereManager.Instance.EnqueuePrewarm(ps);
                    }
                }
                else
                {
                    // 이미 인스턴스가 존재할 경우
                    foreach (var ps in _particleInstances)
                    {
                        if (ps == null) continue;
                        
                        var emission = ps.emission;
                        if (!emission.enabled)
                        {
                            emission.enabled = true;
                            if (ps.particleCount == 0)
                            {
                                AtmosphereManager.Instance.EnqueuePrewarm(ps);
                            }
                            else if (!ps.isPlaying)
                            {
                                ps.Play(true);
                            }
                        }
                    }
                }
            }
            else
            {
                // 화면 밖으로 나감: 배출만 멈추고 파티클이 모두 죽으면 풀에 반환합니다.
                for (int i = _particleInstances.Count - 1; i >= 0; i--)
                {
                    var ps = _particleInstances[i];
                    if (ps == null)
                    {
                        _particleInstances.RemoveAt(i);
                        continue;
                    }
                    
                    var emission = ps.emission;
                    emission.enabled = false;
                    
                    if (ps.particleCount == 0)
                    {
                        AtmosphereManager.Instance.ReturnParticle(_particlePrefabMap[ps], ps);
                        _particlePrefabMap.Remove(ps);
                        _particleInstances.RemoveAt(i);
                    }
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
