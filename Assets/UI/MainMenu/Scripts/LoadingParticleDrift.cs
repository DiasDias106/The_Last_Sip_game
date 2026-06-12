using UnityEngine;
public sealed class LoadingParticleDrift : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private float horizontalAmplitude = 18f;
    [SerializeField] private float verticalAmplitude = 28f;
    [SerializeField] private float speed = 0.32f;
    [SerializeField] private float alphaMin = 0.12f;
    [SerializeField] private float alphaMax = 0.42f;
    [SerializeField] private float phaseOffset;
    private Vector2 basePosition;
    private CanvasGroup canvasGroup;
    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        if (target != null)
        {
            basePosition = target.anchoredPosition;
        }
    }
    private void Update()
    {
        if (target == null)
        {
            return;
        }
        float t = Time.unscaledTime * speed + phaseOffset;
        float driftX = Mathf.Sin(t * 0.9f) * horizontalAmplitude;
        float driftY = Mathf.Cos(t * 0.65f) * verticalAmplitude;
        target.anchoredPosition = basePosition + new Vector2(driftX, driftY);
        if (canvasGroup != null)
        {
            float alphaT = 0.5f + 0.5f * Mathf.Sin(t * 1.4f);
            canvasGroup.alpha = Mathf.Lerp(alphaMin, alphaMax, alphaT);
        }
    }
}
