using UnityEngine;
public class RotacionarPortal : MonoBehaviour
{
    public float velocidadeX = -100f;
    void Update()
    {
        transform.Rotate(velocidadeX * Time.deltaTime, 0f, 0f);
    }
}