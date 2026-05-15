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

    private string currentTabName = "";

    private void OnEnable()
    {
        BackToNavigation(); 
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
        if (Input.GetKeyDown(KeyCode.Escape))
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
