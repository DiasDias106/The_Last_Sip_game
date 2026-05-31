using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 10)]
    public string textEN;
    
    [TextArea(3, 10)]
    public string textPT;

    public AudioClip audioClip;
}

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
    
    [Tooltip("Se ativo, apenas UMA frase aleatória desta lista será mostrada por vez (útil para dicas/pensamentos soltos).")]
    public bool playRandomLine = false;

    public string GetLine(int index, LanguageManager.Language language)
    {
        if (index < 0 || index >= lines.Length) return "";

        return language == LanguageManager.Language.PT ? lines[index].textPT : lines[index].textEN;
    }
}
