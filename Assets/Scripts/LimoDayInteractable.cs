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
    [SerializeField] private float driveSpeed = 9f;
    [SerializeField] private float preDriveDelay = 0.35f;
    [SerializeField] private float cameraPitch = 16f;
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";

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
        if (controller == null)
        {
            return;
        }

        _sequenceStarted = true;
        StartCoroutine(DriveToCredits(controller));
    }

    private IEnumerator DriveToCredits(UserImportedPlayerController controller)
    {
        controller.SetExternalControlLocked(true);
        controller.SetCameraFollowOverride(transform, true, cameraPitch);
        controller.PlayExternalAnimation("idle");

        if (playerSeatPoint != null)
        {
            controller.transform.position = playerSeatPoint.position;
            controller.transform.rotation = playerSeatPoint.rotation;
            controller.transform.SetParent(transform, true);
        }

        controller.SetVisualVisible(false);
        yield return new WaitForSeconds(preDriveDelay);

        Vector3 fallbackTarget = transform.position + transform.forward * 32f;
        Vector3 destination = driveTarget != null ? driveTarget.position : fallbackTarget;

        while (Vector3.Distance(transform.position, destination) > 0.2f)
        {
            Vector3 direction = destination - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                Vector3 step = direction.normalized * driveSpeed * Time.deltaTime;
                if (step.sqrMagnitude > direction.sqrMagnitude)
                {
                    step = direction;
                }

                transform.position += step;
                transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            }

            yield return null;
        }

        GlobalSceneTransitionRequest.PendingOpenCredits = true;
        SceneManager.LoadScene(mainMenuSceneName);
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
        Vector3 targetPosition = playerSeatPoint != null ? playerSeatPoint.position : transform.position;
        playerPosition.y = 0f;
        targetPosition.y = 0f;
        return Vector3.Distance(playerPosition, targetPosition) <= interactionDistance;
    }
}
