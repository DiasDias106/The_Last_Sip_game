using System;
using System.IO;
using UnityEngine;

public static class SaveManager
{
    private const int SlotCount = 3;
    private const string ActiveSlotKey = "ActiveSaveSlot";

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
            displayName = "Novo Jogo"
        };

        SaveSlot(data);
    }

    public static void SetActiveSlot(int slotIndex)
    {
        PlayerPrefs.SetInt(ActiveSlotKey, ClampSlot(slotIndex));
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
            displayName = "Slot Vazio"
        };
    }
}

