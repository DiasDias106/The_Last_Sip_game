using UnityEngine;
using UnityEngine.UI;
using System.Collections;
public class DialogueUIManager : MonoBehaviour
{
    public static DialogueUIManager Instance { get; private set; }
    private Canvas _canvas;
    private Text _dialogueText;
    private GameObject _panel;
    private AudioSource _audioSource;
    private Coroutine _dialogueRoutine;
    public float typingSpeed = 0.03f;
    public float delayAfterLine = 3.0f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoCreate()
    {
        if (FindObjectOfType<DialogueUIManager>() == null)
        {
            GameObject go = new GameObject("DialogueUIManager_Auto");
            go.AddComponent<DialogueUIManager>();
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
        _audioSource = gameObject.AddComponent<AudioSource>();
    }
    private void Update()
    {
        if (_dialogueRoutine != null && _panel != null)
        {
            bool subtitlesEnabled = PlayerPrefs.GetInt("MainMenu.Subtitles", 0) == 0;
            if (_panel.activeSelf != subtitlesEnabled)
            {
                _panel.SetActive(subtitlesEnabled);
            }
        }
    }
    private void EnsureUI()
    {
        if (_canvas != null) return;
        GameObject canvasGo = new GameObject("DialogueCanvas");
        canvasGo.transform.SetParent(transform);
        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 100;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();
        _panel = new GameObject("DialoguePanel");
        _panel.transform.SetParent(canvasGo.transform, false);
        Image bg = _panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        RectTransform panelRect = _panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.05f);
        panelRect.anchorMax = new Vector2(0.8f, 0.15f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        GameObject textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(_panel.transform, false);
        _dialogueText = textGo.AddComponent<Text>();
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (defaultFont == null) 
        {
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        _dialogueText.font = defaultFont;
        _dialogueText.color = Color.white;
        _dialogueText.alignment = TextAnchor.MiddleCenter;
        _dialogueText.resizeTextForBestFit = true;
        _dialogueText.resizeTextMinSize = 18;
        _dialogueText.resizeTextMaxSize = 40;
        RectTransform textRect = _dialogueText.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.1f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        _panel.SetActive(false);
    }
    public void StartDialogue(DialogueData dialogueData)
    {
        EnsureUI();
        if (dialogueData == null || dialogueData.lines == null || dialogueData.lines.Length == 0) 
        {
            Debug.LogWarning("[DialogueUIManager] Tentativa de iniciar diálogo vazio.");
            return;
        }
        if (_dialogueRoutine != null) StopCoroutine(_dialogueRoutine);
        _dialogueRoutine = StartCoroutine(PlayDialogueRoutine(dialogueData));
    }
    private IEnumerator PlayDialogueRoutine(DialogueData data)
    {
        bool subtitlesEnabled = PlayerPrefs.GetInt("MainMenu.Subtitles", 0) == 0;
        _panel.SetActive(subtitlesEnabled);
        if (data.playRandomLine)
        {
            int randomIndex = Random.Range(0, data.lines.Length);
            if (data.lines[randomIndex].audioClip != null)
            {
                _audioSource.clip = data.lines[randomIndex].audioClip;
                _audioSource.Play();
            }
            yield return StartCoroutine(TypeLine(data.GetLine(randomIndex, LanguageManager.Instance.CurrentLanguage)));
            yield return new WaitForSecondsRealtime(delayAfterLine);
        }
        else
        {
            for (int i = 0; i < data.lines.Length; i++)
            {
                if (data.lines[i].audioClip != null)
                {
                    _audioSource.clip = data.lines[i].audioClip;
                    _audioSource.Play();
                }
                yield return StartCoroutine(TypeLine(data.GetLine(i, LanguageManager.Instance.CurrentLanguage)));
                yield return new WaitForSecondsRealtime(delayAfterLine);
            }
        }
        _panel.SetActive(false);
        _dialogueRoutine = null;
    }
    private IEnumerator TypeLine(string line)
    {
        _dialogueText.text = "";
        Debug.Log($"[DialogueUIManager] Escrevendo legenda: {line}");
        if (string.IsNullOrEmpty(line)) yield break;
        foreach (char c in line.ToCharArray())
        {
            _dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }
    }
}
