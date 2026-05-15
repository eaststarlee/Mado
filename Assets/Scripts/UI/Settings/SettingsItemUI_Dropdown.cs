using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 비디오 탭의 드롭다운(해상도)을 제어하는 UI 컴포넌트.
/// 상용 게임 스탠다드에 맞춰 데이터만 갱신하며, 실제 화면 해상도 변경은 Apply 버튼을 누를 때 발생합니다.
/// </summary>
public class SettingsItemUI_Dropdown : MonoBehaviour
{
    public enum DropdownTarget { Resolution, FrameRate }
    [SerializeField] private DropdownTarget target;
    [SerializeField] private TMP_Dropdown dropdown;

    private List<string> resolutionOptions = new List<string> { "1920 x 1080", "1600 x 900", "1280 x 720" };
    private List<string> frameRateOptions = new List<string> { "60 FPS", "120 FPS", "Unlimited" };

    private void OnEnable()
    {
        if (SettingsManager.Instance == null || dropdown == null) return;
        
        dropdown.ClearOptions();

        var data = SettingsManager.Instance.Data;
        int currentIndex = 0;

        if (target == DropdownTarget.Resolution)
        {
            dropdown.AddOptions(resolutionOptions);
            string currentRes = $"{data.Video.ResolutionWidth} x {data.Video.ResolutionHeight}";
            currentIndex = resolutionOptions.FindIndex(o => o == currentRes);
        }
        else if (target == DropdownTarget.FrameRate)
        {
            dropdown.AddOptions(frameRateOptions);
            if (data.Video.TargetFrameRate == 60) currentIndex = 0;
            else if (data.Video.TargetFrameRate == 120) currentIndex = 1;
            else currentIndex = 2; // Unlimited (-1)
        }

        dropdown.value = (currentIndex >= 0) ? currentIndex : 0;
        
        dropdown.onValueChanged.RemoveListener(OnChanged);
        dropdown.onValueChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        if (dropdown != null) dropdown.onValueChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(int index)
    {
        var data = SettingsManager.Instance.Data;

        if (target == DropdownTarget.Resolution)
        {
            string[] res = dropdown.options[index].text.Split('x');
            data.Video.ResolutionWidth = int.Parse(res[0].Trim());
            data.Video.ResolutionHeight = int.Parse(res[1].Trim());
        }
        else if (target == DropdownTarget.FrameRate)
        {
            if (index == 0) data.Video.TargetFrameRate = 60;
            else if (index == 1) data.Video.TargetFrameRate = 120;
            else data.Video.TargetFrameRate = -1; // Unlimited
        }

        // [중요] 여기서 즉시 적용하지 않습니다. 해상도 변경은 Apply 버튼으로 제어됩니다.
    }
}
