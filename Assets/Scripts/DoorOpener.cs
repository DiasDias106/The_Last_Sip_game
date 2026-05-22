using UnityEngine;
using System.Collections;

public class DoorOpener : MonoBehaviour
{
    public Vector3 openOffset   = new Vector3(0f, 4f, 0f);
    public float   openDuration = 1.5f;

    private bool _opened;

    public void Open()
    {
        if (_opened) return;
        _opened = true;
        StartCoroutine(OpenRoutine());
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

        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }
}
