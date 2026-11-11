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
        yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        while (true)
        {
            if (DialogueUIManager.Instance != null && fightDialogue != null)
            {
                DialogueUIManager.Instance.StartDialogue(fightDialogue);
            }
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }
}
