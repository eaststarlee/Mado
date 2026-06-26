using UnityEngine;

/// <summary>
/// 세이브 포인트 (할로우나이트 벤치 스타일)
/// 플레이어가 상호작용하면 SaveManager를 통해 전체 게임 상태를 저장합니다.
/// </summary>
public class SavePoint : SpawnPoint
{
    [Header("Settings")]
    public bool isDefaultSavePoint = true;
    
    [Header("Spawn Points")]
    public Transform petSpawnPoint; // 펫 스폰 위치 (에디터에서 설정)

    private bool playerInRange = false;
    private bool isActivated = false;
    private PlayerController targetPlayer;
    
    void Start()
    {
        // 기본 세이브 포인트면 자동 활성화
        if (isDefaultSavePoint)
        {
            ActivateSavePoint();
        }
    }
    
    void Update()
    {
        // ↓키 또는 ↑키로 상호작용
        if (playerInRange && targetPlayer != null && (targetPlayer.IsDownPressed || targetPlayer.IsUpPressed))
        {
            if (targetPlayer.Health.IsDead) return;
            if (targetPlayer.StateMachine.CurrentState == targetPlayer.HitState) return;
            
            ActivateSavePoint();
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            targetPlayer = other.GetComponent<PlayerController>();
            // TODO: UI 표시 ("↓키로 저장")
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            targetPlayer = null;
            // TODO: UI 숨김
        }
    }
    
    /// <summary>
    /// 세이브 포인트 활성화.
    /// 1) GameManager에 이 포인트를 현재 세이브 포인트로 등록
    /// 2) 체력 회복
    /// 3) SaveData에 위치 기록 후 비동기 저장
    /// </summary>
    private void ActivateSavePoint()
    {
        if (!isActivated)
        {
            isActivated = true;
            // TODO: 처음 발견 시 이펙트/사운드
        }
        
        // GameManager에 이 포인트 등록 (기존 호환성용)
        GameEvents.RaiseSavePointActivated(transform);
        
        // 체력 회복
        if (targetPlayer != null && targetPlayer.Health != null)
        {
            targetPlayer.Health.ResetHealth();
        }

        if (GameProgressManager.Instance != null)
        {
            var data = GameProgressManager.Instance.CurrentData;
            if (data != null)
            {
                data.lastSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                data.lastSpawnId = spawnId;
                data.lastPosition = new float[] { transform.position.x, transform.position.y, transform.position.z };

                // 스탯 갱신 (선택적)
                if (targetPlayer != null && targetPlayer.Health != null)
                {
                    data.currentHP = targetPlayer.Health.CurrentHealth;
                }
            }

            // 파일로 비동기 덤프
            GameProgressManager.Instance.SaveCurrentProgress();

            Debug.Log($"[SavePoint] '{spawnId}' 세이브 포인트 저장 완료.");
        }
        else
        {
            Debug.LogWarning("[SavePoint] GameProgressManager 인스턴스가 없습니다. 저장을 건너뜁니다.");
        }

        // 앉는 애니메이션 / 중앙 정렬 / 이동 제한
        if (targetPlayer != null)
        {
            targetPlayer.RestAt(transform.position.x);
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
