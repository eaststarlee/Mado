using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// [레거시 — BootSequencer로 대체됨]
/// 이 스크립트는 Master 씬에서 BootSequencer로 교체해야 합니다.
/// 새 시스템: BootSequencer.cs
/// 이 파일은 교체 완료 후 삭제하세요.
/// </summary>
[System.Obsolete("GameBootstrapper는 BootSequencer로 대체되었습니다. Master 씬의 컴포넌트를 BootSequencer로 교체하세요.")]
public class GameBootstrapper : MonoBehaviour
{
    private IEnumerator Start()
    {
        Debug.LogWarning("[GameBootstrapper] 이 컴포넌트는 레거시입니다. Master 씬에서 BootSequencer로 교체하세요.");

        int waitFrames = 0;
        while (SceneLoader.Instance == null && waitFrames < 15)
        {
            waitFrames++;
            yield return null;
        }

        if (SceneLoader.Instance == null)
        {
            Debug.LogError("[GameBootstrapper] SceneLoader를 찾지 못했습니다.");
            yield break;
        }

        yield return null;
        yield return null;

        bool isEditorSandbox = false;

        // 2. 이미 로드된 방 씬이 있는지 확인 (에디터 샌드박스 지원)
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && scene.name != "Master" && scene.name != "DontDestroyOnLoad" && !scene.name.Contains("Init"))
            {
                isEditorSandbox = true;
                SceneManager.SetActiveScene(scene);
                SceneLoader.Instance.BindToAlreadyLoadedRoom(scene.name);
                break;
            }
        }

        // 3. 주인공 및 펫 활성화 보장
        EnsurePlayerAndPet();

        // 4. 실제 게임 로드 (샌드박스 모드가 아닐 때만)
        if (!isEditorSandbox)
        {
            var saveManager = SaveManager.Instance ?? FindFirstObjectByType<SaveManager>();
            if (saveManager != null)
            {
                // 레거시 호환: 슬롯 0을 기본으로 로드
                saveManager.LoadSlot(0);
                var data = saveManager.CurrentData;
                SceneLoader.Instance.LoadNextRoom(data.lastSceneName, data.lastSpawnId);
            }
            else
            {
                Debug.LogError("[GameBootstrapper] SaveManager를 찾을 수 없습니다!");
            }
        }
    }

    private void EnsurePlayerAndPet()
    {
        var player = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null && !player.gameObject.activeSelf) player.gameObject.SetActive(true);

        var pet = FindAnyObjectByType<PetController>(FindObjectsInactive.Include);
        if (pet != null && !pet.gameObject.activeSelf) pet.gameObject.SetActive(true);
    }
}
