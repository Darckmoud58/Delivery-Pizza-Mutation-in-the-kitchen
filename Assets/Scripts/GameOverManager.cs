using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public void VolverAlMenu()
    {
        // Esto recarga la escena actual para reiniciar todo
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}