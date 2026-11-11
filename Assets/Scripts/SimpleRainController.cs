using UnityEngine;
public class SimpleRainController : MonoBehaviour
{
    [Header("Material Settings")]
    public Material customRainMaterial;
    [Header("Rain Settings")]
    [Range(100, 10000)]
    public float rainIntensity = 2000f;
    [Range(5f, 50f)]
    public float rainSpeed = 25f;
    [Range(0.005f, 0.1f)]
    public float dropWidth = 0.015f;
    [Range(0.5f, 5f)]
    public float dropLengthScale = 2.5f;
    public Color rainColor = new Color(0.6f, 0.7f, 0.9f, 0.2f);
    [Header("Area Settings")]
    public Vector2 rainAreaSize = new Vector2(30f, 30f);
    public float rainHeight = 15f;
    private ParticleSystem rainParticleSystem;
    private ParticleSystemRenderer pRenderer;
    void Start()
    {
        GameObject rainObj = new GameObject("RainParticleSystem");
        rainObj.transform.SetParent(this.transform);
        rainObj.transform.localPosition = new Vector3(0, rainHeight, 0); 
        rainObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rainParticleSystem = rainObj.AddComponent<ParticleSystem>();
        pRenderer = rainParticleSystem.GetComponent<ParticleSystemRenderer>();
        pRenderer.renderMode = ParticleSystemRenderMode.Stretch;
        pRenderer.velocityScale = 0.1f;
        if (customRainMaterial != null)
        {
            pRenderer.material = customRainMaterial;
        }
        else
        {
            Shader defaultShader = Shader.Find("Sprites/Default");
            if (defaultShader != null) pRenderer.material = new Material(defaultShader);
        }
        AudioClip rainSound = Resources.Load<AudioClip>("RainSound");
        if (rainSound != null)
        {
            AudioSource audioSource = rainObj.AddComponent<AudioSource>();
            audioSource.clip = rainSound;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.1f;
            audioSource.Play();
        }
        ApplySettings();
        rainParticleSystem.Play();
    }
    void Update()
    {
        if (rainParticleSystem != null)
        {
            ApplySettings();
        }
    }
    private void ApplySettings()
    {
        var main = rainParticleSystem.main;
        main.loop = true;
        main.startLifetime = rainHeight / rainSpeed;
        main.startSpeed = rainSpeed;
        main.startSize = dropWidth;
        main.startColor = rainColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 15000;
        var emission = rainParticleSystem.emission;
        emission.rateOverTime = rainIntensity;
        var shape = rainParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(rainAreaSize.x, rainAreaSize.y, 1f);
        pRenderer.lengthScale = dropLengthScale;
    }
}
