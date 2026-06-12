using UnityEngine;
using UnityEngine.UI;
public class AutoScrollCredits : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.065f;
    [SerializeField] private float startDelay = 1.0f;
    private float timer = 0f;
    private void Start()
    {
        if (scrollRect == null)
            scrollRect = GetComponent<ScrollRect>();
        ResetScroll();
    }
    private void OnEnable()
    {
        ResetScroll();
    }
    public void ResetScroll()
    {
        timer = 0f;
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
            scrollRect.velocity = Vector2.zero;
        }
    }
    private void Update()
    {
        if (scrollRect == null || scrollRect.content == null) return;
        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height) return;
        if (Mathf.Abs(scrollRect.velocity.y) > 5f)
        {
            timer = -2f;
            return;
        }
        if (timer < startDelay)
        {
            timer += Time.unscaledDeltaTime;
            return;
        }
        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.unscaledDeltaTime;
        if (scrollRect.verticalNormalizedPosition < 0f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
