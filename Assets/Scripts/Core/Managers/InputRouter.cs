using UnityEngine;
using UnityEngine.InputSystem;

public class InputRouter : MonoBehaviour
{
    public static InputRouter Instance { get; private set; }

    public Controls Actions { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        Actions = new Controls();
    }

    private InputActionMap permanentMap;
    private InputActionMap uiMap;

    private void OnEnable()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
        
        permanentMap = Actions.asset.FindActionMap("Permanent");
        uiMap = Actions.asset.FindActionMap("UI");

        // Permanent 맵은 게임 중 한 번 활성화되면 절대 꺼지지 않음
        if (permanentMap != null) permanentMap.Enable();

        UpdateActionMaps(GameStateManager.Instance != null ? GameStateManager.Instance.CurrentState : GameState.Gameplay);
    }

    private void OnDisable()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
        Actions.Disable();
    }

    private void HandleGameStateChanged(GameState prev, GameState current)
    {
        UpdateActionMaps(current);
    }

    private void UpdateActionMaps(GameState state)
    {
        switch (state)
        {
            case GameState.Gameplay:
                Actions.Player.Enable();
                if (uiMap != null) uiMap.Disable();
                break;
            case GameState.Paused:
                Actions.Player.Disable();
                if (uiMap != null) uiMap.Enable();
                break;
            default:
                // Loading, Cutscene, Dead 등에서는 모두 차단
                Actions.Player.Disable();
                if (uiMap != null) uiMap.Disable();
                break;
        }
    }
}
