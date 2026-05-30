using UnityEngine;

// Coloca este script num trigger box que cobre a entrada da arena do boss.
// Quando o player entra, o boss começa a detetar/perseguir.
[RequireComponent(typeof(Collider))]
public class BossArenaTrigger : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var boss in Object.FindObjectsByType<BossController>(FindObjectsSortMode.None))
            boss.ActivateArena();

        // Trigger só ativa uma vez
        gameObject.SetActive(false);
    }
}
