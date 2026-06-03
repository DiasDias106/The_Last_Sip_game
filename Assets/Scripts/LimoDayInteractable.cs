using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class LimoDayInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private float interactionDistance = 3f;

    [Header("Sequence")]
    [SerializeField] private Transform playerSeatPoint;
    [SerializeField] private Transform driveTarget;
    [SerializeField] private float playerWalkSpeed = 3.5f;
    [SerializeField] private float driveSpeed = 9f;
    [SerializeField] private float seatEnterSpeed = 2.2f;
    [SerializeField] private float enterDelay = 0.2f;
    [SerializeField] private float preDriveDelay = 0.35f;
    [SerializeField] private float maxDriveTime = 4f;
    [SerializeField] private float cameraPitch = 8f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private Vector3 enterApproachOffset = new Vector3(-1.5f, 0f, -0.35f);
    [SerializeField] private Vector3 seatOffset = new Vector3(0f, 0.9f, 0.1f);
    [SerializeField] private Vector3 cameraAnchorOffset = new Vector3(-2.5f, 2.4f, -10.5f);
    [SerializeField] private Vector3 cameraLookOffset = new Vector3(0f, 1.2f, 0f);
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
        GameObject cameraAnchor = CreateCameraAnchor();
        controller.SetCameraFollowOverride(cameraAnchor.transform, true, cameraPitch, true);
        yield return MovePlayerToLimo(controller);

        controller.SetVisualVisible(false);
        yield return new WaitForSeconds(preDriveDelay);

        Vector3 driveDirection = Vector3.right;
        float destinationX = transform.position.x + 32f;
        float driveTimer = 0f;

        while (Mathf.Abs(transform.position.x - destinationX) > 0.05f)
        {
            Vector3 limoPosition = transform.position;
            limoPosition.x = Mathf.MoveTowards(limoPosition.x, destinationX, driveSpeed * Time.deltaTime);
            transform.position = limoPosition;
            UpdateCameraAnchor(cameraAnchor.transform, driveDirection);

            driveTimer += Time.deltaTime;
            if (driveTimer >= maxDriveTime)
            {
                break;
            }

            yield return null;
        }

        controller.ClearCameraFollowOverride();
        Destroy(cameraAnchor);
        GlobalSceneTransitionRequest.PendingOpenCredits = true;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private IEnumerator MovePlayerToLimo(UserImportedPlayerController controller)
    {
        Vector3 approachPoint = GetApproachPoint();
        Vector3 seatPoint = GetSeatPoint();
        float moveTimer = 0f;

        controller.PlayExternalAnimation("walk");
        while (Vector3.Distance(controller.transform.position, approachPoint) > 0.08f)
        {
            Vector3 direction = approachPoint - controller.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                controller.FaceDirection(direction);
                Vector3 step = direction.normalized * playerWalkSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                {
                    step = direction;
                }

                controller.transform.position += step;
            }

            moveTimer += Time.deltaTime;
            if (moveTimer >= maxEnterWalkTime)
            {
                break;
            }

            yield return null;
        }

        Vector3 enterDirection = seatPoint - controller.transform.position;
        enterDirection.y = 0f;
        if (enterDirection.sqrMagnitude > 0.0001f)
        {
            controller.FaceDirection(enterDirection);
        }

        controller.PlayExternalAnimation("idle");
        yield return new WaitForSeconds(enterDelay);

        moveTimer = 0f;
        controller.PlayExternalAnimation("walk");
        while (Vector3.Distance(controller.transform.position, seatPoint) > 0.05f)
        {
            Vector3 direction = seatPoint - controller.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                controller.FaceDirection(direction);
                Vector3 step = direction.normalized * seatEnterSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                {
                    step = direction;
                }

                controller.transform.position += step;
            }

            moveTimer += Time.deltaTime;
            if (moveTimer >= maxEnterWalkTime)
            {
                break;
            }

            yield return null;
        }

        controller.PlayExternalAnimation("idle");
        controller.transform.position = seatPoint;
        controller.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
    }

    private Vector3 GetApproachPoint()
    {
        Vector3 right = GetLimoRight();
        Vector3 forward = Vector3.right;
        return transform.position + right * enterApproachOffset.x + Vector3.up * enterApproachOffset.y + forward * enterApproachOffset.z;
    }

    private Vector3 GetSeatPoint()
    {
        Vector3 right = GetLimoRight();
        Vector3 forward = Vector3.right;
        return transform.position + right * seatOffset.x + Vector3.up * seatOffset.y + forward * seatOffset.z;
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

    private GameObject CreateCameraAnchor()
    {
        Vector3 driveDirection = Vector3.right;
        GameObject anchor = new GameObject("LimoDayCameraAnchor");
        UpdateCameraAnchor(anchor.transform, driveDirection);
        return anchor;
    }

    private void UpdateCameraAnchor(Transform anchor, Vector3 driveDirection)
    {
        anchor.position = transform.position + cameraAnchorOffset;

        Vector3 lookDirection = (transform.position + cameraLookOffset) - anchor.position;
        if (lookDirection.sqrMagnitude <= 0.0001f)
        {
            lookDirection = driveDirection;
        }

        anchor.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
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
