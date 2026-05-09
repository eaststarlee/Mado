using System;

[Serializable]
public class EntityState
{
    public bool active;
    public int state;
}

[Serializable]
public class SaveMetaData
{
    public int saveFormatVersion = 1;
    public string gameVersion = "1.0.0";
    public long lastSavedAt;
    public float totalPlayTime;
    public int slotIndex;
}
