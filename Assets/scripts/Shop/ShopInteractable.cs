using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShopInteractable : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2.5f;

    private bool _isPlayerNear;
    private GUIStyle _promptStyle;

    private void Update()
    {
        if (Time.timeScale <= 0f) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float distSq = (transform.position - player.transform.position).sqrMagnitude;
        _isPlayerNear = distSq <= interactionDistance * interactionDistance;

        if (_isPlayerNear && GetInteractPressed())
        {
            OpenShop();
        }
    }

    private void OpenShop()
    {
        if (ShopMenuController.Instance == null)
        {
            var go = new GameObject("ShopMenuController");
            go.AddComponent<ShopMenuController>();
        }

        ShopMenuController.Instance.Show();
    }

    private void OnGUI()
    {
        if (!_isPlayerNear || Time.timeScale <= 0f || (ShopMenuController.Instance != null && ShopMenuController.Instance.IsVisible))
        {
            return;
        }

        EnsureStyle();
        const string text = "E - Abrir Loja";
        Vector2 size = _promptStyle.CalcSize(new GUIContent(text));
        
        Vector2 screenPos;
        GameObject targetObj = GameObject.Find("leather (1)");
        if (targetObj != null && Camera.main != null)
        {
            Vector3 worldPos = targetObj.transform.position;
            Vector3 sp = Camera.main.WorldToScreenPoint(worldPos);
            
            // If the object is behind the camera, don't draw it
            if (sp.z < 0) return;
            
            // GUI coordinates have Y=0 at the top, but ScreenPoint has Y=0 at the bottom.
            screenPos = new Vector2(sp.x - size.x / 2f, Screen.height - sp.y - size.y / 2f);
        }
        else
        {
            // Fallback to center screen
            screenPos = new Vector2(Screen.width / 2f - size.x / 2f, Screen.height / 2f + 50f);
        }

        Rect rect = new Rect(screenPos.x, screenPos.y, size.x, size.y);
        GUI.Label(rect, text, _promptStyle);
    }

    private void EnsureStyle()
    {
        if (_promptStyle != null) return;

        _promptStyle = new GUIStyle(GUI.skin.label);
        _promptStyle.fontSize = 24;
        _promptStyle.fontStyle = FontStyle.Bold;
        _promptStyle.normal.textColor = Color.white;
        _promptStyle.alignment = TextAnchor.MiddleCenter;
    }

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        return kb != null && kb.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }
}
