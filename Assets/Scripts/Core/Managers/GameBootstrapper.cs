using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임이 시작될 때 최초의 씬(방)을 불러오고 세이브 데이터를 연결해주는 부팅 매니저.
/// 이 컴포넌트는 Master 씬의 Managers 오브젝트(또는 전용 오브젝트)에 부착합니다.
/// </summary>
public class GameBootstrapper : MonoBehaviour
{
    private IEnumerator Start()
    {
        Debug.Log("[GameBootstrapper] Start 코루틴 진입 완료 (매니저 초기화 대기 시작)");

        // 1. 모든 싱글톤 매니저들이 Awake를 마칠 때까지 대기
        int waitFrames = 0;
        while (SceneLoader.Instance == null && waitFrames < 15)
        {
            waitFrames++;
            yield return null;
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[GameBootstrapper] SceneLoader 인스턴스를 찾지 못했습니다. (WaitFrames: " + waitFrames + ")");
            yield break;
        }

        Debug.Log($"[GameBootstrapper] SceneLoader 준비됨 (WaitFrames: {waitFrames}). 씬 상태 점검 시작...");

        // 2프레임 더 대기하여 씬들이 Hierarchy에 완전히 올라오도록 함
        yield return null;
        yield return null;

        bool isEditorSandbox = false;

#if UNITY_EDITOR
        Debug.Log($"[GameBootstrapper] 현재 로드된 씬 수: {SceneManager.sceneCount}");
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            Debug.Log($"[GameBootstrapper] 씬 확인중 [{i}]: {scene.name} (Loaded: {scene.isLoaded})");
            
            // Master/DontDestroyOnLoad 외에 이미 로드된 방 씬이 있다면 샌드박스 모드로 판단
            if (scene.isLoaded && scene.name != "Master" && scene.name != "DontDestroyOnLoad" && !scene.name.Contains("Init"))
            {
                isEditorSandbox = true;
                Debug.Log($"[GameBootstrapper] >>> 에디터 샌드박스 모드 확정: '{scene.name}' 기준 바인딩 수행 <<<");
                
                SceneManager.SetActiveScene(scene);
                SceneLoader.Instance.BindToAlreadyLoadedRoom(scene.name);
                break;
            }
        }

        if (!isEditorSandbox)
        {
            Debug.Log("[GameBootstrapper] 에디터에서 실행 중이나, 적절한 방 씬을 찾지 못했습니다. (일반 로드 시퀀스로 전환 가능성 있음)");
        }
#endif

        // [추가] 빌드/에디터 공통: 세이브 데이터를 불러오기 전, Master 씬의 플레이어와 펫을 먼저 찾아 활성화
        EnsurePlayerAndPet();

        if (!isEditorSandbox)
        {
            Debug.Log("[GameBootstrapper] 실제 게임 로드 시퀀스 시작 (세이브 파일 확인)");
            
            var saveManager = SaveManager.Instance ?? FindFirstObjectByType<SaveManager>();
            if (saveManager != null)
            {
                SaveData currentSave = saveManager.LoadGame();
                Debug.Log($"[GameBootstrapper] 세이브 데이터 로드 완료: {currentSave.lastSceneName} (Spawn: {currentSave.lastSpawnId})");
                SceneLoader.Instance.LoadNextRoom(currentSave.lastSceneName, currentSave.lastSpawnId);
            }
            else
            {
                Debug.LogError("[GameBootstrapper] 치명적 에러: SaveManager 인스턴스를 찾을 수 없습니다.");
            }
        }
    }

    /// <summary>
    /// Master 씬에 숨겨져(비활성화) 있을 수 있는 플레이어와 펫을 찾아 활성화합니다.
    /// </summary>
    private void EnsurePlayerAndPet()
    {
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            if (!player.gameObject.activeSelf)
            {
                Debug.Log("[GameBootstrapper] 비활성화된 플레이어를 발견하여 활성화합니다.");
                player.gameObject.SetActive(true);
            }
        }
        else
        {
            Debug.LogWarning("[GameBootstrapper] 플레이어를 찾을 수 없습니다. (Master 씬 구성을 확인하세요)");
        }

        var pet = FindAnyObjectByType<PetController>(FindObjectsInactive.Include);
        if (pet != null)
        {
            if (!pet.gameObject.activeSelf)
            {
                Debug.Log("[GameBootstrapper] 비활성화된 펫을 발견하여 활성화합니다.");
                pet.gameObject.SetActive(true);
            }
        }
    }
}
