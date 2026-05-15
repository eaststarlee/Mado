using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 클릭할 때마다 값을 순환 선택(Cycle)하고, 개별 Apply 버튼을 제어하는 UI 컴포넌트.
/// </summary>
public class SettingsItemUI_Cycle : MonoBehaviour
{
    public enum CycleTarget { Resolution, ScreenMode, VSync }
    [SerializeField] private CycleTarget target;
    [SerializeField] private Button nextButton; 
    [SerializeField] private TextMeshProUGUI valueText; 
    [SerializeField] private Button individualApplyButton; 

    private List<string> options = new List<string>();
    private int currentIndex = 0;
    private int originalIndex = 0;

    private void OnEnable()
    {
        SetupOptions();
        LoadCurrentValue();
        UpdateUI();
        
        if (nextButton != null)
        {
            nextButton.onClick.RemoveListener(OnNextClicked);
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (individualApplyButton != null)
        {
            individualApplyButton.onClick.RemoveListener(OnApplyClicked);
            individualApplyButton.onClick.AddListener(OnApplyClicked);
            individualApplyButton.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
        if (individualApplyButton != null) individualApplyButton.onClick.RemoveListener(OnApplyClicked);
    }

    private void SetupOptions()
    {
        options.Clear();
        switch (target)
        {
            case CycleTarget.Resolution:
                options.AddRange(new string[] { "1920 x 1080", "1600 x 900", "1280 x 720" });
                break;
            case CycleTarget.ScreenMode:
                options.AddRange(new string[] { "Fullscreen", "Windowed", "Borderless" });
                break;
            case CycleTarget.VSync:
                options.AddRange(new string[] { "On", "Off" });
                break;
        }
    }

    private void LoadCurrentValue()
    {
        if (SettingsManager.Instance == null) return;
        var data = SettingsManager.Instance.Data;

        switch (target)
        {
            case CycleTarget.Resolution:
                string currentRes = $"{data.Video.ResolutionWidth} x {data.Video.ResolutionHeight}";
                currentIndex = options.FindIndex(o => o == currentRes);
                break;
            case CycleTarget.ScreenMode:
                currentIndex = data.Video.FullScreenMode switch {
                    FullScreenMode.ExclusiveFullScreen => 0,
                    FullScreenMode.Windowed => 1,
                    FullScreenMode.FullScreenWindow => 2,
                    _ => 0
                };
                break;
            case CycleTarget.VSync:
                currentIndex = data.Video.VSync ? 0 : 1;
                break;
        }

        if (currentIndex < 0) currentIndex = 0;
        originalIndex = currentIndex;
    }

    private void OnNextClicked()
    {
        currentIndex = (currentIndex + 1) % options.Count;
        UpdateUI();
        
        // VSync는 즉시 적용 (Apply 버튼 필요 없음)
        if (target == CycleTarget.VSync)
        {
            ApplyValue();
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ApplyVideoSettings();
                SettingsManager.Instance.SaveSettings();
            }
        }
        // 해상도나 화면모드는 값이 다를 때만 Apply 버튼 활성화
        else if (individualApplyButton != null)
        {
            individualApplyButton.gameObject.SetActive(currentIndex != originalIndex);
        }
    }

    private void UpdateUI()
    {
        if (valueText != null) valueText.text = options[currentIndex];
    }

    private void OnApplyClicked()
    {
        ApplyValue();
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ApplyVideoSettings();
            SettingsManager.Instance.SaveSettings();
        }
        originalIndex = currentIndex;
        if (individualApplyButton != null) individualApplyButton.gameObject.SetActive(false);
    }

    public void ApplyValue()
    {
        var data = SettingsManager.Instance.Data;
        switch (target)
        {
            case CycleTarget.Resolution:
                string[] res = options[currentIndex].Split('x');
                data.Video.ResolutionWidth = int.Parse(res[0].Trim());
                data.Video.ResolutionHeight = int.Parse(res[1].Trim());
                break;
            case CycleTarget.ScreenMode:
                data.Video.FullScreenMode = currentIndex switch {
                    0 => FullScreenMode.ExclusiveFullScreen,
                    1 => FullScreenMode.Windowed,
                    2 => FullScreenMode.FullScreenWindow,
                    _ => FullScreenMode.ExclusiveFullScreen
                };
                break;
            case CycleTarget.VSync:
                data.Video.VSync = (currentIndex == 0);
                break;
        }
    }
}
