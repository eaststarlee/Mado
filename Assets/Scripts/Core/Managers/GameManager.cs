using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체를 관리하는 싱글톤 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Respawn Settings")]
    [SerializeField] private float fallbackRespawnDelay = 2f; // FadeManager 없을 때만 사용
    
    private Transform currentSavePoint;
    private PlayerController player;
    private PlayerHealth playerHealth;
    private Collider2D playerCollider;
    private Rigidbody2D playerRigidbody;
    
    private bool isRespawning = false;
    
    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void OnEnable()
    {
        // 이벤트 구독
        GameEvents.OnPlayerDeath += OnPlayerDeathEvent;
        GameEvents.OnSavePointActivated += SetCurrentSavePoint;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    void OnDisable()
    {
        // 이벤트 구독 해제
        GameEvents.OnPlayerDeath -= OnPlayerDeathEvent;
        GameEvents.OnSavePointActivated -= SetCurrentSavePoint;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    /// <summary>
    /// Player가 자신을 등록 (Find 대신 자가 등록 패턴)
    /// </summary>
    public void RegisterPlayer(PlayerController playerController)
    {
        player = playerController;
        playerHealth = player.GetComponent<PlayerHealth>();
        playerCollider = player.GetComponent<Collider2D>();
        playerRigidbody = player.RB;
        
        Debug.Log("Player registered to GameManager");
    }
    
    /// <summary>
    /// 세이브 포인트 설정
    /// </summary>
    public void SetCurrentSavePoint(Transform savePoint)
    {
        currentSavePoint = savePoint;
        Debug.Log($"Current save point set: {savePoint.name}");
    }
    
    /// <summary>
    /// 플레이어 사망 이벤트 핸들러
    /// </summary>
    private void OnPlayerDeathEvent()
    {
        // 중복 방지
        if (isRespawning) return;
        
        StartCoroutine(RespawnPlayer());
    }
    
    /// <summary>
    /// 부활 실행 (Fade 연출 + 완전한 초기화)
    /// </summary>
    private IEnumerator RespawnPlayer()
    {
        isRespawning = true;
        
        // TimeScale 안전 처리
        Time.timeScale = 1f;
        
        // 펫 AI 즉시 정지 (Ghost Follow 방지)
        if (player.Pet != null)
        {
            player.Pet.StopFollow();
            
            var petRB = player.Pet.GetComponent<Rigidbody2D>();
            if (petRB != null)
            {
                petRB.linearVelocity = Vector2.zero;
            }
            
            player.Pet.StopAllCoroutines();
        }
        
        // 플레이어 피격 판정 차단
        if (playerCollider != null)
        {
            playerCollider.enabled = false;
        }
        
        // Fade Out (암전 시작)
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeOut(1f);
        }
        else
        {
            // Fallback: FadeManager 없을 때
            yield return new WaitForSeconds(fallbackRespawnDelay);
        }
        
        // 완전 암전 상태 보장
        yield return null;
        
        // === 완전 암전 상태: 모든 재배치 수행 ===
        
        if (player != null && currentSavePoint != null)
        {
            // 플레이어 재배치
            player.transform.position = currentSavePoint.position;
            playerRigidbody.linearVelocity = Vector2.zero;
            playerRigidbody.angularVelocity = 0f;
            playerRigidbody.bodyType = RigidbodyType2D.Dynamic;
            
            // 체력 복구
            if (playerHealth != null)
            {
                playerHealth.ResetHealth();
            }
            
            // 펫 재배치 (PetSpawnPoint 활용)
            if (player.Pet != null)
            {
                Vector3 targetPos;
                var savePointScript = currentSavePoint.GetComponent<SavePoint>();
                
                if (savePointScript != null && savePointScript.petSpawnPoint != null)
                {
                    targetPos = savePointScript.petSpawnPoint.position;
                }
                else
                {
                    targetPos = currentSavePoint.position + new Vector3(-1f, 0.5f, 0);
                }
                
                player.Pet.transform.position = targetPos;
                
                var trail = player.Pet.GetComponentInChildren<TrailRenderer>();
                if (trail != null)
                {
                    trail.emitting = false;
                    trail.Clear();
                }
            }
        }
        
        // 물리/Trail 적용 대기
        yield return null;
        
        // 피격 판정 복구
        if (playerCollider != null)
        {
            playerCollider.enabled = true;
        }
        
        // 펫 기능 복구
        if (player.Pet != null)
        {
            player.Pet.ResumeFollow();
            
            var trail = player.Pet.GetComponentInChildren<TrailRenderer>();
            if (trail != null)
            {
                trail.emitting = true;
            }
        }
        
        // Idle 상태로 복귀
        player.StateMachine.ChangeState(player.IdleState);
        
        // Fade In (화면 밝아짐)
        if (FadeManager.Instance != null)
        {
            yield return FadeManager.Instance.FadeIn(1f);
        }
        
        isRespawning = false;
    }
    
    /// <summary>
    /// 씬 로드 시 기본 세이브 포인트 설정 (안전 체크)
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 현재 세이브 포인트가 없을 때만 기본 세이브 포인트 찾기
        if (currentSavePoint == null)
        {
            var defaultSavePoint = FindFirstObjectByType<SavePoint>();
            if (defaultSavePoint != null && defaultSavePoint.isDefaultSavePoint)
            {
                SetCurrentSavePoint(defaultSavePoint.transform);
            }
        }
    }
}
