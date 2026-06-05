using UnityEngine;

public class SimpleIdleAnimation : MonoBehaviour
{
    [Header("Floating (Levitacao)")]
    public bool enableFloating = true;
    public float floatAmplitude = 0.03f; // Quão alto ele sobe e desce
    public float floatSpeed = 1.5f;

    [Header("Breathing (Respiracao)")]
    public bool enableBreathing = true;
    public float breathAmplitude = 0.015f; // Quanto ele incha/desincha
    public float breathSpeed = 2.0f;

    [Header("Wobble (Balanco)")]
    public bool enableWobble = true;
    public float wobbleAmplitude = 1.5f; // Rotação em graus
    public float wobbleSpeed = 1.0f;

    private Vector3 startPos;
    private Vector3 startScale;
    private Quaternion startRot;

    void Start()
    {
        // Guardar os valores originais para usar como base matemática
        startPos = transform.localPosition;
        startScale = transform.localScale;
        startRot = transform.localRotation;
    }

    void Update()
    {
        // 1. Efeito de flutuar suavemente (Levitação)
        if (enableFloating)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }

        // 2. Efeito de respirar (Aumentar e diminuir ligeiramente a escala)
        if (enableBreathing)
        {
            float scaleMod = 1f + Mathf.Sin(Time.time * breathSpeed) * breathAmplitude;
            // Estica ligeiramente no peito (X e Z) e comprime no Y para dar ilusao de respirar
            transform.localScale = new Vector3(
                startScale.x * scaleMod,
                startScale.y * (1f + (1f - scaleMod) * 0.5f), 
                startScale.z * scaleMod
            );
        }

        // 3. Efeito de balanço (Parece que está vivo a olhar em volta suavemente)
        if (enableWobble)
        {
            float rotX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmplitude;
            float rotZ = Mathf.Cos(Time.time * wobbleSpeed * 0.8f) * wobbleAmplitude * 0.5f;
            transform.localRotation = startRot * Quaternion.Euler(rotX, 0f, rotZ);
        }
    }
}
