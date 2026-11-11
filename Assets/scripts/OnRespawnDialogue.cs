using UnityEngine;
public class OnRespawnDialogue : MonoBehaviour
{
    [Header("Dialogo a tocar depois de morrer")]
    public DialogueData dialogueMorreu;
    private void Start()
    {
        if (PlayerPrefs.GetInt("JustRespawned", 0) == 1)
        {
            PlayerPrefs.SetInt("JustRespawned", 0);
            PlayerPrefs.Save();
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "beco_noite")
                return;
            if (dialogueMorreu != null && DialogueUIManager.Instance != null)
            {
                Invoke(nameof(PlayDeathDialogue), 2.5f);
            }
        }
    }
    private void PlayDeathDialogue()
    {
        DialogueUIManager.Instance.StartDialogue(dialogueMorreu);
    }
}
