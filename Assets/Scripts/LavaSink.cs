using UnityEngine;
using System.Collections;

// Coloca este script no trigger/collider da lava.
// Qualquer Rigidbody (exceto o player) que entre afunda progressivamente.
[RequireComponent(typeof(Collider))]
public class LavaSink : MonoBehaviour
{
    [SerializeField] private float sinkSpeed   = 0.8f;
    [SerializeField] private float sinkDepth   = 2f;
    [SerializeField] private float destroyAfter = 3f;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) return;

        var rb = other.attachedRigidbody;
        if (rb == null) return;

        StartCoroutine(SinkObject(rb));
    }

    private IEnumerator SinkObject(Rigidbody rb)
    {
        if (rb == null) yield break;

        rb.useGravity = false;
        rb.linearDamping = 5f;
        rb.angularDamping = 5f;

        float elapsed = 0f;
        while (rb != null && elapsed < destroyAfter)
        {
            elapsed += Time.deltaTime;
            rb.AddForce(Vector3.down * sinkSpeed, ForceMode.Acceleration);
            yield return null;
        }

        if (rb != null)
            Destroy(rb.gameObject);
    }
}
