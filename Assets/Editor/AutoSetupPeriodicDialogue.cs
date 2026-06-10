#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class AutoSetupPeriodicDialogue
{
    static AutoSetupPeriodicDialogue()
    {
        EditorApplication.delayCall += SetupOnce;
    }

    static void SetupOnce()
    {
        if (EditorPrefs.GetBool("PeriodicDialogueSetupDone_v2", false)) return;

        string[] scenes = { 
            "Assets/Scenes/level_1.unity", 
            "Assets/Scenes/level_2.unity", 
            "Assets/Scenes/level_3.unity" 
        };
        
        var guids = AssetDatabase.FindAssets("Dialogo_Luta t:DialogueData");
        if (guids.Length == 0)
        {
            Debug.LogWarning("Dialogo_Luta asset not found. Skipping auto setup.");
            return;
        }
        
        var dialogueData = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guids[0]));
        bool anyChanged = false;

        foreach (var scenePath in scenes)
        {
            if (!File.Exists(scenePath)) continue;
            
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            
            PeriodicDialogueTrigger existingTrigger = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                existingTrigger = root.GetComponent<PeriodicDialogueTrigger>();
                if (existingTrigger != null) break;
            }
            
            if (existingTrigger == null)
            {
                var go = new GameObject("PeriodicDialogueTrigger");
                SceneManager.MoveGameObjectToScene(go, scene);
                existingTrigger = go.AddComponent<PeriodicDialogueTrigger>();
                Debug.Log($"Added PeriodicDialogueTrigger to {scene.name}");
            }

            existingTrigger.fightDialogue = dialogueData;
            existingTrigger.minInterval = 20f;
            existingTrigger.maxInterval = 40f;
            EditorUtility.SetDirty(existingTrigger);
            
            EditorSceneManager.SaveScene(scene);
            anyChanged = true;
            
            EditorSceneManager.CloseScene(scene, true);
        }
        
        if (anyChanged)
        {
            Debug.Log("Periodic Dialogue intervals successfully updated in all levels!");
        }
        
        EditorPrefs.SetBool("PeriodicDialogueSetupDone_v2", true);
    }
}
#endif
