#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

[InitializeOnLoad]
public class SetupIntroCinematics
{
    static SetupIntroCinematics()
    {
        EditorApplication.delayCall += DoSetup;
    }

    static void DoSetup()
    {
        if (EditorPrefs.GetBool("IntroCinematicsSetupDone_v3", false)) return;

        // 1. Create DialogueData Assets
        string audioBecoPath = "Assets/Dialogos/18.mp3";
        string audioMasmorraPath = "Assets/Dialogos/19.mp3";
        
        string assetBecoPath = "Assets/Dialogos/Dialogo_IntroBeco.asset";
        string assetMasmorraPath = "Assets/Dialogos/Dialogo_IntroMasmorra.asset";

        DialogueData becoData = AssetDatabase.LoadAssetAtPath<DialogueData>(assetBecoPath);
        if (becoData == null)
        {
            becoData = ScriptableObject.CreateInstance<DialogueData>();
            becoData.lines = new DialogueLine[1];
            becoData.lines[0] = new DialogueLine
            {
                audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioBecoPath),
                textPT = "Como é que eu vim parar aqui?... Espera, acho que vejo uma garrafa ao longe.",
                textEN = "How did I end up here?... Wait, I think I see a bottle in the distance."
            };
            AssetDatabase.CreateAsset(becoData, assetBecoPath);
        }

        DialogueData masmorraData = AssetDatabase.LoadAssetAtPath<DialogueData>(assetMasmorraPath);
        if (masmorraData == null)
        {
            masmorraData = ScriptableObject.CreateInstance<DialogueData>();
            masmorraData.lines = new DialogueLine[1];
            masmorraData.lines[0] = new DialogueLine
            {
                audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioMasmorraPath),
                textPT = "Não me lembro de como cheguei aqui. Mas aqueles portais na parede... parecem ser a minha única saída daqui.",
                textEN = "I don't remember how I got here. But those portals on the wall... they look like my only way out."
            };
            AssetDatabase.CreateAsset(masmorraData, assetMasmorraPath);
        }
        
        AssetDatabase.SaveAssets();

        // 2. Add Component to Scenes
        string[] becoScenes = { "Assets/Scenes/beco_noite.unity" };
        string[] masmorraScenes = { "Assets/Scenes/masmorra_spawn.unity" };
        
        bool anyChanged = false;

        foreach (var scenePath in becoScenes)
        {
            if (ApplyToScene(scenePath, becoData)) anyChanged = true;
        }
        
        foreach (var scenePath in masmorraScenes)
        {
            if (ApplyToScene(scenePath, masmorraData)) anyChanged = true;
        }

        // Clean up beco_dia
        if (RemoveFromScene("Assets/Scenes/beco_dia.unity")) anyChanged = true;

        if (anyChanged)
        {
            Debug.Log("Intro Cinematics setup successfully completed!");
        }

        EditorPrefs.SetBool("IntroCinematicsSetupDone_v3", true);
    }

    static bool RemoveFromScene(string scenePath)
    {
        if (!File.Exists(scenePath)) return false;
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        bool modified = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            var manager = root.GetComponentInChildren<IntroCinematicManager>(true);
            if (manager != null)
            {
                Object.DestroyImmediate(manager.gameObject);
                modified = true;
            }
        }

        if (modified)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Removed IntroCinematicManager from {scene.name}");
        }
        EditorSceneManager.CloseScene(scene, true);
        return modified;
    }

    static bool ApplyToScene(string scenePath, DialogueData data)
    {
        if (!File.Exists(scenePath)) return false;

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        bool modified = false;

        IntroCinematicManager manager = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            manager = root.GetComponentInChildren<IntroCinematicManager>(true);
            if (manager != null) break;
        }

        if (manager == null)
        {
            var go = new GameObject("IntroCinematicManager");
            SceneManager.MoveGameObjectToScene(go, scene);
            manager = go.AddComponent<IntroCinematicManager>();
            Debug.Log($"Added IntroCinematicManager to {scene.name}");
            modified = true;
        }

        if (manager.introDialogue != data)
        {
            manager.introDialogue = data;
            EditorUtility.SetDirty(manager);
            modified = true;
        }

        if (modified)
        {
            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.CloseScene(scene, true);
        return modified;
    }
}
#endif
