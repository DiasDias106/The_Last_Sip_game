using UnityEngine;
using System.Collections;
public class DoorOpener : MonoBehaviour
{
    [Header("Position Settings")]
    public Vector3 openOffset   = new Vector3(0f, 4f, 0f);
    public bool useAbsoluteLocalPosition = false;
    public Vector3 targetLocalPosition = Vector3.zero;
    public float   openDuration = 1.5f;
    public bool hideOnOpen = false;
    [Header("Rotation Settings")]
    public bool useRotation = false;
    public Vector3 targetRotationEuler = Vector3.zero;
    [Header("Collider Settings")]
    public bool disableColliderOnOpen = true;
    private Vector3 _closedPosition;
    private Quaternion _closedRotation;
    private bool _opened;
    void Awake()
    {
        _closedPosition = transform.position;
        _closedRotation = transform.rotation;
        if (gameObject.name == "door2" && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "level_2")
        {
            useRotation = true;
            targetRotationEuler = new Vector3(transform.eulerAngles.x, -135.468f, transform.eulerAngles.z);
            openOffset = Vector3.zero;
            disableColliderOnOpen = false;
        }
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
        transform.rotation = _closedRotation;
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
        Vector3 startPos = transform.position;
        Vector3 endPos   = startPos + openOffset;
        if (useAbsoluteLocalPosition)
        {
            if (transform.parent != null)
                endPos = transform.parent.TransformPoint(targetLocalPosition);
            else
                endPos = targetLocalPosition;
        }
        Quaternion startRot = transform.rotation;
        Quaternion endRot   = startRot;
        if (useRotation)
        {
            endRot = Quaternion.Euler(targetRotationEuler);
        }
        float   t     = 0f;
        while (t < openDuration)
        {
            t += Time.deltaTime;
            float fraction = Mathf.SmoothStep(0f, 1f, t / openDuration);
            transform.position = Vector3.Lerp(startPos, endPos, fraction);
            if (useRotation)
            {
                transform.rotation = Quaternion.Slerp(startRot, endRot, fraction);
            }
            yield return null;
        }
        transform.position = endPos;
        if (useRotation)
        {
            transform.rotation = endRot;
        }
        if (disableColliderOnOpen)
        {
            foreach (var col in GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }
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
