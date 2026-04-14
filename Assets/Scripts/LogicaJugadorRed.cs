using UnityEngine;
using FishNet.Object;
using System.Collections;

public class LogicaJugadorRed : NetworkBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 6f;

    [Header("Vida")]
    public int vidaMax = 100;
    public int vida = 100;

    [Header("Ataque")]
    public int fuerzaAtaque = 10;
    public float radioAtaque = 1.2f;
    public Vector2 offsetAtaque = new Vector2(0.8f, 0f);
    public float tiempoGolpe = 0.15f;
    public float duracionAtaque = 0.35f;
    public float cooldownAtaque = 0.45f;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    Collider2D col;

    bool atacando = false;
    bool muerto = false;
    float siguienteAtaque = 0f;

    Vector2 ultimaDireccion = Vector2.right;
    Vector2 ultimoInputEnviado = new Vector2(999, 999);
    Vector2 ultimaDireccionEnviada = new Vector2(999, 999);

    public bool EstaMuerto => muerto;

    public override void OnStartClient()
    {
        base.OnStartClient();

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (!IsOwner && rb != null)
            rb.simulated = false;

        if (IsOwner && HUD_Jugador.Instance != null)
            HUD_Jugador.Instance.ActualizarVida(vida, vidaMax);

        // Si tienes un panel de Game Over en escena y lo dejaste activo por error, lo apagamos.
        if (IsOwner)
        {
            GameObject go = BuscarObjetoInactivo("GameOverPanel");
            if (go != null) go.SetActive(false);
        }
    }

    void Update()
    {
        if (!IsOwner || muerto) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector2 inputDir = new Vector2(h, v).normalized;

        if (inputDir != Vector2.zero)
            ultimaDireccion = inputDir;

        if (!atacando)
        {
            if (rb != null)
                rb.velocity = inputDir * velocidad;
        }
        else
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
        }

        if (sr != null && Mathf.Abs(ultimaDireccion.x) > 0.01f)
            sr.flipX = ultimaDireccion.x < 0f;

        AplicarAnimacionMovimiento(inputDir, atacando);

        // Sincroniza animación a los demás clientes solo cuando cambia.
        if (Cambio(inputDir, ultimoInputEnviado) || Cambio(ultimaDireccion, ultimaDireccionEnviada))
        {
            ultimoInputEnviado = inputDir;
            ultimaDireccionEnviada = ultimaDireccion;
            ServerActualizarMovimiento(inputDir, ultimaDireccion);
        }

        if (Input.GetKeyDown(KeyCode.J) && !atacando && Time.time >= siguienteAtaque)
        {
            siguienteAtaque = Time.time + cooldownAtaque;
            StartCoroutine(RutinaAtaqueLocal(ultimaDireccion));
            ServerAtacar(ultimaDireccion);
        }
    }

    bool Cambio(Vector2 a, Vector2 b)
    {
        return (a - b).sqrMagnitude > 0.0001f;
    }

    [ServerRpc]
    void ServerActualizarMovimiento(Vector2 inputDir, Vector2 dirMirada)
    {
        ObserversActualizarMovimiento(inputDir, dirMirada);
    }

    [ObserversRpc]
    void ObserversActualizarMovimiento(Vector2 inputDir, Vector2 dirMirada)
    {
        if (IsOwner || muerto) return;

        if (sr != null && Mathf.Abs(dirMirada.x) > 0.01f)
            sr.flipX = dirMirada.x < 0f;

        if (!atacando)
            AplicarAnimacionMovimiento(inputDir, false);
    }

    IEnumerator RutinaAtaqueLocal(Vector2 dir)
    {
        atacando = true;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (sr != null && Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0f;

        AplicarAnimacionMovimiento(Vector2.zero, true);
        DispararAnimacionAtaque();

        yield return new WaitForSeconds(duracionAtaque);

        atacando = false;
        AplicarAnimacionMovimiento(Vector2.zero, false);
    }

    IEnumerator RutinaAtaqueRemoto(Vector2 dir)
    {
        atacando = true;

        if (sr != null && Mathf.Abs(dir.x) > 0.01f)
            sr.flipX = dir.x < 0f;

        AplicarAnimacionMovimiento(Vector2.zero, true);
        DispararAnimacionAtaque();

        yield return new WaitForSeconds(duracionAtaque);

        atacando = false;
        AplicarAnimacionMovimiento(Vector2.zero, false);
    }

    [ServerRpc]
    void ServerAtacar(Vector2 dir)
    {
        ObserversReproducirAtaque(dir);
        StartCoroutine(EjecutarDanio(dir));
    }

    [ObserversRpc]
    void ObserversReproducirAtaque(Vector2 dir)
    {
        if (IsOwner || muerto) return;
        StartCoroutine(RutinaAtaqueRemoto(dir));
    }

    IEnumerator EjecutarDanio(Vector2 dir)
    {
        yield return new WaitForSeconds(tiempoGolpe);

        if (dir == Vector2.zero)
            dir = Vector2.right;

        Vector2 dirNormalizada = dir.normalized;
        Vector2 posAtaque = (Vector2)transform.position +
                            new Vector2(offsetAtaque.x * Mathf.Sign(dirNormalizada.x == 0 ? 1 : dirNormalizada.x), offsetAtaque.y);

        Collider2D[] hits = Physics2D.OverlapCircleAll(posAtaque, radioAtaque);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            IAEnemigoRed enemigo = hit.GetComponent<IAEnemigoRed>();
            if (enemigo != null)
            {
                enemigo.RecibirDanio(fuerzaAtaque);
                continue;
            }

            HornoController horno = hit.GetComponent<HornoController>();
            if (horno != null)
            {
                horno.RecibirDanio(fuerzaAtaque);
            }
        }
    }

    public void RecibirDanio(int dmg)
    {
        if (!IsServerInitialized || muerto) return;

        vida -= dmg;
        if (vida < 0) vida = 0;

        RpcActualizarVida(vida);

        if (vida <= 0)
        {
            muerto = true;
            RpcMorir();
        }
        else
        {
            // Empujón leve hacia arriba para que no se quede trabado con el enemigo
            rb.velocity = Vector2.up * 2f;
        }
    }

    [ObserversRpc]
    void RpcActualizarVida(int nuevaVida)
    {
        vida = nuevaVida;

        if (sr != null)
            StartCoroutine(FlashRojo());

        if (IsOwner && HUD_Jugador.Instance != null)
            HUD_Jugador.Instance.ActualizarVida(vida, vidaMax);
    }

    [ObserversRpc]
    void RpcMorir()
    {
        muerto = true;
        atacando = false;

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        AplicarAnimacionMovimiento(Vector2.zero, false);
        DispararAnimacionMuerte();

        if (IsOwner)
        {
            GameObject panel = BuscarObjetoInactivo("GameOverPanel");
            if (panel != null)
                panel.SetActive(true);
        }
    }

    IEnumerator FlashRojo()
    {
        if (sr == null) yield break;

        sr.color = Color.red;
        yield return new WaitForSeconds(0.15f); // Un poquito más de tiempo
        sr.color = Color.white; // Forzamos a que vuelva a blanco
    }

    void AplicarAnimacionMovimiento(Vector2 inputDir, bool enAtaque)
    {
        if (anim == null) return;

        float speed = inputDir.magnitude;

        SetBoolIfExists("isWalking", speed > 0.01f && !enAtaque);
        SetBoolIfExists("IsWalking", speed > 0.01f && !enAtaque);
        SetBoolIfExists("run", speed > 0.01f && !enAtaque);
        SetBoolIfExists("Run", speed > 0.01f && !enAtaque);
        SetBoolIfExists("corriendo", speed > 0.01f && !enAtaque);
        SetBoolIfExists("Corriendo", speed > 0.01f && !enAtaque);

        SetBoolIfExists("idle", speed <= 0.01f && !enAtaque && !muerto);
        SetBoolIfExists("Idle", speed <= 0.01f && !enAtaque && !muerto);
        SetBoolIfExists("quieto", speed <= 0.01f && !enAtaque && !muerto);
        SetBoolIfExists("Quieto", speed <= 0.01f && !enAtaque && !muerto);

        SetBoolIfExists("isAttacking", enAtaque);
        SetBoolIfExists("IsAttacking", enAtaque);
        SetBoolIfExists("attacking", enAtaque);
        SetBoolIfExists("Atacando", enAtaque);
        SetBoolIfExists("atacando", enAtaque);

        SetFloatIfExists("Speed", speed);
        SetFloatIfExists("speed", speed);
        SetFloatIfExists("Horizontal", inputDir.x);
        SetFloatIfExists("horizontal", inputDir.x);
        SetFloatIfExists("Vertical", inputDir.y);
        SetFloatIfExists("vertical", inputDir.y);
        SetFloatIfExists("MoveX", inputDir.x);
        SetFloatIfExists("MoveY", inputDir.y);
        SetFloatIfExists("VelX", inputDir.x);
        SetFloatIfExists("VelY", inputDir.y);
    }

    void DispararAnimacionAtaque()
    {
        if (anim == null) return;

        SetTriggerIfExists("attack");
        SetTriggerIfExists("Attack");
        SetTriggerIfExists("ataque");
        SetTriggerIfExists("Ataque");
        SetTriggerIfExists("atacar");
        SetTriggerIfExists("Atacar");
    }

    void DispararAnimacionMuerte()
    {
        if (anim == null) return;

        SetTriggerIfExists("death");
        SetTriggerIfExists("Death");
        SetTriggerIfExists("die");
        SetTriggerIfExists("Die");
        SetTriggerIfExists("muerte");
        SetTriggerIfExists("Muerte");
        SetTriggerIfExists("muerto");
        SetTriggerIfExists("Muerto");

        SetBoolIfExists("dead", true);
        SetBoolIfExists("Dead", true);
        SetBoolIfExists("muerto", true);
        SetBoolIfExists("Muerto", true);
    }

    void SetBoolIfExists(string paramName, bool value)
    {
        if (anim == null) return;
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].name == paramName && anim.parameters[i].type == AnimatorControllerParameterType.Bool)
            {
                anim.SetBool(paramName, value);
                return;
            }
        }
    }

    void SetFloatIfExists(string paramName, float value)
    {
        if (anim == null) return;
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].name == paramName && anim.parameters[i].type == AnimatorControllerParameterType.Float)
            {
                anim.SetFloat(paramName, value);
                return;
            }
        }
    }

    void SetTriggerIfExists(string paramName)
    {
        if (anim == null) return;
        for (int i = 0; i < anim.parameters.Length; i++)
        {
            if (anim.parameters[i].name == paramName && anim.parameters[i].type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(paramName);
                return;
            }
        }
    }

    GameObject BuscarObjetoInactivo(string nombre)
    {
        Transform[] todos = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in todos)
        {
            if (t.name == nombre)
                return t.gameObject;
        }
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector2 dir = ultimaDireccion == Vector2.zero ? Vector2.right : ultimaDireccion.normalized;
        Vector2 posAtaque = (Vector2)transform.position +
                            new Vector2(offsetAtaque.x * Mathf.Sign(dir.x == 0 ? 1 : dir.x), offsetAtaque.y);

        Gizmos.DrawWireSphere(posAtaque, radioAtaque);
    }
}