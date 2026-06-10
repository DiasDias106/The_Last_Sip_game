using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LimoDayInteractable : MonoBehaviour
{
    private const string SkipSplashKey = "__SKIP_SPLASH__";

    [Header("Auto Caminhar")]
    [SerializeField] private float autoWalkSpeed = 2.5f;
    [SerializeField] private float doorStopDistance = 0.35f;
    [SerializeField] private float maxAutoWalkTime = 12f;

    [Header("Sequência")]
    [SerializeField] private float entryHoldTime = 1.0f;
    [SerializeField] private float driveSpeed = 9f;
    [SerializeField] private float cameraPitch = 8f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

    [Header("Credits — Logo")]
    [SerializeField] private Sprite logoSprite;
    [SerializeField] private float logoFadeIn = 1.2f;
    [SerializeField] private float logoHoldTime = 2.5f;
    [SerializeField] private float logoFadeOut = 1f;

    [Header("Credits — Música")]
    [SerializeField] private AudioClip creditsMusic;
    [SerializeField] private float creditsMusicVolume = 0.75f;

    [Header("Credits — Texto")]
    [SerializeField] private float creditsScrollSpeed = 55f;
    [SerializeField, TextArea(6, 20)] private string creditsText =
        "THE LAST SIP\n\n\n" +
        "Desenvolvido por\n" +
        "Francisco Dias\n\n\n" +
        "Orientação\n" +
        "Professor\n\n\n" +
        "Agradecimentos\n" +
        "...\n\n\n\n\n\n";

    // Zona de ativacao: player chega ao calcamento (sul da limo)
    [Header("Zona de Ativação")]
    [SerializeField] private float triggerZMin = 25f;
    [SerializeField] private float triggerZMax = 30f;

    private bool _sequenceStarted;
    private GameObject _player;

    private void Update()
    {
        if (_sequenceStarted) return;
        UserImportedPlayerController ctrl = GetController();
        if (ctrl == null) return;
        float pz = ctrl.transform.position.z;
        if (pz >= triggerZMin && pz <= triggerZMax)
            TryBeginAutoWalk();
    }

    public void TryBeginAutoWalk()
    {
        if (_sequenceStarted) return;
        UserImportedPlayerController ctrl = GetController();
        if (ctrl == null) return;
        _sequenceStarted = true;
        StartCoroutine(AutoWalkAndPlay(ctrl));
    }

    private UserImportedPlayerController GetController()
    {
        if (_player == null) _player = GameObject.FindGameObjectWithTag("Player");
        return _player != null ? _player.GetComponent<UserImportedPlayerController>() : null;
    }

    // Player caminha em frente (Z), para a face da limo mais proxima
    private Vector3 GetEntryPoint(Vector3 playerPos)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            Bounds b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            float targetZ = playerPos.z < b.center.z ? b.min.z : b.max.z;
            return new Vector3(playerPos.x, playerPos.y, targetZ);
        }
        return new Vector3(playerPos.x, playerPos.y, transform.position.z);
    }

    private IEnumerator AutoWalkAndPlay(UserImportedPlayerController controller)
    {
        controller.SetExternalControlLocked(true);
        controller.PlayExternalAnimation("walk");

        Vector3 target = GetEntryPoint(controller.transform.position);

        float walkTimer = 0f;
        float stuckCheckTimer = 0f;
        Vector3 lastCheckPos = controller.transform.position;
        bool pastInitialDelay = false;

        while (walkTimer < maxAutoWalkTime)
        {
            Vector3 dir = target - controller.transform.position;
            dir.y = 0f;
            if (dir.magnitude <= doorStopDistance) break;

            // Detetar se o player ficou preso (lancil ou MeshCollider da limo)
            stuckCheckTimer += Time.deltaTime;
            if (stuckCheckTimer >= 0.4f)
            {
                if (pastInitialDelay)
                {
                    float moved = new Vector2(
                        controller.transform.position.x - lastCheckPos.x,
                        controller.transform.position.z - lastCheckPos.z).magnitude;
                    if (moved < 0.05f) break;
                }
                lastCheckPos = controller.transform.position;
                stuckCheckTimer = 0f;
                pastInitialDelay = true;
            }

            controller.FaceDirection(dir);
            controller.MoveExternally(dir.normalized * autoWalkSpeed);
            walkTimer += Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(PlaySequence(controller));
    }

    private IEnumerator PlaySequence(UserImportedPlayerController controller)
    {
        CharacterController cc = controller.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Snap do player para junto da face da limo antes de esconder
        Vector3 entryPoint = GetEntryPoint(controller.transform.position);
        Vector3 snapPos = controller.transform.position;
        snapPos.z = entryPoint.z + (controller.transform.position.z > entryPoint.z ? 0.15f : -0.15f);
        controller.transform.position = snapPos;

        controller.SetVisualVisible(false);
        controller.PlayExternalAnimation("idle");

        yield return new WaitForSeconds(entryHoldTime);

        GameObject anchor = new GameObject("LimoDayCameraAnchor");
        anchor.transform.position = transform.position + new Vector3(-8f, 3f, 0f);
        anchor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        controller.SetCameraFollowOverride(anchor.transform, true, cameraPitch);

        Canvas screenCanvas = new GameObject("LimoDayScreen").AddComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 999;
        screenCanvas.gameObject.AddComponent<CanvasScaler>();
        DontDestroyOnLoad(screenCanvas.gameObject);

        Image bg = new GameObject("Bg").AddComponent<Image>();
        bg.transform.SetParent(screenCanvas.transform, false);
        FillRect(bg.rectTransform);
        bg.color = Color.clear;
        bg.raycastTarget = false;

        float destX = transform.position.x + 50f;
        float driveTimer = 0f;
        float fadeAlpha = 0f;

        while (fadeAlpha < 1f)
        {
            if (Mathf.Abs(transform.position.x - destX) > 0.05f)
                transform.position += Vector3.right * driveSpeed * Time.deltaTime;

            driveTimer += Time.deltaTime;
            if (driveTimer >= 2.5f)
            {
                fadeAlpha = Mathf.Clamp01(fadeAlpha + Time.deltaTime / 2f);
                bg.color = new Color(0f, 0f, 0f, fadeAlpha);
            }

            yield return null;
        }

        bg.color = Color.black;
        controller.ClearCameraFollowOverride();
        Destroy(anchor);

        RuntimeSceneTransitionOverlay.ShowBlackForSceneLoad();
        Destroy(screenCanvas.gameObject);
        PlayerPrefs.SetInt(SkipSplashKey, 1);
        PlayerPrefs.Save();
        GlobalSceneTransitionRequest.PendingOpenCredits = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private static void FillRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
