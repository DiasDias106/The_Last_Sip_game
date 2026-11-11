using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Script temporário para identificar animações — adiciona ao Sif, corre o jogo e usa ← → para ver cada clip
public class SifAnimDebug : MonoBehaviour
{
    [SerializeField] private string gltfPath = "Assets/NPC/great_grey_wolf_sif_-_dark_souls_remastered/scene.gltf";

    private AnimationClip[] _clips;
    private PlayableGraph _graph;
    private AnimationPlayableOutput _output;
    private AnimationClipPlayable _current;
    private Animator _anim;
    private int _index = 0;

    private static readonly string[] Names = {
        "[0] 1.67s", "[1] 4.33s", "[2] 2.83s",
        "[3] 3.00s", "[4] 6.00s", "[5] 0.67s"
    };

    private void Start()
    {
        _anim = GetComponentInChildren<Animator>();
        if (_anim == null) { Debug.LogError("Animator não encontrado!"); return; }

#if UNITY_EDITOR
        var list = new System.Collections.Generic.List<AnimationClip>();
        foreach (var a in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(gltfPath))
            if (a is AnimationClip c && !c.name.StartsWith("__preview__"))
                list.Add(c);
        _clips = list.ToArray();
#endif
        if (_clips == null || _clips.Length == 0) { Debug.LogError("Clips não encontrados!"); return; }

        _anim.runtimeAnimatorController = null; // desligar o AC para ver clips diretos
        _graph = PlayableGraph.Create("SifDebug");
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
        Debug.Log($"[SifDebug] Clip {Names[i]} — {_clips[i].name}");
    }

    private void OnDestroy() { if (_graph.IsValid()) _graph.Destroy(); }

    private void OnGUI()
    {
        if (_clips == null) return;
        GUI.skin.label.fontSize = 18;
        GUI.Label(new Rect(10, 10, 500, 35), $"← → mudar clip | Atual: {Names[_index]}");
        GUI.Label(new Rect(10, 50, 500, 35), "Diz ao Claude qual clip é o Walk e qual é o Run!");
    }
}
