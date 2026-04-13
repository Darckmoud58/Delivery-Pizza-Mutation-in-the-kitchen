using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class LogicaJugadorRed : MonoBehaviourPun, IPunObservable
{
    [Header("Stats")]
    public float velocidad = 6f;
    public int vida = 100;
    public int fuerzaAtaque = 10;
    public float radioAtaque = 1.2f;
    public Vector2 offsetAtaque = new Vector2(0.8f, 0f);

    [Header("Límites del escenario")]
    public float xMin = -9f, xMax = 9f;
    public float yMin = -4.5f, yMax = -0.5f;

    [Header("Controles")]
    public KeyCode teclaAtaque = KeyCode.J;

    // Componentes
    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;

    // Estado
    float nextAttackTime;
    bool atacando;

    // Input & movimiento (separamos lectura y física)
    Vector2 inputDir = Vector2.zero;
    const float inputDeadzone = 0.1f; // <--- deadzone para evitar input "ruidoso"

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        // Forzar estado inicial del animator para evitar "walk" stuck
        if (anim != null) anim.SetBool("isWalking", false);

        // Si no es nuestro jugador local, no simular la fisica localmente
        if (!photonView.IsMine && rb != null) rb.simulated = false;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // ----- LECTURA DE INPUT -----
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float hx = Mathf.Abs(h) > inputDeadzone ? h : 0f;
        float vy = Mathf.Abs(v) > inputDeadzone ? v : 0f;

        inputDir = new Vector2(hx, vy).normalized;

        // ---- ANIMACION: isWalking ----
        if (anim != null)
        {
            bool moviendose = inputDir.sqrMagnitude > 0.0001f && !atacando;
            anim.SetBool("isWalking", moviendose);
        }

        // ---- FLIP del sprite ----
        if (sr != null)
        {
            if (hx > 0) sr.flipX = false;
            else if (hx < 0) sr.flipX = true;
        }

        // ---- ATAQUE (configurable) ----
        if (Input.GetKeyDown(teclaAtaque) && Time.time >= nextAttackTime && !atacando)
        {
            nextAttackTime = Time.time + 0.4f;
            StartCoroutine(EjecutarAtaque());
        }

        // ---- LÍMITES aplicados en Update para evitar teletransporte visual
        Vector3 p = transform.position;
        p.x = Mathf.Clamp(p.x, xMin, xMax);
        p.y = Mathf.Clamp(p.y, yMin, yMax);
        transform.position = p;
    }

    // Aplicar la fisica en FixedUpdate (mejor para Rigidbody2D)
    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (rb == null) return;

        if (!atacando)
            rb.velocity = inputDir * velocidad;
        else
            rb.velocity = Vector2.zero;
    }

    IEnumerator EjecutarAtaque()
    {
        atacando = true;

        if (anim != null)
        {
            anim.SetBool("isWalking", false);
            anim.SetTrigger("attack");
        }

        // Espera al momento del impacto visual
        yield return new WaitForSeconds(0.15f);

        // --- Lógica de daño (OverlapCircleAll) ---
        float dirX = (sr != null && sr.flipX) ? -1f : 1f;
        Vector2 posAtaque = (Vector2)transform.position + new Vector2(offsetAtaque.x * dirX, offsetAtaque.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(posAtaque, radioAtaque);
        foreach (var hit in hits)
        {
            if (hit == null || hit.gameObject == gameObject) continue;

            if (hit.CompareTag("Enemy"))
            {
                hit.GetComponent<PhotonView>()?.RPC("RPC_RecibirDanioEnemigo", RpcTarget.All, fuerzaAtaque);
            }
            if (hit.CompareTag("Horno"))
            {
                hit.GetComponent<PhotonView>()?.RPC("RPC_RecibirDanioHorno", RpcTarget.All, fuerzaAtaque);
            }
        }

        yield return new WaitForSeconds(0.25f);
        atacando = false;
    }

    void LateUpdate()
    {
        if (sr != null)
            sr.sortingOrder = Mathf.RoundToInt(transform.position.y * -100);
    }

    [PunRPC]
    public void RPC_RecibirDanio(int danio)
    {
        vida -= danio;
        Debug.Log("¡Jugador herido! Vida restante: " + vida);

        // Efecto visual de daño (Flash Rojo)
        StartCoroutine(FlashRojo());

        if (vida <= 0)
        {
            // Aquí podrías activar una animación de muerte antes de desactivar
            gameObject.SetActive(false);
        }
    }

    IEnumerator FlashRojo()
    {
        if (sr != null)
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sr.color = Color.white;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        float dirX = sr != null && sr.flipX ? -1f : 1f;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere((Vector2)transform.position + new Vector2(offsetAtaque.x * dirX, offsetAtaque.y), radioAtaque);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting) stream.SendNext(vida);
        else vida = (int)stream.ReceiveNext();
    }
}
