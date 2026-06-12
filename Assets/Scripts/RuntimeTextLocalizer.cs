using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public sealed class RuntimeTextLocalizer : MonoBehaviour
{
    private static RuntimeTextLocalizer instance;
    private bool refreshQueued;
    private LanguageManager observedManager;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (instance != null)
        {
            return;
        }
        GameObject go = new GameObject("RuntimeTextLocalizer_Auto");
        instance = go.AddComponent<RuntimeTextLocalizer>();
        DontDestroyOnLoad(go);
    }
    public static void RequestRefresh()
    {
        if (instance == null)
        {
            AutoCreate();
        }
        if (instance != null)
        {
            instance.refreshQueued = true;
        }
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        refreshQueued = true;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnsubscribeFromLanguageManager();
    }
    private void Update()
    {
        EnsureLanguageSubscription();
        if (!refreshQueued)
        {
            return;
        }
        refreshQueued = false;
        ApplyLocalization();
    }
    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        refreshQueued = true;
    }
    private void HandleLanguageChanged()
    {
        refreshQueued = true;
    }
    private void EnsureLanguageSubscription()
    {
        if (observedManager == LanguageManager.Instance)
        {
            return;
        }
        UnsubscribeFromLanguageManager();
        observedManager = LanguageManager.Instance;
        if (observedManager != null)
        {
            observedManager.OnLanguageChanged += HandleLanguageChanged;
        }
    }
    private void UnsubscribeFromLanguageManager()
    {
        if (observedManager != null)
        {
            observedManager.OnLanguageChanged -= HandleLanguageChanged;
            observedManager = null;
        }
    }
    private static void ApplyLocalization()
    {
        TMP_Text[] tmpTexts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        for (int i = 0; i < tmpTexts.Length; i++)
        {
            TMP_Text text = tmpTexts[i];
            if (text == null)
            {
                continue;
            }
            text.text = UiText.LocalizeKnownText(text.text);
        }
        Text[] legacyTexts = FindObjectsByType<Text>(FindObjectsInactive.Include);
        for (int i = 0; i < legacyTexts.Length; i++)
        {
            Text text = legacyTexts[i];
            if (text == null)
            {
                continue;
            }
            text.text = UiText.LocalizeKnownText(text.text);
        }
    }
}
