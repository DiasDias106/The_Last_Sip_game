using UnityEngine;
[RequireComponent(typeof(BoxCollider))]
public class DialogueTriggerArea : MonoBehaviour
{
    [Header("Dialogue Content")]
    public DialogueData dialogueData;
    [Header("Settings")]
    public bool playOnlyOnce = true;
    private bool _hasPlayed = false;
    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        gameObject.layer = 2; 
    }
    private void OnTriggerEnter(Collider other)
    {
        if (playOnlyOnce && _hasPlayed) return;
        if (other.CompareTag("Player") || other.GetComponent<UserImportedPlayerController>() != null)
        {
            if (dialogueData != null)
            {
                DialogueUIManager.Instance.StartDialogue(dialogueData);
                _hasPlayed = true;
            }
            else
            {
                Debug.LogWarning("Esta área de diálogo não tem um DialogueData associado!");
            }
        }
    }
}
