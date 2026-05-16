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

    private string currentTabName = "";

    private void OnEnable()
    {
        BackToNavigation(); 
        if (titleTextObject != null) titleTextObject.SetActive(true);
    }

    public void ShowTab(string tabName)
    {
        currentTabName = tabName;

        if (navigationRoot != null) navigationRoot.SetActive(false);

        if (audioTab != null) audioTab.SetActive(tabName == "Audio");
        if (videoTab != null) videoTab.SetActive(tabName == "Video");
        if (controlsTab != null) controlsTab.SetActive(tabName == "Controls");
    }

    public void BackToNavigation()
    {
        currentTabName = "";

        if (navigationRoot != null) navigationRoot.SetActive(true);

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
}
