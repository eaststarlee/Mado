using UnityEngine;

/// <summary>
/// 세이브 포인트 (할로우나이트 벤치 스타일)
/// </summary>
public class SavePoint : MonoBehaviour
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
            // 사망 중이면 상호작용 불가
            if (targetPlayer.Health.IsDead) return;
            
            // 피격 중이면 상호작용 불가
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
    /// 세이브 포인트 활성화 (재사용 가능)
    /// </summary>
    private void ActivateSavePoint()
    {
        // 처음 발견했을 때만 시각적 상태 업데이트
        if (!isActivated) 
        {
            isActivated = true;
            // TODO: 처음 발견 시 이펙트/사운드
        }
        
        // GameManager에 세이브 포인트 등록
        GameEvents.RaiseSavePointActivated(transform);
        
        // 체력 회복
        if (targetPlayer != null && targetPlayer.Health != null)
        {
            targetPlayer.Health.ResetHealth();
        }
        
        // TODO: 앉는 애니메이션
        // TODO: 저장 이펙트
    }
    
    void OnDrawGizmos()
    {
        // 에디터에서 세이브 포인트 위치 표시
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
