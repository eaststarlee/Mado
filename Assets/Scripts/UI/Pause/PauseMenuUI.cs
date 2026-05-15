using UnityEngine;
using UnityEngine.UI;

public class PauseMenuUI : UIPanel
{
    [Header("Buttons")]
    public Button resumeButton;
    public Button settingsButton;
    public Button mainMenuButton;

    // TODO: SettingsRootPanel 연결
    // public UIPanel settingsPanel;
    
    [Header("Contents")]
    [Tooltip("배경을 제외한 실제 버튼들이 들어있는 오브젝트 (Resume, Settings, Quit 등)")]
    public GameObject menuContent;

    [Header("Dialogs")]
    public ConfirmDialog confirmDialog;

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        if (menuContent != null) menuContent.SetActive(false);
    }

    public override void OnGainFocus()
    {
        base.OnGainFocus();
        if (menuContent != null) menuContent.SetActive(true);
    }

    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);
        
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
            
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }



    private void OnResumeClicked()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.ChangeState(GameState.Gameplay);
        }
    }

    private void OnSettingsClicked()
    {
        // TODO: UIManager.Instance.PushPanel(settingsPanel);
        Debug.Log("[PauseMenuUI] Settings Clicked");
    }

    private void OnMainMenuClicked()
    {
        if (confirmDialog != null)
        {
            // 빈 문자열을 넘기면 에디터(인스펙터)에 적어두신 텍스트가 그대로 유지됩니다.
            confirmDialog.ShowDialog("", "", () => 
            {
                // 확인 눌렀을 때의 동작
                Time.timeScale = 1f; // 시간 정지 해제
                
                // [중요] 씬을 다시 로드하기 전에 UI 매니저의 스택을 비워야 잔상이 남지 않습니다.
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ClearStack();
                }

                GameStateManager.Instance.ChangeState(GameState.Loading);
                UnityEngine.SceneManagement.SceneManager.LoadScene("Master"); 
            });
        }
        else
        {
            Debug.LogWarning("[PauseMenuUI] ConfirmDialog가 연결되지 않았습니다.");
        }
    }
}
