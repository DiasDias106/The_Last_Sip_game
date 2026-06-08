using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RelinkTrapPrefabs
{
    public static string Run()
    {
        int flechas = RelinkGroup("armadilha_flechas", "Assets/Prefabs/Armadilhas/armadilha_flechas.prefab");
        int espinhos = RelinkGroup("armadilha_espinhos", "Assets/Prefabs/Armadilhas/armadilha_espinhos.prefab");
        int machado = RelinkGroup("armadilha_machado", "Assets/Prefabs/Armadilhas/armadilha_machado.prefab");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        return $"Religadas instancias: flechas={flechas}, espinhos={espinhos}, machado={machado}";
    }

    private static int RelinkGroup(string groupName, string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var group = GameObject.Find(groupName);
        if (prefab == null || group == null)
        {
            return 0;
        }

        var children = new List<GameObject>();
        for (int i = 0; i < group.transform.childCount; i++)
        {
            children.Add(group.transform.GetChild(i).gameObject);
        }

        int count = 0;
        foreach (var oldGo in children)
        {
            if (oldGo.name != groupName && oldGo.name != groupName + "_template")
            {
                continue;
            }

            var parent = oldGo.transform.parent;
            var sibling = oldGo.transform.GetSiblingIndex();
            var localPosition = oldGo.transform.localPosition;
            var localRotation = oldGo.transform.localRotation;
            var localScale = oldGo.transform.localScale;
            var active = oldGo.activeSelf;
            var isStatic = oldGo.isStatic;

            var newGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab, oldGo.scene);
            newGo.name = groupName;
            newGo.transform.SetParent(parent, false);
            newGo.transform.SetSiblingIndex(sibling);
            newGo.transform.localPosition = localPosition;
            newGo.transform.localRotation = localRotation;
            newGo.transform.localScale = localScale;
            newGo.SetActive(active);
            newGo.isStatic = isStatic;

            Object.DestroyImmediate(oldGo);
            count++;
        }

        return count;
    }
}
