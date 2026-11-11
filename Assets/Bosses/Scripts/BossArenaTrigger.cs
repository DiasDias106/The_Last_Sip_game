using UnityEngine;

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    [SerializeField] private GameObject[] arenaWalls;

    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsSortMode.None))
            boss.ActivateArena();

        foreach (var boss in Object.FindObjectsByType<PaladinFinalBossController>(FindObjectsSortMode.None))
            boss.ActivateArena();

        if (arenaWalls != null)
            foreach (var wall in arenaWalls)
                if (wall != null) wall.SetActive(true);

        gameObject.SetActive(false);
    }
}
