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
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        var stateMgr = GameStateManager.Instance;
        if (stateMgr == null) return;

        if (stateMgr.CurrentState == GameState.Loading) return;

        if (stateMgr.CurrentState == GameState.Gameplay)
        {
            stateMgr.ChangeState(GameState.Paused);
        }
        else if (stateMgr.CurrentState == GameState.Paused)
        {
            // [중요] 패널이 2개 이상 쌓여있다면(예: 설정창이 열려있다면),
            // 전역 핸들러인 여기서 일시정지를 풀지 않고, 해당 UI 스크립트(SettingsMenuUI 등)의 Update()에서 
            // 자체적인 ESC 로직(뒤로가기 등)을 수행하도록 양보합니다.
            if (UIManager.Instance != null && UIManager.Instance.PanelCount > 1)
            {
                return;
            }

            // 스택에 패널이 하나뿐이라면(일시정지 메뉴만 있다면) 일시정지를 해제합니다.
            stateMgr.ChangeState(GameState.Gameplay);
        }
    }
}
