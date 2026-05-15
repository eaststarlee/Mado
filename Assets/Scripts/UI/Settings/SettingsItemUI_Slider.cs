using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsItemUI_Slider : MonoBehaviour
{
    public enum SliderTarget { Master, Music, SFX, Voice, Brightness }
    [SerializeField] private SliderTarget target;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;

    private void OnEnable()
    {
        if (SettingsManager.Instance == null) return;

        var data = SettingsManager.Instance.Data;
        float val = target switch {
            SliderTarget.Master => data.Audio.MasterVolume,
            SliderTarget.Music => data.Audio.MusicVolume,
            SliderTarget.SFX => data.Audio.SFXVolume,
            SliderTarget.Voice => data.Audio.VoiceVolume,
            SliderTarget.Brightness => data.Video.Brightness,
            _ => 100f
        };
        slider.value = val;
        UpdateText(val);
        slider.onValueChanged.AddListener(OnChanged);
    }

    private void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(OnChanged);
    }

    private void OnChanged(float val)
    {
        var data = SettingsManager.Instance.Data;
        switch (target) {
            case SliderTarget.Master: data.Audio.MasterVolume = val; SettingsManager.Instance.ApplyAudioSettings(); break;
            case SliderTarget.Music: data.Audio.MusicVolume = val; SettingsManager.Instance.ApplyAudioSettings(); break;
            case SliderTarget.SFX: data.Audio.SFXVolume = val; SettingsManager.Instance.ApplyAudioSettings(); break;
            case SliderTarget.Voice: data.Audio.VoiceVolume = val; SettingsManager.Instance.ApplyAudioSettings(); break;
            case SliderTarget.Brightness: data.Video.Brightness = val; SettingsManager.Instance.ApplyBrightness(); break;
        }
        UpdateText(val);
    }

    private void UpdateText(float val)
    {
        if (valueText != null) valueText.text = Mathf.RoundToInt(val).ToString();
    }
}
