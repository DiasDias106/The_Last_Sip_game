using UnityEngine;

public class RotacionarPortal : MonoBehaviour
{
    // Usamos apenas um número simples agora. 
    // Mude no Inspector para deixar mais rápido ou mais devagar.
    public float velocidadeX = -100f;

    void Update()
    {
        // Força a rotação estritamente no eixo X. Os eixos Y e Z ficam travados em zero.
        transform.Rotate(velocidadeX * Time.deltaTime, 0f, 0f);
    }
}