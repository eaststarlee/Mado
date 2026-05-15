using System;
using UnityEngine;

[Serializable]
public class AudioSettingsData
{
    [Range(0f, 100f)] public float MasterVolume = 100f;
    [Range(0f, 100f)] public float MusicVolume = 100f;
    [Range(0f, 100f)] public float SFXVolume = 100f;
    [Range(0f, 100f)] public float VoiceVolume = 100f;
}

[Serializable]
public class VideoSettingsData
{
    public int ResolutionWidth = 1920;
    public int ResolutionHeight = 1080;
    public FullScreenMode FullScreenMode = FullScreenMode.FullScreenWindow;
    
    public bool VSync = true;
    public int TargetFrameRate = -1; 
    
    [Range(0f, 100f)] public float Brightness = 100f; 
}

[Serializable]
public class InputSettingsData
{
    public string KeyboardBindingOverrides = "";
    public string GamepadBindingOverrides = "";
}

[Serializable]
public class SettingsData
{
    public AudioSettingsData Audio = new AudioSettingsData();
    public VideoSettingsData Video = new VideoSettingsData();
    public InputSettingsData Input = new InputSettingsData();
}
