using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ShopInteractable : MonoBehaviour
{
    [SerializeField] private float interactionDistance = 2.5f;

    [Header("Dialogos da Loja")]
    public DialogueData dialogoInventarioCheio;
    public DialogueData dialogoComprar;
    public DialogueData dialogoSairSemComprar;

    private bool _isPlayerNear;
    private GUIStyle _promptStyle;
    private const float PromptMaxWidth = 220f;

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

        ShopMenuController.Instance.dialogoInventarioCheio = dialogoInventarioCheio;
        ShopMenuController.Instance.dialogoComprar = dialogoComprar;
        ShopMenuController.Instance.dialogoSairSemComprar = dialogoSairSemComprar;

        ShopMenuController.Instance.Show();
    }

    private void OnGUI()
    {
        if (!_isPlayerNear || Time.timeScale <= 0f || (ShopMenuController.Instance != null && ShopMenuController.Instance.IsVisible))
        {
            return;
        }

        EnsureStyle();
        string text = MovementKeyBindings.GetDisplayName(MovementKeyBindings.GameAction.Interact) + " - Abrir Loja";
        GUIContent content = new GUIContent(text);
        float width = Mathf.Min(PromptMaxWidth, Mathf.Max(120f, _promptStyle.CalcSize(content).x));
        float height = _promptStyle.CalcHeight(content, width);
        
        Vector2 screenPos;
        GameObject targetObj = GameObject.Find("leather (1)");
        if (targetObj != null && Camera.main != null)
        {
            Vector3 worldPos = targetObj.transform.position;
            Vector3 sp = Camera.main.WorldToScreenPoint(worldPos);
            
            // If the object is behind the camera, don't draw it
            if (sp.z < 0) return;
            
            // GUI coordinates have Y=0 at the top, but ScreenPoint has Y=0 at the bottom.
            screenPos = new Vector2(sp.x - width / 2f, Screen.height - sp.y - height / 2f);
        }
        else
        {
            // Fallback to center screen
            screenPos = new Vector2(Screen.width / 2f - width / 2f, Screen.height / 2f + 50f);
        }

        Rect rect = new Rect(screenPos.x, screenPos.y, width, height);
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
        _promptStyle.wordWrap = true;
    }

    private static bool GetInteractPressed()
    {
#if ENABLE_INPUT_SYSTEM
        MovementKeyBindings.EnsureLoaded();
        return MovementKeyBindings.WasPressedThisFrame(MovementKeyBindings.GameAction.Interact);
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
