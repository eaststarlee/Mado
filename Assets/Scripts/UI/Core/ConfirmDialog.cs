using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ConfirmDialog : UIPanel
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;

    private UnityAction onConfirmAction;

    private void Start()
    {
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelClicked);
    }

    private void Update()
    {
        // UI의 Cancel 액션(X버튼 등) 또는 Permanent의 Pause 액션(ESC)을 감지
        if (InputRouter.Instance != null && InputRouter.Instance.Actions != null)
        {
            var uiCancel = InputRouter.Instance.Actions.UI.Cancel;
            var permPause = InputRouter.Instance.Actions.Permanent.Pause;

            if (uiCancel.WasPressedThisFrame() || permPause.WasPressedThisFrame())
            {
                OnCancelClicked();
            }
        }
    }

    public void ShowDialog(string title, string message, UnityAction onConfirm)
    {
        // 다이얼로그가 다른 UI에 가려지지 않도록 최상단으로 올림
        transform.SetAsLastSibling();

        if (!string.IsNullOrEmpty(title) && titleText != null) 
            titleText.text = title;
            
        if (!string.IsNullOrEmpty(message) && messageText != null) 
            messageText.text = message;
        
        onConfirmAction = onConfirm;
        
        UIManager.Instance.PushPanel(this);
    }

    private void OnConfirmClicked()
    {
        onConfirmAction?.Invoke();
        UIManager.Instance.PopPanel();
    }

    private void OnCancelClicked()
    {
        UIManager.Instance.PopPanel();
    }
}
