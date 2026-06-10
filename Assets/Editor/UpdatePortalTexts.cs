#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

[InitializeOnLoad]
public class UpdatePortalTexts
{
    static UpdatePortalTexts()
    {
        EditorApplication.delayCall += DoUpdate;
    }

    static void DoUpdate()
    {
        if (EditorPrefs.GetBool("PortalTextsUpdated_v1", false)) return;

        string scenePath = "Assets/Scenes/masmorra_spawn.unity";
        if (!File.Exists(scenePath)) return;
        
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
        bool anyChanged = false;

        foreach (var root in scene.GetRootGameObjects())
        {
            var portals = root.GetComponentsInChildren<PortalSceneInteractable>(true);
            foreach (var portal in portals)
            {
                var serializedObject = new SerializedObject(portal);
                var promptTextProp = serializedObject.FindProperty("promptText");
                if (promptTextProp != null)
                {
                    promptTextProp.stringValue = "Entrar";
                    serializedObject.ApplyModifiedProperties();
                    anyChanged = true;
                }
            }
        }
        
        if (anyChanged)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Portal texts in masmorra_spawn successfully updated to 'Entrar'!");
        }
        
        EditorSceneManager.CloseScene(scene, true);
        EditorPrefs.SetBool("PortalTextsUpdated_v1", true);
    }
}
#endif
