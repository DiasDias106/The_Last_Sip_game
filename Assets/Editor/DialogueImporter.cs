using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class DialogueImporter
{
    [MenuItem("Tools/Importar Diálogos (legendas.txt)")]
    public static void Import()
    {
        string path = "Assets/dialogos/legendas.txt";
        if (!File.Exists(path))
        {
            Debug.LogError("Ficheiro não encontrado: " + path);
            return;
        }

        string[] lines = File.ReadAllLines(path);
        ParseAndCreateAll(lines);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Importação dos MP3 e legendas concluída com sucesso! Ficheiros criados na pasta 'Assets/dialogos'.");
    }

    private static void ParseAndCreateAll(string[] lines)
    {
        string currentCategory = "";
        DialogueData currentData = null;
        List<DialogueLine> currentLines = new List<DialogueLine>();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            if (line.ToLower().Contains("loja:") && !line.ToLower().Contains("interacoes loja:"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Loja";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true; 
                currentLines = new List<DialogueLine>();
            }
            else if (line.ToLower().Contains("luta:"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Luta";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true;
                currentLines = new List<DialogueLine>();
            }
            else if (line.ToLower().Contains("morreu"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Morreu";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true;
                currentLines = new List<DialogueLine>();
            }
            else if (line.ToLower().Contains("inventario cheio:"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Loja_InventarioCheio";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true;
                currentLines = new List<DialogueLine>();
            }
            else if (line.ToLower().Contains("sair sem comprar nada:"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Loja_SairSemComprar";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true;
                currentLines = new List<DialogueLine>();
            }
            else if (line.ToLower().Contains("comprar:"))
            {
                SaveCurrent(currentCategory, currentData, currentLines);
                currentCategory = "Dialogo_Loja_Comprar";
                currentData = ScriptableObject.CreateInstance<DialogueData>();
                currentData.playRandomLine = true;
                currentLines = new List<DialogueLine>();
            }
            else if (int.TryParse(line, out _))
            {
                // É um número (ID do áudio)
                DialogueLine dl = new DialogueLine();
                string audioPath = $"Assets/dialogos/{line}.mp3";
                dl.audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);

                // Ler a próxima linha com texto (PT)
                i++;
                while (i < lines.Length && string.IsNullOrEmpty(lines[i].Trim())) i++;
                if (i < lines.Length) dl.textPT = lines[i].Trim();

                // Ler a próxima linha com texto (EN)
                i++;
                while (i < lines.Length && string.IsNullOrEmpty(lines[i].Trim())) i++;
                if (i < lines.Length) dl.textEN = lines[i].Trim();

                currentLines.Add(dl);
            }
        }
        SaveCurrent(currentCategory, currentData, currentLines);
    }

    private static void SaveCurrent(string category, DialogueData data, List<DialogueLine> linesList)
    {
        if (data != null && linesList.Count > 0)
        {
            data.lines = linesList.ToArray();
            AssetDatabase.CreateAsset(data, $"Assets/dialogos/{category}.asset");
        }
    }
}
