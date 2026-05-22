using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AutoCopyPrefab
{
    [MenuItem("Tools/Copiar Menu do Level 1")]
    public static void RunCopy()
    {
        Debug.Log("A copiar o PauseMenuCanvas do level_1...");
        
        UnityEngine.SceneManagement.Scene level1Scene = default;
        bool wasOpen = false;
        for (int i = 0; i < EditorSceneManager.sceneCount; i++)
        {
            var s = EditorSceneManager.GetSceneAt(i);
            if (s.name == "level_1")
            {
                level1Scene = s;
                wasOpen = true;
                break;
            }
        }

        if (!wasOpen)
        {
            level1Scene = EditorSceneManager.OpenScene("Assets/Scenes/level_1.unity", OpenSceneMode.Additive);
        }

        GameObject canvas = null;
        foreach (var go in level1Scene.GetRootGameObjects())
        {
            if (go.name == "PauseMenuCanvas")
            {
                canvas = go;
                break;
            }
        }

        if (canvas != null)
        {
            var prefabPath = "Assets/Resources/PauseMenuCanvas.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(canvas, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log("Cópia concluída com sucesso! O Prefab em Resources foi atualizado com a versão exata do level_1.");
        }
        else
        {
            Debug.LogError("Não foi possível encontrar o PauseMenuCanvas no level_1!");
        }

        if (!wasOpen)
        {
            EditorSceneManager.CloseScene(level1Scene, true);
        }
    }
}
