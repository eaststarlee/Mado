using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public SettingsData Data { get; private set; }
    
    [Header("Engine References")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Image brightnessOverlay; 

    private string saveFilePath;
    public event Action OnSettingsChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            saveFilePath = Path.Combine(Application.persistentDataPath, "GlobalSettings.json");
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start() => ApplyAllSettings();

    public void LoadSettings()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                Data = JsonUtility.FromJson<SettingsData>(json);
            }
            catch (Exception)
            {
                Data = new SettingsData(); 
            }
        }
        else
        {
            Data = new SettingsData();
        }
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] Save Failed: {e.Message}");
        }
    }

    public void ApplyAllSettings()
    {
        ApplyAudioSettings();
        ApplyVideoSettings();
        InputRebindSystem.LoadOverridesFromSettings();
        OnSettingsChanged?.Invoke();
    }

    public void ApplyAudioSettings()
    {
        if (mainAudioMixer == null) return;
        SetMixerVolume("MasterVolume", Data.Audio.MasterVolume);
        SetMixerVolume("MusicVolume", Data.Audio.MusicVolume);
        SetMixerVolume("SFXVolume", Data.Audio.SFXVolume);
        SetMixerVolume("VoiceVolume", Data.Audio.VoiceVolume);
    }

    private void SetMixerVolume(string parameterName, float volume)
    {
        float dB = volume > 0.01f ? Mathf.Log10(volume / 100f) * 20f : -80f;
        mainAudioMixer.SetFloat(parameterName, dB);
    }

    public void ApplyVideoSettings()
    {
        Screen.SetResolution(Data.Video.ResolutionWidth, Data.Video.ResolutionHeight, Data.Video.FullScreenMode);
        QualitySettings.vSyncCount = Data.Video.VSync ? 1 : 0;
        Application.targetFrameRate = Data.Video.VSync ? -1 : Data.Video.TargetFrameRate;
        ApplyBrightness();
    }

    public void ApplyBrightness()
    {
        if (brightnessOverlay != null)
        {
            Color c = brightnessOverlay.color;
            // 밝기가 높을수록(100) 오버레이의 알파값(검은색)은 0에 가까워집니다.
            c.a = Mathf.Lerp(0.9f, 0f, Data.Video.Brightness / 100f);
            brightnessOverlay.color = c;
        }
    }

    public void ResetAudioDefaults()
    {
        Data.Audio = new AudioSettingsData();
        ApplyAudioSettings();
        SaveSettings();
    }

    public void ResetVideoDefaults()
    {
        Data.Video = new VideoSettingsData();
        ApplyVideoSettings();
        SaveSettings();
    }

    public void ResetInputDefaults()
    {
        if (InputRouter.Instance != null && InputRouter.Instance.Actions != null)
        {
            InputRouter.Instance.Actions.asset.RemoveAllBindingOverrides();
        }
        Data.Input = new InputSettingsData();
        SaveSettings();
    }
}
