using System;

[Serializable]
public sealed class SaveSlotData
{
    public int slotIndex;
    public bool hasSave;
    public string currentScene;
    public string lastPlayedDate;
    public float playTime;
    public string displayName;
}

