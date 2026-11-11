using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Adiciona a qualquer NPC para ver as animações — usa ← → para mudar, remove depois
public class NPCAnimDebug : MonoBehaviour
{
    [SerializeField] private string gltfFolder = "capra_demon_-_dark_souls_remastered_test_2";

    private AnimationClip[] _clips;
    private PlayableGraph _graph;
    private AnimationPlayableOutput _output;
    private AnimationClipPlayable _current;
    private Animator _anim;
    private int _index = 0;

    private void Start()
    {
        _anim = GetComponentInChildren<Animator>();
        if (_anim == null) { Debug.LogError("Animator não encontrado"); return; }

#if UNITY_EDITOR
        string path = $"Assets/NPC/{gltfFolder}/scene.gltf";
        var list = new System.Collections.Generic.List<AnimationClip>();
        foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path))
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                list.Add(c);
        _clips = list.ToArray();
#endif
        if (_clips == null || _clips.Length == 0) { Debug.LogError("Clips não encontrados"); return; }

        _anim.runtimeAnimatorController = null;
        _graph = PlayableGraph.Create("NPCDebug");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        _output = AnimationPlayableOutput.Create(_graph, "Debug", _anim);
        _graph.Play();
        Play(_index);
    }

    private void Update()
    {
        if (_clips == null) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) { _index = (_index + 1) % _clips.Length; Play(_index); }
        if (Input.GetKeyDown(KeyCode.LeftArrow))  { _index = (_index + _clips.Length - 1) % _clips.Length; Play(_index); }
    }

    private void Play(int i)
    {
        if (_current.IsValid()) _current.Destroy();
        _current = AnimationClipPlayable.Create(_graph, _clips[i]);
        _output.SetSourcePlayable(_current);
        Debug.Log($"[NPCDebug] [{i}] {_clips[i].name} ({_clips[i].length:F2}s)");
    }

    private void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }

    private void OnGUI()
    {
        if (_clips == null) return;
        GUI.skin.label.fontSize = 16;
        GUI.Label(new Rect(10, 10, 500, 30), $"← → mudar | [{_index}] {(_clips.Length > _index ? _clips[_index].name : "")}");
        GUI.Label(new Rect(10, 45, 500, 30), "Diz ao Claude: [índice]=walk, [índice]=run, etc.");
    }
}
