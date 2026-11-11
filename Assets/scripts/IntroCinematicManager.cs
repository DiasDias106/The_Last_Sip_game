using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
public class IntroCinematicManager : MonoBehaviour
{
    [SerializeField] public DialogueData introDialogue;
    [SerializeField] private float descentDuration = 3.5f;
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 1.5f, -1.5f);
    private void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        int activeSlot = SaveManager.GetActiveSlot();
        string prefsKey = $"IntroPlayed_{sceneName}_Slot_{activeSlot}";
        if (PlayerPrefs.GetInt(prefsKey, 0) == 0)
        {
            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
            var playerController = FindObjectOfType<UserImportedPlayerController>();
            if (playerController != null)
            {
                playerController.cameraLocalOffset = new Vector3(0f, 0.4f, -3.2f) + startOffset;
            }
            StartCoroutine(PlayCinematic());
        }
    }
    private IEnumerator PlayCinematic()
    {
        yield return null;
        yield return null;
        var playerController = FindObjectOfType<UserImportedPlayerController>();
        if (playerController == null) yield break;
        playerController.SetExternalControlLocked(true);
        Vector3 finalLocalOffset = new Vector3(0f, 0.4f, -3.2f);
        Vector3 startLocalOffset = finalLocalOffset + startOffset; 
        playerController.cameraLocalOffset = startLocalOffset;
        if (DialogueUIManager.Instance != null && introDialogue != null)
        {
            DialogueUIManager.Instance.StartDialogue(introDialogue);
        }
        float elapsed = 0f;
        while (elapsed < descentDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / descentDuration);
            float easeT = 1f - Mathf.Pow(1f - t, 4f); 
            playerController.cameraLocalOffset = Vector3.Lerp(startLocalOffset, finalLocalOffset, easeT);
            yield return null;
        }
        playerController.cameraLocalOffset = finalLocalOffset;
        playerController.SetExternalControlLocked(false);
    }
}
