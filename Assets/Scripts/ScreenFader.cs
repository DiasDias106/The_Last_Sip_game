using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    private Image _overlay;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
    }
    void BuildOverlay()
    {
        var canvasGo = new GameObject("ScreenFadeCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();
        var imgGo = new GameObject("FadeOverlay");
        imgGo.transform.SetParent(canvasGo.transform, false);
        _overlay = imgGo.AddComponent<Image>();
        _overlay.color = new Color(0f, 0f, 0f, 0f);
        _overlay.raycastTarget = false;
        var rt = imgGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    public static ScreenFader GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("ScreenFader");
        return go.AddComponent<ScreenFader>();
    }
    public IEnumerator FadeOut(float duration = 0.5f)
    {
        yield return Fade(0f, 1f, duration);
    }
    public IEnumerator FadeIn(float duration = 0.5f)
    {
        yield return Fade(1f, 0f, duration);
    }
    IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / duration);
            _overlay.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        _overlay.color = new Color(0f, 0f, 0f, to);
    }
}
