using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class HornoController : MonoBehaviourPun, IPunObservable
{
    [Header("Vida del Horno")]
    public int vidaMaxima = 200;
    public int vidaActual;

    [Header("Tiempos aleatorios de apertura")]
    public float cerradoMin = 2f;
    public float cerradoMax = 5f;
    public float abiertoMin = 1.5f;
    public float abiertoMax = 3f;

    [Header("Generador de enemigos")]
    public string prefabEnemigo = "Enemigo";
    public float spawnFase0 = 5f;   // vida > 60%
    public float spawnFase1 = 3f;   // vida 30-60%
    public float spawnFase2 = 1.5f; // vida < 30%
    public Vector2 spawnOffsetMin = new Vector2(-1.5f, -1.2f);
    public Vector2 spawnOffsetMax = new Vector2(1.5f, -0.6f);

    // Estado interno
    bool estaAbierto = false;
    bool destruido = false;

    Animator anim;
    Coroutine rutinaApertura;
    Coroutine rutinaSpawn;

    void Start()
    {
        vidaActual = vidaMaxima;
        anim = GetComponent<Animator>();

        // Solo el Master controla la lógica
        if (PhotonNetwork.IsMasterClient)
        {
            rutinaApertura = StartCoroutine(CicloAperturaRandom());
            rutinaSpawn = StartCoroutine(LoopSpawn());
        }

        ActualizarHUD();
    }

    // ─────────────────────────────────────────
    // CICLO ABIERTO / CERRADO RANDOM
    // ─────────────────────────────────────────
    IEnumerator CicloAperturaRandom()
    {
        while (!destruido)
        {
            // Cerrado por tiempo aleatorio
            float tiempoCerrado = Random.Range(cerradoMin, cerradoMax);
            photonView.RPC(nameof(RPC_CambiarEstado), RpcTarget.All, false);
            yield return new WaitForSeconds(tiempoCerrado);

            if (destruido) yield break;

            // Abierto por tiempo aleatorio
            float tiempoAbierto = Random.Range(abiertoMin, abiertoMax);
            photonView.RPC(nameof(RPC_CambiarEstado), RpcTarget.All, true);
            yield return new WaitForSeconds(tiempoAbierto);
        }
    }

    [PunRPC]
    void RPC_CambiarEstado(bool abierto)
    {
        estaAbierto = abierto;

        if (anim != null)
            anim.SetBool("abierto", estaAbierto);

        Debug.Log("Horno: " + (estaAbierto ? "ABIERTO - vulnerable" : "CERRADO - invulnerable"));

        [PunRPC]
        void RPC_CambiarEstado(bool abierto)
        {
            estaAbierto = abierto;
            if (anim != null)
                anim.SetBool("abierto", estaAbierto);

            Debug.Log("RPC recibido - Horno: " + (estaAbierto ? "ABIERTO" : "CERRADO") + " | anim null? " + (anim == null));
        }
    }

    // ─────────────────────────────────────────
    // SPAWN DE ENEMIGOS POR FASE
    // ─────────────────────────────────────────
    IEnumerator LoopSpawn()
    {
        while (!destruido)
        {
            yield return new WaitForSeconds(GetTiempoSpawn());
            SpawnEnemigo();
        }
    }

    float GetTiempoSpawn()
    {
        float p = (float)vidaActual / vidaMaxima;
        if (p <= 0.3f) return spawnFase2;
        if (p <= 0.6f) return spawnFase1;
        return spawnFase0;
    }

    void SpawnEnemigo()
    {
        if (destruido) return;
        float x = Random.Range(spawnOffsetMin.x, spawnOffsetMax.x);
        float y = Random.Range(spawnOffsetMin.y, spawnOffsetMax.y);
        Vector3 pos = transform.position + new Vector3(x, y, 0f);
        PhotonNetwork.Instantiate(prefabEnemigo, pos, Quaternion.identity);
    }

    // ─────────────────────────────────────────
    // RECIBIR DAÑO
    // ─────────────────────────────────────────
    [PunRPC]
    public void RPC_RecibirDanioHorno(int danio)
    {
        if (destruido) return;

        // Si está cerrado, no recibe daño
        if (!estaAbierto)
        {
            Debug.Log("Horno cerrado. Sin daño.");
            return;
        }

        vidaActual = Mathf.Clamp(vidaActual - danio, 0, vidaMaxima);
        Debug.Log("Horno golpeado. Vida: " + vidaActual);

        ActualizarFaseAnimacion();
        ActualizarHUD();

        if (vidaActual <= 0)
            Morir();
    }

    // ─────────────────────────────────────────
    // FASES DE DAÑO (animación)
    // ─────────────────────────────────────────
    void ActualizarFaseAnimacion()
    {
        if (anim == null) return;

        float p = (float)vidaActual / vidaMaxima;
        int fase = 0;
        if (p <= 0.3f) fase = 2;
        else if (p <= 0.6f) fase = 1;

        anim.SetInteger("fase", fase);
    }

    // ─────────────────────────────────────────
    // MUERTE
    // ─────────────────────────────────────────
    void Morir()
    {
        destruido = true;

        if (rutinaApertura != null) StopCoroutine(rutinaApertura);
        if (rutinaSpawn != null) StopCoroutine(rutinaSpawn);

        if (anim != null) anim.SetTrigger("destruido");

        if (HUD_Jefe_Sprites.Instance != null)
            HUD_Jefe_Sprites.Instance.OcultarHUD();

        Debug.Log("¡HORNO DESTRUIDO!");

        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(DestruirDespues(2f));
    }

    IEnumerator DestruirDespues(float t)
    {
        yield return new WaitForSeconds(t);
        PhotonNetwork.Destroy(gameObject);
    }

    // ─────────────────────────────────────────
    // HUD
    // ─────────────────────────────────────────
    void ActualizarHUD()
    {
        if (HUD_Jefe_Sprites.Instance != null)
            HUD_Jefe_Sprites.Instance.SetVida(vidaActual, vidaMaxima);
    }

    // ─────────────────────────────────────────
    // SINCRONIZACIÓN PHOTON
    // ─────────────────────────────────────────
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(vidaActual);
            stream.SendNext(estaAbierto);
            stream.SendNext(destruido);
        }
        else
        {
            vidaActual = (int)stream.ReceiveNext();
            estaAbierto = (bool)stream.ReceiveNext();
            destruido = (bool)stream.ReceiveNext();

            ActualizarFaseAnimacion();

            if (anim != null)
                anim.SetBool("abierto", estaAbierto);
        }
    }
}
