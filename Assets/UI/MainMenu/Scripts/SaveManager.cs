using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const int SlotCount = 3;
    private const string ActiveSlotKey = "ActiveSaveSlot";
    private const string SessionSlotKey = "SaveSessionSlot";
    private const string SessionStartUtcTicksKey = "SaveSessionStartUtcTicks";

    public static bool HasSave(int slotIndex)
    {
        return LoadSlot(slotIndex).hasSave;
    }

    public static bool HasAnySave()
    {
        for (int i = 1; i <= SlotCount; i++)
        {
            if (HasSave(i))
            {
                return true;
            }
        }

        return false;
    }

    public static SaveSlotData LoadSlot(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        string path = GetSlotPath(clamped);
        if (!File.Exists(path))
        {
            return CreateEmptySlot(clamped);
        }

        try
        {
            string json = File.ReadAllText(path);
            SaveSlotData data = JsonUtility.FromJson<SaveSlotData>(json);
            if (data == null)
            {
                return CreateEmptySlot(clamped);
            }

            data.slotIndex = clamped;
            if (string.IsNullOrEmpty(data.displayName))
            {
                data.displayName = data.hasSave ? "Jogo Guardado" : "Slot Vazio";
            }

            if (string.IsNullOrEmpty(data.currentScene) && data.hasSave)
            {
                data.currentScene = "Beco";
            }

            if (data.screenshotPath == null)
            {
                data.screenshotPath = string.Empty;
            }

            return data;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Erro ao ler slot {clamped}: {ex.Message}");
            return CreateEmptySlot(clamped);
        }
    }

    public static void SaveSlot(SaveSlotData data)
    {
        if (data == null)
        {
            return;
        }

        data.slotIndex = ClampSlot(data.slotIndex);
        string path = GetSlotPath(data.slotIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Application.persistentDataPath);
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(path, json);
    }

    public static void DeleteSlot(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        string path = GetSlotPath(clamped);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    public static void CreateNewGame(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        SaveSlotData data = new SaveSlotData
        {
            slotIndex = clamped,
            hasSave = true,
            currentScene = "Beco",
            lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            playTime = 0f,
            displayName = "Novo Jogo",
            screenshotPath = string.Empty
        };

        SaveSlot(data);
    }

    public static void SetActiveSlot(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        PlayerPrefs.SetInt(ActiveSlotKey, clamped);
        PlayerPrefs.SetInt(SessionSlotKey, clamped);
        PlayerPrefs.SetString(SessionStartUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
    }

    public static int GetActiveSlot()
    {
        return ClampSlot(PlayerPrefs.GetInt(ActiveSlotKey, 1));
    }

    public static string GetNextSceneForSlot(int slotIndex)
    {
        SaveSlotData data = LoadSlot(slotIndex);
        if (!data.hasSave || string.IsNullOrWhiteSpace(data.currentScene))
        {
            return "Beco";
        }

        return data.currentScene;
    }

    public static string GetSlotScreenshotPath(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        return Path.Combine(Application.persistentDataPath, $"save_slot_{clamped}_preview.png");
    }

    public static void SaveActiveSlotFromCurrentScene(string currentScene, string screenshotPath = "")
    {
        int slotIndex = GetActiveSlot();
        SaveSlotData data = LoadSlot(slotIndex);
        if (!data.hasSave)
        {
            data.hasSave = true;
            data.displayName = "Jogo guardado";
        }

        data.slotIndex = slotIndex;
        data.currentScene = string.IsNullOrWhiteSpace(currentScene) ? "Beco" : currentScene;
        data.lastPlayedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        data.playTime = Mathf.Max(0f, data.playTime + ConsumeSessionElapsedSeconds(slotIndex));
        data.displayName = "Jogo guardado";
        data.screenshotPath = screenshotPath ?? string.Empty;
        SaveSlot(data);
    }

    public static float GetLivePlayTimeForSlot(int slotIndex)
    {
        int clamped = ClampSlot(slotIndex);
        SaveSlotData data = LoadSlot(clamped);
        float value = Mathf.Max(0f, data.playTime);

        int sessionSlot = PlayerPrefs.GetInt(SessionSlotKey, clamped);
        if (sessionSlot != clamped)
        {
            return value;
        }

        string ticksRaw = PlayerPrefs.GetString(SessionStartUtcTicksKey, string.Empty);
        if (!long.TryParse(ticksRaw, out long ticks))
        {
            return value;
        }

        DateTime startUtc;
        try
        {
            startUtc = new DateTime(ticks, DateTimeKind.Utc);
        }
        catch
        {
            return value;
        }

        float elapsed = Mathf.Max(0f, (float)(DateTime.UtcNow - startUtc).TotalSeconds);
        return value + elapsed;
    }

    private static int ClampSlot(int slotIndex)
    {
        return Mathf.Clamp(slotIndex, 1, SlotCount);
    }

    private static string GetSlotPath(int slotIndex)
    {
        return Path.Combine(Application.persistentDataPath, $"save_slot_{slotIndex}.json");
    }

    private static SaveSlotData CreateEmptySlot(int slotIndex)
    {
        return new SaveSlotData
        {
            slotIndex = slotIndex,
            hasSave = false,
            currentScene = string.Empty,
            lastPlayedDate = string.Empty,
            playTime = 0f,
            displayName = "Slot Vazio",
            screenshotPath = string.Empty
        };
    }

    private static float ConsumeSessionElapsedSeconds(int slotIndex)
    {
        int sessionSlot = PlayerPrefs.GetInt(SessionSlotKey, slotIndex);
        if (sessionSlot != slotIndex)
        {
            PlayerPrefs.SetInt(SessionSlotKey, slotIndex);
            PlayerPrefs.SetString(SessionStartUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            return 0f;
        }

        string ticksRaw = PlayerPrefs.GetString(SessionStartUtcTicksKey, string.Empty);
        if (!long.TryParse(ticksRaw, out long ticks))
        {
            PlayerPrefs.SetString(SessionStartUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
            PlayerPrefs.Save();
            return 0f;
        }

        float elapsed;
        try
        {
            DateTime startUtc = new DateTime(ticks, DateTimeKind.Utc);
            elapsed = Mathf.Max(0f, (float)(DateTime.UtcNow - startUtc).TotalSeconds);
        }
        catch
        {
            elapsed = 0f;
        }

        PlayerPrefs.SetString(SessionStartUtcTicksKey, DateTime.UtcNow.Ticks.ToString());
        PlayerPrefs.Save();
        return elapsed;
    }
}
