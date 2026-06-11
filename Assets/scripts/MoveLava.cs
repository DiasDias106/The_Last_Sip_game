using UnityEngine;
using System.Collections.Generic;

public class MoveLava : MonoBehaviour
{
    [Header("Movimento da Textura")]
    public float velocidadeX = 0.05f;
    public float velocidadeY = 0.05f;

    [Header("Ondulacao Fisica 3D")]
    public bool ativarOndulacao = true;
    public float alturaDaOnda = 0.3f; // Altura real da onda em metros
    public float velocidadeDaOnda = 1.5f;
    public float escalaDaOnda = 0.5f; // Quão largas são as ondas

    private Renderer[] renderers;
    private MeshFilter[] meshFilters;
    
    // Dicionários para guardar a geometria original de cada pedaço de lava individualmente
    private Dictionary<MeshFilter, Vector3[]> baseVerticesMap = new Dictionary<MeshFilter, Vector3[]>();
    private Dictionary<MeshFilter, Vector3[]> displacedVerticesMap = new Dictionary<MeshFilter, Vector3[]>();

    void Start()
    {
        // O SEGREDO: Ao usar InChildren, este script funciona quer esteja diretamente na peça de lava,
        // quer esteja num objeto "Pai" vazio como o 'lava_piso'! Ele vai procurar todos os filhos e controlá-los.
        renderers = GetComponentsInChildren<Renderer>();
        meshFilters = GetComponentsInChildren<MeshFilter>();

        // Garantir colliders para que PlayerLavaRespawn (OnControllerColliderHit) funcione
        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh != null && mf.GetComponent<MeshCollider>() == null)
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        foreach (var mf in meshFilters)
        {
            if (mf.sharedMesh != null)
            {
                // Criamos uma cópia da malha para não danificar o ficheiro 3D original
                Mesh instancedMesh = Instantiate(mf.sharedMesh);
                mf.mesh = instancedMesh;
                
                baseVerticesMap[mf] = instancedMesh.vertices;
                displacedVerticesMap[mf] = new Vector3[instancedMesh.vertices.Length];
            }
        }
    }

    void Update()
    {
        // 1. Animar as texturas de todos os filhos
        float offsetX = Time.time * velocidadeX;
        float offsetY = Time.time * velocidadeY;

        foreach (var rend in renderers)
        {
            if (rend != null && rend.material != null)
            {
                if (rend.material.HasProperty("_BaseMap")) rend.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
                if (rend.material.HasProperty("_EmissionMap")) rend.material.SetTextureOffset("_EmissionMap", new Vector2(offsetX, offsetY));
                if (rend.material.HasProperty("_BumpMap")) rend.material.SetTextureOffset("_BumpMap", new Vector2(offsetX, offsetY));
            }
        }

        // 2. Animar os vértices físicos (Ondulação) de todos os filhos
        if (ativarOndulacao)
        {
            float time = Time.time * velocidadeDaOnda;
            
            foreach (var mf in meshFilters)
            {
                if (!baseVerticesMap.ContainsKey(mf)) continue;
                
                float scaleY = mf.transform.localScale.y;
                if (Mathf.Abs(scaleY) < 0.001f) scaleY = 1f;

                Vector3[] baseVerts = baseVerticesMap[mf];
                Vector3[] displacedVerts = displacedVerticesMap[mf];

                for (int i = 0; i < baseVerts.Length; i++)
                {
                    Vector3 worldPos = mf.transform.TransformPoint(baseVerts[i]);
                    
                    float waveX = Mathf.Sin(time + worldPos.x * escalaDaOnda);
                    float waveZ = Mathf.Sin(time + worldPos.z * escalaDaOnda);
                    float alturaRealNoMundo = (waveX + waveZ) * alturaDaOnda;
                    
                    Vector3 movimentoLocal = mf.transform.InverseTransformVector(new Vector3(0, alturaRealNoMundo, 0));
                    displacedVerts[i] = baseVerts[i] + movimentoLocal;
                }
                
                mf.mesh.vertices = displacedVerts;
                mf.mesh.RecalculateNormals(); 
            }
        }
    }
}