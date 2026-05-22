using UnityEngine;
using UnityEngine.Events;

public class LeverInteractable : MonoBehaviour, IInteractable
{
    public bool IsActivated { get; private set; }
    public UnityEvent onActivated = new UnityEvent();

    public bool CanInteract => !IsActivated;

    public void Interact()
    {
        if (IsActivated) return;
        IsActivated = true;

        if (transform.childCount > 0)
        {
            var handle = transform.GetChild(0);
            handle.localRotation = Quaternion.Euler(40f, handle.localEulerAngles.y, handle.localEulerAngles.z);
        }

        onActivated?.Invoke();
    }
}
