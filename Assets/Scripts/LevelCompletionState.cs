public static class LevelCompletionState
{
    private const string Prefix = "__LEVEL_COMPLETED__";
    private const int DefaultSlot = 1;
    public static string GetKey(string sceneName)
    {
        return GetKey(sceneName, GetActiveSlotSafe());
    }
    public static string GetKey(string sceneName, int slotIndex)
    {
        int slot = slotIndex <= 0 ? DefaultSlot : slotIndex;
        return Prefix + "_S" + slot + "_" + sceneName;
    }
    public static bool IsCompleted(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }
        int slot = GetActiveSlotSafe();
        string slotKey = GetKey(sceneName, slot);
        return UnityEngine.PlayerPrefs.GetInt(slotKey, 0) == 1;
    }
    public static void SetCompleted(string sceneName, bool completed)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }
        string slotKey = GetKey(sceneName, GetActiveSlotSafe());
        UnityEngine.PlayerPrefs.SetInt(slotKey, completed ? 1 : 0);
    }
    public static void ClearForSlot(int slotIndex)
    {
        int slot = slotIndex <= 0 ? DefaultSlot : slotIndex;
        string[] knownScenes =
        {
            "level_1",
            "level_2",
            "level_3",
            "boss_room"
        };
        for (int i = 0; i < knownScenes.Length; i++)
        {
            UnityEngine.PlayerPrefs.DeleteKey(GetKey(knownScenes[i], slot));
        }
    }
    public static void ClearLegacyGlobal()
    {
        string[] knownScenes =
        {
            "level_1",
            "level_2",
            "level_3",
            "boss_room"
        };
        for (int i = 0; i < knownScenes.Length; i++)
        {
            UnityEngine.PlayerPrefs.DeleteKey(Prefix + knownScenes[i]);
        }
    }
    private static int GetActiveSlotSafe()
    {
        try
        {
            return SaveManager.GetActiveSlot();
        }
        catch
        {
            return DefaultSlot;
        }
    }
}
