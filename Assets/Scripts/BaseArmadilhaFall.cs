using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class BaseArmadilhaFall : MonoBehaviour
{
    [SerializeField] private float fallDelay = 1f;

    private Rigidbody _rb;
    private bool _triggered;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb = gameObject.AddComponent<Rigidbody>();
        }

        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    public void TriggerFall()
    {
        if (_triggered)
        {
            return;
        }

        _triggered = true;
        StartCoroutine(FallAfterDelay());
    }

    private IEnumerator FallAfterDelay()
    {
        yield return new WaitForSeconds(fallDelay);

        gameObject.isStatic = false;
        _rb.isKinematic = false;
        _rb.useGravity = true;
    }
}
