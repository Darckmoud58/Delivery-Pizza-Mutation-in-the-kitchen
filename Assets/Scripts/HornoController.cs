using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;

public class HornoController : NetworkBehaviour
{
    [SyncVar(OnChange = nameof(OnVidaChanged))]
    public int vidaActual = 200;
    public int vidaMaxima = 200;

    public GameObject prefabEnemigo; // Cambiado de string a GameObject

    void Start()
    {
        if (IsServer)
        {
            vidaActual = vidaMaxima;
            StartCoroutine(LoopSpawn());
        }
    }

    IEnumerator LoopSpawn()
    {
        while (vidaActual > 0)
        {
            yield return new WaitForSeconds(3f);
            GameObject go = Instantiate(prefabEnemigo, transform.position + Vector3.down, Quaternion.identity);
            ServerManager.Spawn(go);
        }
    }

    public void RecibirDanio(int dmg)
    {
        if (!IsServer) return;
        vidaActual -= dmg;
        if (vidaActual <= 0) Despawn();
    }

    void OnVidaChanged(int prev, int next, bool asServer)
    {
        // Aquí actualizas tu barra de vida de la HUD
    }
}