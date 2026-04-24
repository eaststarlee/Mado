using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 부팅 시퀀스를 단계별로 관리합니다. GameBootstrapper를 대체합니다.
///
/// ■ 에디터 (방 씬 열고 Play) : 현재 씬 즉시 바인딩, 슬롯 선택 생략
/// ■ 에디터 (Master 씬 Play) : 빌드와 동일하게 슬롯 선택 진행
/// ■ 빌드 (첫 실행)           : 슬롯 선택 → 새 게임
/// ■ 빌드 (재실행)            : 슬롯 선택 → 기존 데이터 로드
///
/// ■ ISaveable 타이밍 보장:
///   씬 로드 완료 후 WaitForEndOfFrame()을 거쳐 BroadcastLoad()를 호출합니다.
///   이렇게 하면 모든 오브젝트의 Start()가 완료된 후 OnLoad가 실행됩니다.
/// </summary>
public class BootSequencer : MonoBehaviour
{
    [Header("슬롯 설정")]
    [Tooltip("슬롯 선택 화면을 건너뛰고 자동으로 이 슬롯을 로드합니다. -1이면 항상 슬롯 선택 화면 표시.")]
    [SerializeField] private int autoLoadSlot = -1;

    [Header("UI 연결")]
    [Tooltip("에디터에서 MainMenuUI 오브젝트를 연결해 주세요.")]
    [SerializeField] private MainMenuUI mainMenuUI;

    private int selectedSlot = -1;
    private bool isSlotSelected = false;

    public void SelectSlotAndStart(int slotIndex)
    {
        selectedSlot = slotIndex;
        isSlotSelected = true;
    }

    private IEnumerator Start()
    {
        // ── 1. 모든 싱글톤 매니저 Awake 완료 대기 ──────────
        int waitFrames = 0;
        while ((SceneLoader.Instance == null || SaveManager.Instance == null) && waitFrames < 15)
        {
            waitFrames++;
            yield return null;
        }

        if (SceneLoader.Instance == null || SaveManager.Instance == null)
        {
            Debug.LogError("[BootSequencer] 필수 매니저를 찾지 못했습니다. Master 씬 구성을 확인하세요.");
            yield break;
        }

        // 씬 Hierarchy 안정화 대기
        yield return null;
        yield return null;

#if UNITY_EDITOR
        // ── 2a. [에디터] 방 씬이 이미 열려있으면 즉시 바인딩 ─
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!IsSystemScene(scene.name) && scene.isLoaded)
            {
                Debug.Log($"[BootSequencer] 에디터 샌드박스: '{scene.name}' 씬 바인딩");

                if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);

                EnsureCharactersActive();
                SceneManager.SetActiveScene(scene);
                SceneLoader.Instance.BindToAlreadyLoadedRoom(scene.name);

                // 모든 Start() 완료 후 ISaveable 복원 (임시 슬롯 0 사용)
                SaveManager.Instance.LoadSlot(0);
                yield return new WaitForEndOfFrame();
                SaveManager.Instance.BroadcastLoad();

                PlaytimeTracker.Instance?.StartTracking();
                yield break;
            }
        }
#endif

        // ── 2b. [빌드 / Master만] 슬롯 결정 ────────────────
        if (autoLoadSlot >= 0)
        {
            selectedSlot = autoLoadSlot;
            isSlotSelected = true;
        }
        else
        {
            // UI 표시
            if (mainMenuUI != null)
            {
                mainMenuUI.gameObject.SetActive(true);
                mainMenuUI.Initialize(this);
            }
            else
            {
                Debug.LogWarning("[BootSequencer] MainMenuUI가 연결되지 않았습니다. 임시로 슬롯 0을 로드합니다.");
                selectedSlot = 0;
                isSlotSelected = true;
            }
        }

        // 유저가 UI에서 버튼을 누를 때까지 무한 대기
        yield return new WaitUntil(() => isSlotSelected);

        // UI 숨김
        if (mainMenuUI != null) mainMenuUI.gameObject.SetActive(false);

        // ── 3. 슬롯 로드 ────────────────────────────────────
        bool loaded = SaveManager.Instance.LoadSlot(selectedSlot);
        if (!loaded)
        {
            Debug.LogError("[BootSequencer] 슬롯 로드 실패.");
            yield break;
        }

        var data = SaveManager.Instance.CurrentData;
        Debug.Log($"[BootSequencer] 슬롯 {selectedSlot} 로드 완료 → {data.lastSceneName} (Spawn: {data.lastSpawnId})");

        // ── 4. 플레이어/펫 활성화 보장 ─────────────────────
        EnsureCharactersActive();

        // ── 5. 씬 로드 ──────────────────────────────────────
        SceneLoader.Instance.LoadNextRoom(data.lastSceneName, data.lastSpawnId);

        // ── 6. 씬 로드 완료 + Start() 사이클 후 ISaveable 복원 ─
        // SceneLoader가 IsTransitioning을 false로 바꿀 때까지 대기
        yield return new WaitUntil(() => !SceneLoader.Instance.IsTransitioning);
        yield return new WaitForEndOfFrame(); // 모든 Start() 완료 보장

        SaveManager.Instance.BroadcastLoad();

        // ── 7. 플레이 시간 추적 시작 ────────────────────────
        if (PlaytimeTracker.Instance != null)
        {
            PlaytimeTracker.Instance.SetInitial(data.totalPlayTime);
            PlaytimeTracker.Instance.StartTracking();
        }

        Debug.Log("[BootSequencer] 부팅 시퀀스 완료.");
    }

    // ── 헬퍼 ────────────────────────────────────────────────

    /// <summary>시스템 씬(Master, DontDestroyOnLoad 등) 여부 판별</summary>
    private bool IsSystemScene(string name)
        => name == "Master" || name == "DontDestroyOnLoad" || name.Contains("Init");

    /// <summary>Master 씬에 비활성화된 플레이어/펫이 있다면 강제 활성화합니다.</summary>
    private void EnsureCharactersActive()
    {
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null && !player.gameObject.activeSelf)
            player.gameObject.SetActive(true);

        var pet = FindAnyObjectByType<PetController>(FindObjectsInactive.Include);
        if (pet != null && !pet.gameObject.activeSelf)
            pet.gameObject.SetActive(true);
    }
}
