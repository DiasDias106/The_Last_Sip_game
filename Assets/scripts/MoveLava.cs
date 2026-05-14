using UnityEngine;

public class MoveLava : MonoBehaviour
{
    public float velocidadeX = 0.05f;
    public float velocidadeY = 0.05f;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float offsetX = Time.time * velocidadeX;
        float offsetY = Time.time * velocidadeY;

        // No URP, a textura principal geralmente se chama "_BaseMap"
        rend.material.SetTextureOffset("_BaseMap", new Vector2(offsetX, offsetY));
        // Se tiver problemas com o brilho e normal não acompanhando, 
        // adicione as linhas abaixo também:
        rend.material.SetTextureOffset("_EmissionMap", new Vector2(offsetX, offsetY));
        rend.material.SetTextureOffset("_BumpMap", new Vector2(offsetX, offsetY));
    }
}