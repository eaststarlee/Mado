#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 에디터 환경 편의성 기능 (빌드 시 자동 제외)
/// 방(Room) 씬 단독으로 플레이 버튼을 눌렀을 때, 
/// 관련된 모든 매니저들이 에러 없이 동작하도록 Master 씬을 강제 선행 로드합니다.
/// </summary>
public static class MasterSceneAutoLoader
{
    private const string MasterSceneName = "Master";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoLoadMasterScene()
    {
        bool hasMaster = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == MasterSceneName)
            {
                hasMaster = true;
                break;
            }
        }

        if (!hasMaster)
        {
            Debug.Log($"[Editor QoL] 현재 씬(방) 단독 테스트를 감지했습니다. 매니저 연동을 위해 {MasterSceneName} 씬을 백그라운드로 로드합니다.");
            SceneManager.LoadScene(MasterSceneName, LoadSceneMode.Additive);
        }
    }
}
#endif
