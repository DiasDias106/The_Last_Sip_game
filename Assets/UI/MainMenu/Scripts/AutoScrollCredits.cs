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
        
        // Se o conteúdo não for maior que a viewport, não podemos rolar!
        // Ignoramos o auto scroll nestes casos.
        if (scrollRect.content.rect.height <= scrollRect.viewport.rect.height) return;

        // Se o utilizador arrastar manualmente com velocidade, pausamos
        if (Mathf.Abs(scrollRect.velocity.y) > 5f)
        {
            timer = -2f; // Pausa 2 segundos
            return;
        }

        if (timer < startDelay)
        {
            timer += Time.unscaledDeltaTime;
            return;
        }

        // Rolar suavemente (mover para cima)
        scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.unscaledDeltaTime;
        
        if (scrollRect.verticalNormalizedPosition < 0f)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
