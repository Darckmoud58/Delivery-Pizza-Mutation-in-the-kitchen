using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class IAEnemigoRed : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Stats")]
    public float velocidad          = 3f;
    public float distanciaDeteccion = 10f;
    public float distanciaAtaque    = 1.1f;
    public int   vida               = 30;
    public int   danioAlJugador     = 5;
    public float cooldownAtaque     = 1.5f;

    Rigidbody2D    rb;
    SpriteRenderer sr;
    Animator       anim;
    Transform      objetivo;
    float          nextAttackTime;

    void Awake()
    {
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
    }
    // Start is called before the first frame update
    void Start()
    {
       // Solo el Master mueve a los enemigos
        if (!PhotonNetwork.IsMasterClient)
            rb.simulated = false;
    }

    // Update is called once per frame
    void Update()
    {
       if (!PhotonNetwork.IsMasterClient) return;

        BuscarObjetivo();
        Mover();
    }

    void BuscarObjetivo()
    {
        GameObject[] jugadores = GameObject.FindGameObjectsWithTag("Player");
        float mejor = Mathf.Infinity;
        objetivo = null;

        foreach (var j in jugadores)
        {
            float d = Vector2.Distance(transform.position, j.transform.position);
            if (d < mejor && d <= distanciaDeteccion)
            {
                mejor    = d;
                objetivo = j.transform;
            }
        }
    }

    void Mover()
    {
        if (objetivo == null) { rb.velocity = Vector2.zero; return; }

        float dist = Vector2.Distance(transform.position, objetivo.position);

        if (dist > distanciaAtaque)
        {
            // Perseguir
            Vector2 dir = (objetivo.position - transform.position).normalized;
            rb.velocity = dir * velocidad;

            if (anim != null) anim.SetBool("isWalking", true);
            if (sr != null)
            {
                if (rb.velocity.x > 0) sr.flipX = false;
                else if (rb.velocity.x < 0) sr.flipX = true;
            }
        }
        else
        {
            // Atacar al jugador
            rb.velocity = Vector2.zero;
            if (anim != null) anim.SetBool("isWalking", false);

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + cooldownAtaque;
                if (anim != null) anim.SetTrigger("attack");

                PhotonView pvJugador = objetivo.GetComponent<PhotonView>();
                if (pvJugador != null)
                    pvJugador.RPC("RPC_RecibirDanio", RpcTarget.All, danioAlJugador);
            }
        }
    }

    [PunRPC]
    public void RPC_RecibirDanioEnemigo(int danio)
    {
        vida -= danio;
        Debug.Log("Enemigo golpeado! Vida restante: " + vida);

        if (vida <= 0)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.Destroy(gameObject);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(vida);
        else vida = (int)stream.ReceiveNext();
    }
}
