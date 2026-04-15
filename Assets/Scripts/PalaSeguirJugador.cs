// Assets/Scripts/PalaSeguirJugador.cs
using UnityEngine;

public class PalaSeguirJugador : MonoBehaviour
{
    [Tooltip("Transform del jugador local al que seguir")]
    public Transform jugador;

    [Tooltip("Offset en world space relativo al jugador")]
    public Vector3 offset = new Vector3(1.2f, 0.2f, 0f);

    void Update()
    {
        if (jugador != null)
            transform.position = jugador.position + offset;
    }
}