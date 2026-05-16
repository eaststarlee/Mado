using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenuUI : UIPanel
{
    [Header("UI Tab Contents")]
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject videoTab;
    [SerializeField] private GameObject controlsTab;

    [Header("Navigation")]
    [SerializeField] private GameObject navigationRoot; // 탭 버튼들의 부모 (Buttons)
    [SerializeField] private GameObject titleTextObject; // "Settings" 타이틀 텍스트

    [Header("Reset Buttons")]
    [SerializeField] private Button resetAudioButton;
    [SerializeField] private Button resetVideoButton;
    [SerializeField] private Button resetControlsButton;

    private string currentTabName = "";

    private void Start()
    {
        if (resetAudioButton != null) resetAudioButton.onClick.AddListener(OnResetAudioClicked);
        if (resetVideoButton != null) resetVideoButton.onClick.AddListener(OnResetVideoClicked);
        if (resetControlsButton != null) resetControlsButton.onClick.AddListener(OnResetControlsClicked);
    }

    private void OnEnable()
    {
        BackToNavigation(); 
        if (titleTextObject != null) titleTextObject.SetActive(true);
    }

    public void ShowTab(string tabName)
    {
        currentTabName = tabName;

        if (navigationRoot != null) navigationRoot.SetActive(false);
        if (titleTextObject != null) titleTextObject.SetActive(false); // 탭 진입 시 메인 타이틀 숨김

        if (audioTab != null) audioTab.SetActive(tabName == "Audio");
        if (videoTab != null) videoTab.SetActive(tabName == "Video");
        if (controlsTab != null) controlsTab.SetActive(tabName == "Controls");
    }

    public void BackToNavigation()
    {
        currentTabName = "";

        if (navigationRoot != null) navigationRoot.SetActive(true);
        if (titleTextObject != null) titleTextObject.SetActive(true); // 메인 화면 복귀 시 타이틀 표시

        if (audioTab != null) audioTab.SetActive(false);
        if (videoTab != null) videoTab.SetActive(false);
        if (controlsTab != null) controlsTab.SetActive(false);
    }

    private void Update()
    {
        // InputRouter를 통한 ESC 처리
        if (InputRouter.Instance != null && InputRouter.Instance.Actions.Permanent.Pause.WasPressedThisFrame())
        {
            if (!string.IsNullOrEmpty(currentTabName))
            {
                BackToNavigation();
            }
            else
            {
                UIManager.Instance.PopPanel();
            }
        }
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SaveSettings();
        }
    }

    private void OnResetAudioClicked()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.ResetAudioDefaults();
        RefreshActiveTab();
    }

    private void OnResetVideoClicked()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.ResetVideoDefaults();
        RefreshActiveTab();
    }

    private void OnResetControlsClicked()
    {
        if (SettingsManager.Instance != null) SettingsManager.Instance.ResetInputDefaults();
        RefreshActiveTab();
    }

    private void RefreshActiveTab()
    {
        // 활성화된 탭을 껐다가 다시 켜서 하위 컴포넌트들의 OnEnable()이 호출되도록 유도 (UI 값 갱신)
        if (audioTab != null && audioTab.activeSelf) { audioTab.SetActive(false); audioTab.SetActive(true); }
        if (videoTab != null && videoTab.activeSelf) { videoTab.SetActive(false); videoTab.SetActive(true); }
        if (controlsTab != null && controlsTab.activeSelf) { controlsTab.SetActive(false); controlsTab.SetActive(true); }
    }
}
