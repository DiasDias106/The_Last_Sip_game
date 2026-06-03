using UnityEngine;
using UnityEngine.Events;

public class LeverInteractable : MonoBehaviour, IInteractable
{
    public bool IsActivated { get; private set; }
    public UnityEvent onActivated = new UnityEvent();

    public bool CanInteract => !IsActivated;

    [Header("Animation Settings")]
    public float targetRotationX = 46.986f;
    
    private bool useRelativeRotation;
    private float relativeRotationX;
public float animationDuration = 1.5f; // Aumentado para parecer mais pesado

void Awake()
    {
        useRelativeRotation = false;

        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "level_2")
        {
            if (gameObject.name == "big_door_lever")
            {
                targetRotationX = -66.194f;
                animationDuration = 2.5f; // Esta alavanca e ainda mais dura/pesada
            }
            else
            {
                targetRotationX = 46.986f;
                animationDuration = 1.5f;
            }
        }
    }

    public void Interact()
    {
        if (IsActivated) return;
        IsActivated = true;

        StartCoroutine(AnimateAndInvoke());
    }

    private System.Collections.IEnumerator AnimateAndInvoke()
    {
        Transform handle = null;
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "SM_PROP_lever_handle_dungeon_01" || child.name == "SM_PROP_lever_handle_dungeon_03")
            {
                handle = child;
                break;
            }
        }

        if (handle == null && transform.childCount > 0)
        {
            handle = transform.GetChild(0);
        }

        if (handle != null)
        {
            Quaternion startRot = handle.localRotation;
            Quaternion endRot = Quaternion.Euler(targetRotationX, handle.localEulerAngles.y, handle.localEulerAngles.z);

            float t = 0f;
            while (t < animationDuration)
            {
                t += Time.deltaTime;
                float fraction = t / animationDuration;
                handle.localRotation = Quaternion.Slerp(startRot, endRot, fraction);
                yield return null;
            }
            handle.localRotation = endRot;
        }

        onActivated?.Invoke();
    }
}
