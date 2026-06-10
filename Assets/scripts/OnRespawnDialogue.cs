using UnityEngine;

public class OnRespawnDialogue : MonoBehaviour
{
    [Header("Dialogo a tocar depois de morrer")]
    public DialogueData dialogueMorreu;

    private void Start()
    {
        // Se a flag JustRespawned for 1, significa que o jogador acabou de clicar "Tentar Novamente"
        if (PlayerPrefs.GetInt("JustRespawned", 0) == 1)
        {
            // Apagar a flag para não repetir se o jogador sair e voltar ao jogo normalmente
            PlayerPrefs.SetInt("JustRespawned", 0);
            PlayerPrefs.Save();

            // Não tocar o diálogo de morte no beco_noite
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "beco_noite")
                return;

            if (dialogueMorreu != null && DialogueUIManager.Instance != null)
            {
                // Esperar um bocadinho para a cena carregar completamente (o ecrã de loading demora uns segundos)
                Invoke(nameof(PlayDeathDialogue), 2.5f);
            }
        }
    }

    private void PlayDeathDialogue()
    {
        DialogueUIManager.Instance.StartDialogue(dialogueMorreu);
    }
}
