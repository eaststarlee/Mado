using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class SettingsItemUI_KeyBind : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI actionNameText;
    [SerializeField] private TextMeshProUGUI currentBindingText;
    [SerializeField] private Button rebindButton;

    [Header("Target Action")]
    [SerializeField] private string actionPath;
    [SerializeField] private int bindingIndex;

    private InputAction targetAction;

    private void Start()
    {
        if (rebindButton != null)
            rebindButton.onClick.AddListener(OnRebindClicked);

        if (InputRouter.Instance != null && InputRouter.Instance.Actions != null)
        {
            targetAction = InputRouter.Instance.Actions.asset.FindAction(actionPath);
            UpdateUI();
        }
    }

    private void OnEnable() => UpdateUI();

    private void OnRebindClicked()
    {
        if (targetAction == null) return;

        currentBindingText.text = "Press any key...";
        rebindButton.interactable = false;

        InputRebindSystem.StartRebinding(
            targetAction, 
            bindingIndex, 
            onComplete: () => 
            {
                UpdateUI();
                rebindButton.interactable = true;
            },
            onCancel: () => 
            {
                UpdateUI();
                rebindButton.interactable = true;
            }
        );
    }

    public void UpdateUI()
    {
        if (targetAction != null && currentBindingText != null)
        {
            currentBindingText.text = InputRebindSystem.GetBindingName(targetAction, bindingIndex);
        }
    }
}
