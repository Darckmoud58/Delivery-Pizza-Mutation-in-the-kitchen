using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HornoController : MonoBehaviourPun, IPunObservable
{
    [Header("Vida")]
    public int vidaMaxima = 200;
    public int vidaActual;

    [Header("Spawn de enemigos")]
    public string prefabEnemigo = "Enemigo";
    public Vector2 spawnOffsetMin = new Vector2(-1.5f, -1.2f);
    public Vector2 spawnOffsetMax = new Vector2( 1.5f, -0.6f);

    [Header("Cadencia por fase (segundos)")]
    public float spawnFase0 = 5f;   // 100% → 61% vida  (normal)
    public float spawnFase1 = 3f;   // 60%  → 31% vida  (dañado)
    public float spawnFase2 = 1.5f; // 30%  → 1%  vida  (crítico)

    [Header("Enemigos por spawn")]
    public int cantidadFase0 = 1;
    public int cantidadFase2 = 2;   // En fase crítica salen de 2 en 2

    Animator anim;
    bool     destruido;
    Coroutine rutina;

    void Start()
    {
        vidaActual = vidaMaxima;
        anim = GetComponent<Animator>();

        if (PhotonNetwork.IsMasterClient)
            rutina = StartCoroutine(LoopSpawn());
    }

    // ---- LOOP DE SPAWN ----
    IEnumerator LoopSpawn()
    {
        while (!destruido)
        {
            yield return new WaitForSeconds(GetTiempoActual());

            int cantidad = GetCantidadActual();
            for (int i = 0; i < cantidad; i++)
                SpawnEnemigo();
        }
    }

    float GetTiempoActual()
    {
        float p = (float)vidaActual / vidaMaxima;
        if (p <= 0.3f) return spawnFase2;
        if (p <= 0.6f) return spawnFase1;
        return spawnFase0;
    }

    int GetCantidadActual()
    {
        float p = (float)vidaActual / vidaMaxima;
        return p <= 0.3f ? cantidadFase2 : cantidadFase0;
    }

    void SpawnEnemigo()
    {
        if (destruido) return;
        float x = Random.Range(spawnOffsetMin.x, spawnOffsetMax.x);
        float y = Random.Range(spawnOffsetMin.y, spawnOffsetMax.y);
        PhotonNetwork.Instantiate(prefabEnemigo,
            transform.position + new Vector3(x, y, 0), Quaternion.identity);
    }

    // ---- RECIBIR DAÑO ----
    [PunRPC]
    public void RPC_RecibirDanioHorno(int danio)
    {
        if (destruido) return;

        vidaActual = Mathf.Clamp(vidaActual - danio, 0, vidaMaxima);
        ActualizarAnimacion();

        if (vidaActual <= 0) Morir();
    }

    void ActualizarAnimacion()
    {
        if (anim == null) return;
        float p = (float)vidaActual / vidaMaxima;
        int fase = 0;
        if      (p <= 0.3f) fase = 2;
        else if (p <= 0.6f) fase = 1;
        anim.SetInteger("fase", fase);
    }

    void Morir()
    {
        destruido = true;
        if (anim != null) anim.SetTrigger("destruido");
        if (rutina != null) StopCoroutine(rutina);

        Debug.Log("¡HORNO DESTRUIDO! Los enemigos dejan de aparecer.");

        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DestruirDespues(2f));
    }

    IEnumerator DestruirDespues(float t)
    {
        yield return new WaitForSeconds(t);
        PhotonNetwork.Destroy(gameObject);
    }

    // ---- SINCRONIZACIÓN ----
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(vidaActual);
        else
        {
            vidaActual = (int)stream.ReceiveNext();
            ActualizarAnimacion(); // El cliente 2 también actualiza su animación
        }
    }
}
