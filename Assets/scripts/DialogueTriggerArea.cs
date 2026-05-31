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
        // Garante que o collider é do tipo trigger
        GetComponent<BoxCollider>().isTrigger = true;
        
        // Garante que a câmara do jogador ignora este cubo invisível (Layer 2 é a 'Ignore Raycast')
        gameObject.layer = 2; 
    }

    private void OnTriggerEnter(Collider other)
    {
        if (playOnlyOnce && _hasPlayed) return;

        // Se quiseres podes adicionar a tag "Player" ao teu jogador,
        // mas usar o script principal dele funciona como garantia
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
