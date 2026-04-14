using UnityEngine;
using FishNet.Object;
using System.Collections;

public class LogicaJugadorRed : NetworkBehaviour
{
    [Header("Stats")]
    public float velocidad = 6f;
    public int vida = 100;
    public int fuerzaAtaque = 10;
    public float radioAtaque = 1.2f;
    public Vector2 offsetAtaque = new Vector2(0.8f, 0f);

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    bool atacando;

    public override void OnStartClient()
    {
        base.OnStartClient();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (!IsOwner) rb.simulated = false;
    }

    void Update()
    {
        if (!IsOwner) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(h, v).normalized;

        if (!atacando)
            rb.velocity = inputDir * velocidad;
        else
            rb.velocity = Vector2.zero;

        if (anim != null) anim.SetBool("isWalking", inputDir.sqrMagnitude > 0 && !atacando);

        if (h > 0) sr.flipX = false;
        else if (h < 0) sr.flipX = true;

        if (Input.GetKeyDown(KeyCode.J) && !atacando)
        {
            ServerAtacar();
        }
    }

    [ServerRpc]
    void ServerAtacar()
    {
        ObserversAnimacionAtaque();
        StartCoroutine(EjecutarLogicaDmg());
    }

    [ObserversRpc]
    void ObserversAnimacionAtaque()
    {
        if (anim != null) anim.SetTrigger("attack");
        StartCoroutine(LockMovimiento());
    }

    IEnumerator LockMovimiento()
    {
        atacando = true;
        yield return new WaitForSeconds(0.4f);
        atacando = false;
    }

    IEnumerator EjecutarLogicaDmg()
    {
        yield return new WaitForSeconds(0.15f);
        float dirX = sr.flipX ? -1f : 1f;
        Vector2 posAtaque = (Vector2)transform.position + new Vector2(offsetAtaque.x * dirX, offsetAtaque.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(posAtaque, radioAtaque);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Enemy"))
                hit.GetComponent<IAEnemigoRed>()?.RecibirDanio(fuerzaAtaque);
            if (hit.CompareTag("Horno"))
                hit.GetComponent<HornoController>()?.RecibirDanio(fuerzaAtaque);
        }
    }

    public void RecibirDanio(int dmg)
    {
        if (!IsServer) return;
        vida -= dmg;
        ObserversEfectoDmg();
        if (vida <= 0) Despawn();
    }

    [ObserversRpc]
    void ObserversEfectoDmg()
    {
        StartCoroutine(FlashRojo());
    }

    IEnumerator FlashRojo()
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = Color.white;
    }
}