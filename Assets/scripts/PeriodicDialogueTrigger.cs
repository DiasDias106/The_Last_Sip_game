using UnityEngine;
using System.Collections;

public class PeriodicDialogueTrigger : MonoBehaviour
{
    [SerializeField] public DialogueData fightDialogue;
    public float minInterval = 20f;
    public float maxInterval = 40f;

    private Coroutine _dialogueRoutine;

    private void Start()
    {
        if (fightDialogue != null)
        {
            _dialogueRoutine = StartCoroutine(DialogueLoop());
        }
    }

    private void OnDestroy()
    {
        if (_dialogueRoutine != null)
        {
            StopCoroutine(_dialogueRoutine);
        }
    }

    private IEnumerator DialogueLoop()
    {
        // Esperar antes da primeira fala
        yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

        while (true)
        {
            // Tocar o diálogo (o próprio asset está configurado para "playRandomLine")
            if (DialogueUIManager.Instance != null && fightDialogue != null)
            {
                DialogueUIManager.Instance.StartDialogue(fightDialogue);
            }

            // Esperar até à próxima vez
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }
}
