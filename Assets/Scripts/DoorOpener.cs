using UnityEngine;
using System.Collections;

public class DoorOpener : MonoBehaviour
{
    public Vector3 openOffset   = new Vector3(0f, 4f, 0f);
    public float   openDuration = 1.5f;
    public bool hideOnOpen = false;

    private Vector3 _closedPosition;
    private bool _opened;

    void Awake()
    {
        _closedPosition = transform.position;
    }

    public void Open()
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(OpenRoutine());
    }

    public void CloseAndReset()
    {
        StopAllCoroutines();
        _opened = false;
        transform.position = _closedPosition;

        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = true;
        }

        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            r.enabled = true;
        }
    }

    IEnumerator OpenRoutine()
    {
        Vector3 start = transform.position;
        Vector3 end   = start + openOffset;
        float   t     = 0f;

        while (t < openDuration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / openDuration));
            yield return null;
        }

        transform.position = end;

        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            col.enabled = false;
        }

        if (hideOnOpen)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                r.enabled = false;
            }
        }
    }
}
