using UnityEngine;

public class SimpleRainController : MonoBehaviour
{
    [Header("Material Settings")]
    public Material customRainMaterial;

    [Header("Rain Settings")]
    [Range(100, 10000)]
    public float rainIntensity = 2000f; // Quantidade de gotas
    
    [Range(5f, 50f)]
    public float rainSpeed = 25f; // Velocidade da queda
    
    [Range(0.005f, 0.1f)]
    public float dropWidth = 0.015f; // Grossura da gota
    
    [Range(0.5f, 5f)]
    public float dropLengthScale = 2.5f; // Comprimento da gota
    
    public Color rainColor = new Color(0.6f, 0.7f, 0.9f, 0.2f); // Cor da chuva
    
    [Header("Area Settings")]
    public Vector2 rainAreaSize = new Vector2(30f, 30f); // Área que a chuva cobre
    public float rainHeight = 15f; // Altura de onde a chuva cai

    private ParticleSystem rainParticleSystem;
    private ParticleSystemRenderer pRenderer;

    void Start()
    {
        // Cria um novo GameObject para a chuva
        GameObject rainObj = new GameObject("RainParticleSystem");
        rainObj.transform.SetParent(this.transform);
        rainObj.transform.localPosition = new Vector3(0, rainHeight, 0); 
        rainObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Aponta para baixo

        rainParticleSystem = rainObj.AddComponent<ParticleSystem>();
        pRenderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();

        // Configuração inicial do renderizador
        pRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        pRenderer.velocityScale = 0.1f;
        
        // Aplica o material
        if (customRainMaterial != null)
        {
            pRenderer.material = customRainMaterial;
        }
        else
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null) pRenderer.material = new Material(defaultShader);
        }

        // Adiciona som de chuva
        AudioClip rainSound = Resources.Load<AudioClip>("RainSound");
        if (rainSound != null)
        {
            AudioSource audioSource = rainObj.AddComponent<AudioSource>();
            audioSource.clip = rainSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // Som ambiente 2D global
            audioSource.volume = 0.1f;
            audioSource.Play();
        }

        ApplySettings();
        rainParticleSystem.Play();
    }

    // O Update permite que você veja as mudanças em tempo real no Unity enquanto joga!
    void Update()
    {
        if (rainParticleSystem != null)
        {
            ApplySettings();
        }
    }

    private void ApplySettings()
    {
        // Main Module
        var main = rainParticleSystem.main;
        main.loop = true;
        main.startLifetime = rainHeight / rainSpeed; // Tempo de vida baseado na altura e velocidade
        main.startSpeed = rainSpeed;
        main.startSize = dropWidth;
        main.startColor = rainColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 15000;

        // Emission Module
        var emission = rainParticleSystem.emission;
        emission.rateOverTime = rainIntensity;

        // Shape Module
        var shape = rainParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize.x, rainAreaSize.y, 1f);

        // Renderer Module
        pRenderer.lengthScale = dropLengthScale;
    }
}
