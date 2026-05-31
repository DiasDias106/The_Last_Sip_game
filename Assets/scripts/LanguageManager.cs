using UnityEngine;
using System;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance { get; private set; }

    public enum Language { EN, PT }

    public Language CurrentLanguage { get; private set; }

    public Action OnLanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<LanguageManager>() == null)
        {
            GameObject go = new GameObject("LanguageManager_Auto");
            go.AddComponent<LanguageManager>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadLanguage();
    }

    private void LoadLanguage()
    {
        string langStr = PlayerPrefs.GetString("SelectedLanguage", "EN"); // Padrão é Inglês
        if (Enum.TryParse(langStr, out Language parsed))
        {
            CurrentLanguage = parsed;
        }
        else
        {
            CurrentLanguage = Language.EN;
        }
    }

    public void SetLanguage(Language newLanguage)
    {
        CurrentLanguage = newLanguage;
        PlayerPrefs.SetString("SelectedLanguage", CurrentLanguage.ToString());
        PlayerPrefs.Save();
        
        OnLanguageChanged?.Invoke();
    }
}
