using System;
using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    private const string SelectedLanguageKey = "SelectedLanguage";
    private const string LegacyLanguageIndexKey = "MainMenu.Language";

    public static LanguageManager Instance { get; private set; }

    public enum Language { EN, PT }

    public Language CurrentLanguage { get; private set; }

    public Action OnLanguageChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (FindAnyObjectByType<LanguageManager>() == null)
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
        if (PlayerPrefs.HasKey(SelectedLanguageKey))
        {
            string langStr = PlayerPrefs.GetString(SelectedLanguageKey, "PT");
            if (Enum.TryParse(langStr, out Language parsed))
            {
                CurrentLanguage = parsed;
                return;
            }
        }

        int legacyLanguageIndex = PlayerPrefs.GetInt(LegacyLanguageIndexKey, 0);
        CurrentLanguage = legacyLanguageIndex <= 0 ? Language.PT : Language.EN;
    }

    public void SetLanguage(Language newLanguage)
    {
        CurrentLanguage = newLanguage;
        PlayerPrefs.SetString(SelectedLanguageKey, CurrentLanguage.ToString());
        PlayerPrefs.SetInt(LegacyLanguageIndexKey, CurrentLanguage == Language.PT ? 0 : 1);
        PlayerPrefs.Save();

        OnLanguageChanged?.Invoke();
    }
}
