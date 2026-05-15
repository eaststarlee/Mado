using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private Stack<UIPanel> panelStack = new Stack<UIPanel>();
    public int PanelCount => panelStack.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameEvents.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDestroy()
    {
        GameEvents.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState prev, GameState current)
    {
        if (current == GameState.Paused)
        {
            // 꺼져있는 PauseMenuUI를 찾아서 켭니다.
            var pauseMenu = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            if (pauseMenu != null)
            {
                PushPanel(pauseMenu);
            }
        }
        else if (prev == GameState.Paused && current != GameState.Paused)
        {
            // 게임으로 돌아가면 스택을 비웁니다.
            ClearStack();
        }
    }

    public void PushPanel(UIPanel panel)
    {
        if (panel == null) return;
        
        // 중복 푸시 방지
        if (panelStack.Count > 0 && panelStack.Peek() == panel) return;

        // 부모 캔버스 자동 활성화
        Canvas parentCanvas = panel.GetComponentInParent<Canvas>(true);
        if (parentCanvas != null && !parentCanvas.gameObject.activeSelf)
        {
            parentCanvas.gameObject.SetActive(true);
        }

        if (panelStack.Count > 0)
        {
            panelStack.Peek().OnLostFocus();
        }

        panelStack.Push(panel);
        panel.Show();
    }

    public void PopPanel()
    {
        if (panelStack.Count > 0)
        {
            var topPanel = panelStack.Pop();
            topPanel.Hide();

            if (panelStack.Count > 0)
            {
                panelStack.Peek().OnGainFocus();
            }
        }
    }

    public void ClearStack()
    {
        while (panelStack.Count > 0)
        {
            var panel = panelStack.Pop();
            panel.Hide();
        }
    }
}
