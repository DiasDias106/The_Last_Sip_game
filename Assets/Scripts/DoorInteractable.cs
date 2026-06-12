using UnityEngine;
public class DoorInteractable : MonoBehaviour
{
    [SerializeField] private string doorGroupId = "level1_main_gate";
    [SerializeField] private float interactionDistance = 2.3f;
    [SerializeField] private float openAngleY = -95f;
    [SerializeField] private float openSpeed = 3.5f;
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 1.7f, 0f);
    [SerializeField] private bool disableCollidersWhenOpen = true;
    private bool _isOpen;
    private bool _lockedClosed;
    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private Collider[] _ownColliders;
    public float InteractionDistance => interactionDistance;
    public Vector3 PromptWorldPosition => transform.position + promptOffset;
    public string DoorGroupId => doorGroupId;
    private void Awake()
    {
        _closedRotation = transform.rotation;
        _openRotation = _closedRotation * Quaternion.Euler(0f, openAngleY, 0f);
        _ownColliders = GetComponents<Collider>();
        SetColliderState(isOpen: false);
    }
    private void Update()
    {
        Quaternion target = _isOpen ? _openRotation : _closedRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * openSpeed);
    }
    public void Interact()
    {
        if (_lockedClosed)
        {
            return;
        }
        ToggleGroup(doorGroupId);
    }
    public void ForceCloseAndLock()
    {
        SetGroupState(doorGroupId, open: false, lockClosed: true);
    }
    private static void ToggleGroup(string groupId)
    {
        DoorInteractable[] doors = Object.FindObjectsOfType<DoorInteractable>(true);
        bool hasDoorInGroup = false;
        bool anyOpen = false;
        for (int i = 0; i < doors.Length; i++)
        {
            DoorInteractable d = doors[i];
            if (d == null || !d.isActiveAndEnabled || d.doorGroupId != groupId)
            {
                continue;
            }
            hasDoorInGroup = true;
            if (d._isOpen)
            {
                anyOpen = true;
                break;
            }
        }
        if (!hasDoorInGroup)
        {
            return;
        }
        bool targetOpen = !anyOpen;
        SetGroupState(groupId, targetOpen, lockClosed: false);
    }
    private static void SetGroupState(string groupId, bool open, bool lockClosed)
    {
        DoorInteractable[] doors = Object.FindObjectsOfType<DoorInteractable>(true);
        for (int i = 0; i < doors.Length; i++)
        {
            DoorInteractable d = doors[i];
            if (d == null || !d.isActiveAndEnabled || d.doorGroupId != groupId)
            {
                continue;
            }
            d._lockedClosed = lockClosed;
            d.SetOpen(lockClosed ? false : open);
        }
    }
    private void SetOpen(bool open)
    {
        _isOpen = open;
        SetColliderState(_isOpen);
    }
    private void SetColliderState(bool isOpen)
    {
        if (!disableCollidersWhenOpen || _ownColliders == null)
        {
            return;
        }
        for (int i = 0; i < _ownColliders.Length; i++)
        {
            Collider c = _ownColliders[i];
            if (c == null)
            {
                continue;
            }
            c.enabled = !isOpen;
        }
    }
}
