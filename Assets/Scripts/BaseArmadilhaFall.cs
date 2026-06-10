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

        EnsureCollisionSetup();

        _rb.isKinematic = true;
        _rb.useGravity = false;
    }

    private void EnsureCollisionSetup()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null)
            {
                continue;
            }

            collider.enabled = true;
        }

        MeshCollider[] meshColliders = GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < meshColliders.Length; i++)
        {
            MeshCollider meshCollider = meshColliders[i];
            if (meshCollider != null && !meshCollider.convex)
            {
                meshCollider.convex = true;
            }
        }

        Bounds combinedBounds = new Bounds(transform.position, Vector3.zero);
        bool hasBounds = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds)
        {
            return;
        }

        BoxCollider rootBoxCollider = GetComponent<BoxCollider>();
        if (rootBoxCollider == null)
        {
            rootBoxCollider = gameObject.AddComponent<BoxCollider>();
        }

        rootBoxCollider.isTrigger = false;
        rootBoxCollider.center = transform.InverseTransformPoint(combinedBounds.center);
        rootBoxCollider.size = combinedBounds.size;
        rootBoxCollider.enabled = true;
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
