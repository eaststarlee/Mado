using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 비디오 탭의 토글(전체화면, 수직동기화)을 제어하는 UI 컴포넌트.
/// 상용 게임 스탠다드에 맞춰 데이터만 갱신하며, 실제 화면 반영은 Apply 버튼을 누를 때 발생합니다.
/// </summary>
public class SettingsItemUI_Toggle : MonoBehaviour
{
    public enum ToggleTarget { Fullscreen, VSync }
    [SerializeField] private ToggleTarget target;
    [SerializeField] private Toggle toggle;

    private void OnEnable()
    {
        if (SettingsManager.Instance == null || toggle == null) return;
        var data = SettingsManager.Instance.Data;

        // 저장된 설정 데이터로 UI 초기화
        toggle.isOn = target switch {
            ToggleTarget.Fullscreen => data.Video.FullScreenMode == FullScreenMode.FullScreenWindow,
            ToggleTarget.VSync => data.Video.VSync,
            _ => true
        };
        
        // 이벤트 중복 등록 방지
        toggle.onValueChanged.RemoveListener(OnChanged);
        toggle.onValueChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(bool val)
    {
        var data = SettingsManager.Instance.Data;
        
        if (target == ToggleTarget.Fullscreen) 
        {
            data.Video.FullScreenMode = val ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        else if (target == ToggleTarget.VSync)
        {
            data.Video.VSync = val;
        }

        // [중요] 여기서 ApplyVideoSettings()를 즉시 호출하지 않습니다.
        // 해상도와 창 모드는 화면 깜빡임을 유발하므로 사용자가 명시적으로 'Apply' 버튼을 눌렀을 때만 반영합니다.
    }
}
