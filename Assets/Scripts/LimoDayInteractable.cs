using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LimoDayInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 0.8f;

    [Header("Sequence")]
    [SerializeField] private float driveSpeed = 9f;
    [SerializeField] private float preDriveDelay = 0.5f;
    [SerializeField] private float cameraPitch = 8f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private Vector3 enterApproachOffset = new Vector3(-1.5f, 0f, -0.35f);
    [SerializeField] private float maxEnterWalkTime = 2.25f;

    private bool _sequenceStarted;
    private GameObject _player;

    public bool CanInteract => !_sequenceStarted && IsPlayerInRange();

    public void Interact()
    {
        if (_sequenceStarted || !IsPlayerInRange())
        {
            return;
        }

        UserImportedPlayerController controller = _player.GetComponent<UserImportedPlayerController>();
        StartSequence(controller);
    }

    public bool StartSequence(UserImportedPlayerController controller)
    {
        if (_sequenceStarted || controller == null)
        {
            return false;
        }

        _player = controller.gameObject;
        _sequenceStarted = true;
        StartCoroutine(DriveToCredits(controller));
        return true;
    }

    private IEnumerator DriveToCredits(UserImportedPlayerController controller)
    {
        controller.SetExternalControlLocked(true);

        // Disable CC immediately so physics doesn't fight the scripted walk
        CharacterController cc = controller.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // Phase 1: player walks to limo — camera follows player normally (no override, no shake)
        yield return MovePlayerToLimo(controller);

        controller.SetVisualVisible(false);

        yield return new WaitForSeconds(preDriveDelay);

        // Phase 2: fixed side-rear camera watching limo drive away
        GameObject cameraAnchor = new GameObject("LimoDayCameraAnchor");
        cameraAnchor.transform.position = transform.position + new Vector3(-8f, 3f, 0f);
        cameraAnchor.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
        controller.SetCameraFollowOverride(cameraAnchor.transform, true, cameraPitch);

        // Fade overlay
        Canvas fadeCanvas = new GameObject("LimoDayFade").AddComponent<Canvas>();
        fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        fadeCanvas.sortingOrder = 999;
        fadeCanvas.gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
        UnityEngine.UI.Image fadeImg = new GameObject("Img").AddComponent<UnityEngine.UI.Image>();
        fadeImg.transform.SetParent(fadeCanvas.transform, false);
        fadeImg.rectTransform.anchorMin = Vector2.zero;
        fadeImg.rectTransform.anchorMax = Vector2.one;
        fadeImg.rectTransform.offsetMin = Vector2.zero;
        fadeImg.rectTransform.offsetMax = Vector2.zero;
        fadeImg.color = Color.clear;
        fadeImg.raycastTarget = false;

        float destinationX = transform.position.x + 50f;
        float driveTimer = 0f;
        float fadeStartAfter = 2.5f;
        float fadeDuration = 2f;
        float fadeAlpha = 0f;

        while (Mathf.Abs(transform.position.x - destinationX) > 0.05f && fadeAlpha < 1f)
        {
            Vector3 limoPosition = transform.position;
            limoPosition.x = Mathf.MoveTowards(limoPosition.x, destinationX, driveSpeed * Time.deltaTime);
            transform.position = limoPosition;

            driveTimer += Time.deltaTime;

            if (driveTimer >= fadeStartAfter)
            {
                fadeAlpha = Mathf.Clamp01(fadeAlpha + Time.deltaTime / fadeDuration);
                fadeImg.color = new Color(0f, 0f, 0f, fadeAlpha);
            }

            yield return null;
        }

        fadeImg.color = Color.black;
        yield return new WaitForSeconds(0.4f);

        controller.ClearCameraFollowOverride();
        UnityEngine.Object.Destroy(cameraAnchor);
        UnityEngine.Object.Destroy(fadeCanvas.gameObject);
        GlobalSceneTransitionRequest.PendingOpenCredits = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator MovePlayerToLimo(UserImportedPlayerController controller)
    {
        Vector3 approachPoint = GetApproachPoint();
        float walkSpeed = controller.CurrentWalkSpeed;
        float moveTimer = 0f;

        controller.PlayExternalAnimation("walk");
        while (Vector3.Distance(controller.transform.position, approachPoint) > 0.08f)
        {
            Vector3 direction = approachPoint - controller.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                controller.FaceDirection(direction);
                Vector3 step = direction.normalized * walkSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                    step = direction;
                controller.transform.position += step;
            }

            moveTimer += Time.deltaTime;
            if (moveTimer >= maxEnterWalkTime)
                break;

            yield return null;
        }

        controller.PlayExternalAnimation("idle");
    }

    private Vector3 GetApproachPoint()
    {
        Vector3 right = GetLimoRight();
        Vector3 forward = Vector3.right;
        return transform.position + right * enterApproachOffset.x + Vector3.up * enterApproachOffset.y + forward * enterApproachOffset.z;
    }

    private Vector3 GetLimoRight()
    {
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.001f)
        {
            right = Vector3.right;
        }

        return right;
    }

    private bool IsPlayerInRange()
    {
        if (_player == null)
        {
            _player = GameObject.FindGameObjectWithTag("Player");
        }

        if (_player == null)
        {
            return false;
        }

        Vector3 playerPosition = _player.transform.position;
        Vector3 targetPosition = GetApproachPoint();
        playerPosition.y = 0f;
        targetPosition.y = 0f;
        return Vector3.Distance(playerPosition, targetPosition) <= interactionDistance;
    }
}
