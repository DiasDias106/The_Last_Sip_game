using UnityEngine;
public class SimpleIdleAnimation : MonoBehaviour
{
    [Header("Floating (Levitacao)")]
    public bool enableFloating = true;
    public float floatAmplitude = 0.03f;
    public float floatSpeed = 1.5f;
    [Header("Breathing (Respiracao)")]
    public bool enableBreathing = true;
    public float breathAmplitude = 0.015f;
    public float breathSpeed = 2.0f;
    [Header("Wobble (Balanco)")]
    public bool enableWobble = true;
    public float wobbleAmplitude = 1.5f;
    public float wobbleSpeed = 1.0f;
    private Vector3 startPos;
    private Vector3 startScale;
    private Quaternion startRot;
    void Start()
    {
        startPos = transform.localPosition;
        startScale = transform.localScale;
        startRot = transform.localRotation;
    }
    void Update()
    {
        if (enableFloating)
        {
            float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = new Vector3(startPos.x, newY, startPos.z);
        }
        if (enableBreathing)
        {
            float scaleMod = 1f + Mathf.Sin(Time.time * breathSpeed) * breathAmplitude;
            transform.localScale = new Vector3(
                startScale.x * scaleMod,
                startScale.y * (1f + (1f - scaleMod) * 0.5f), 
                startScale.z * scaleMod
            );
        }
        if (enableWobble)
        {
            float rotX = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAmplitude;
            float rotZ = Mathf.Cos(Time.time * wobbleSpeed * 0.8f) * wobbleAmplitude * 0.5f;
            transform.localRotation = startRot * Quaternion.Euler(rotX, 0f, rotZ);
        }
    }
}
