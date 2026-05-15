using UnityEngine;

public class PauseInputHandler : MonoBehaviour
{
    private UnityEngine.InputSystem.InputAction pauseAction;

    private void Start()
    {
        if (InputRouter.Instance != null && InputRouter.Instance.Actions != null)
        {
            var permanentMap = InputRouter.Instance.Actions.asset.FindActionMap("Permanent");
            if (permanentMap != null)
            {
                pauseAction = permanentMap.FindAction("Pause");
                if (pauseAction != null)
                {
                    pauseAction.performed -= OnPausePerformed;
                    pauseAction.performed += OnPausePerformed;
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
        }
    }

    private void OnPausePerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
    {
        TogglePause();
    }

    private int lastToggleFrame = -1;

    private void TogglePause()
    {
        // 한 프레임에 여러 번 토글되는 것을 원천 차단 (뉴 인풋 시스템의 중복 트리거 방지)
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        var stateMgr = GameStateManager.Instance;
        if (stateMgr == null) return;

        // 로딩 중에는 일시정지 조작 무시
        if (stateMgr.CurrentState == GameState.Loading) return;

        if (stateMgr.CurrentState == GameState.Gameplay)
        {
            stateMgr.ChangeState(GameState.Paused);
        }
        else if (stateMgr.CurrentState == GameState.Paused)
        {
            stateMgr.ChangeState(GameState.Gameplay);
        }
    }
}
