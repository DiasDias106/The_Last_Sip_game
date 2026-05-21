using UnityEngine;

public class MemoryPickup : MonoBehaviour
{
    [HideInInspector] public int videoIndex = 1;

    [Header("Float Animation")]
    public float floatSpeed  = 1.5f;
    public float floatHeight = 0.25f;
    public float rotateSpeed = 50f;

    private Vector3 basePosition;

    private bool isPlayerNear = false;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        transform.position = basePosition + Vector3.up * (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Auto-spawn the video player manager if not present in scene
        if (MemoryVideoPlayer.Instance == null)
        {
            var prefab = Resources.Load<GameObject>("MemoryVideoPlayer");
            if (prefab != null)
                Instantiate(prefab);
            else
                new GameObject("MemoryVideoPlayer").AddComponent<MemoryVideoPlayer>();
        }

        MemoryVideoPlayer.Instance?.PlayVideo(videoIndex);
        Destroy(gameObject);
    }
}
