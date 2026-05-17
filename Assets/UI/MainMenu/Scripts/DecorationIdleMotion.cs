using UnityEngine;

public sealed class DecorationIdleMotion : MonoBehaviour
{
    [SerializeField] private RectTransform target;
    [SerializeField] private Vector2 motionAmplitude = new(0f, 6f);
    [SerializeField] private float rotationAmplitude = 1.5f;
    [SerializeField] private float motionSpeed = 0.55f;
    [SerializeField] private float phaseOffset;

    private Vector2 initialPosition;
    private Quaternion initialRotation;

    private void Awake()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        if (target == null)
        {
            enabled = false;
            return;
        }

        initialPosition = target.anchoredPosition;
        initialRotation = target.localRotation;
    }

    private void Update()
    {
        if (target == null)
        {
            return;
        }

        float t = Time.unscaledTime * motionSpeed + phaseOffset;
        float sin = Mathf.Sin(t);
        float cos = Mathf.Cos(t * 0.85f);

        target.anchoredPosition = initialPosition + new Vector2(cos * motionAmplitude.x, sin * motionAmplitude.y);
        target.localRotation = initialRotation * Quaternion.Euler(0f, 0f, sin * rotationAmplitude);
    }
}
