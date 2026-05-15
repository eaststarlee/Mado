using UnityEngine;
using System;
using System.IO;

[Serializable] 
public class AudioSettings {
    public float masterVolume = 1f;
    public float bgmVolume = 1f;
    public float sfxVolume = 1f;
}

[Serializable] 
public class VideoSettings {
    public int resolutionIndex = 0;
    public bool fullscreen = true;
    public bool vsync = true;
    public int targetFPS = 60;
}

[Serializable]
public class GameSettingsData {
    public AudioSettings Audio = new AudioSettings();
    public VideoSettings Video = new VideoSettings();
}

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public GameSettingsData CurrentSettings { get; private set; }

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Path.Combine(Application.persistentDataPath, "settings.json");
        LoadSettings();
    }

    public void LoadSettings()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                CurrentSettings = JsonUtility.FromJson<GameSettingsData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsManager] 설정 로드 실패: {e.Message}");
                CurrentSettings = new GameSettingsData();
            }
        }
        else
        {
            CurrentSettings = new GameSettingsData();
        }

        ApplySettings();
    }

    public void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(CurrentSettings, true);
            File.WriteAllText(savePath, json);
            Debug.Log("[SettingsManager] 설정 저장 완료.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SettingsManager] 설정 저장 실패: {e.Message}");
        }
    }

    public void ApplySettings()
    {
        // 비디오 적용
        Application.targetFrameRate = CurrentSettings.Video.targetFPS;
        QualitySettings.vSyncCount = CurrentSettings.Video.vsync ? 1 : 0;
        Screen.fullScreen = CurrentSettings.Video.fullscreen;
        // 해상도 로직은 해상도 목록 관리 후 추가 적용 가능

        // 오디오는 AudioMixer와 연동 (추후 구현)
    }
}
