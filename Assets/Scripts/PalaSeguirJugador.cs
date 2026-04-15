using UnityEngine;

public class PalaSeguirJugador : MonoBehaviour
{
    public Transform jugador; // Transform del jugador local
    public Vector3 offset = new Vector3(1.5f, 0f, 0f); // Ajusta el offset para que la pala no tape al jugador

    void Update()
    {
        if (jugador != null)
        {
            transform.position = jugador.position + offset;
        }
    }
}