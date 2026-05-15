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

    public void ShowDialog(string title, string message, UnityAction onConfirm)
    {
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
