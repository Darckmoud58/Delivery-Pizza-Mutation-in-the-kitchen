// Assets/Scripts/HornoController.cs
using System.Collections;
using UnityEngine;
using FishNet.Object;

public class HornoController : NetworkBehaviour
{
    [Header("Vida")]
    public int vidaMaxima = 200;
    [HideInInspector] public int vidaActual = 200;

    [Header("Spawns")]
    public GameObject prefabEnemigo;
    public float tiempoEntreSpawns = 4f;
    public float radioSpawn = 1.5f;
    public bool generarEnemigos = true;
    public int spawnCount = 1;
    public int spawnCountIntenso = 2;

    [Header("Animaciones / Timing")]
    public Animator animator;                 // arrastra el Animator del prefab (o se buscará en Awake)
    public string paramIsOpen = "abierto";    // parámetro booleano para abrir/cerrar
    public string triggerHit = "hit";         // trigger para anim dañado
    public string triggerDestroyed = "destruido";

    // Nombres de estados (opcional, usados para Play si lo prefieres)
    public string stateHitName = "Horno_Dañado";
    public string stateClosedName = "Horno_Cerrado";
    public string stateOpenName = "Horno_Abierto";
    public string stateIdleName = "Horno_Idle";
    public string stateDestroyedName = "Horno_Destruido";

    [Tooltip("Tiempo que el horno permanece abierto (vulnerable) si no recibe hits)")]
    public float openWindowDuration = 5f;

    [Tooltip("Tiempo que el horno permanece cerrado (invulnerable) antes de reabrir")]
    public float closeCooldown = 3f;

    [Tooltip("Si true, el horno seguirá ciclando abrir->cerrar automáticamente")]
    public bool autoCycle = true;

    [Header("Intensificación")]
    public bool puedeIntensificar = true;
    [Tooltip("Multiplicador aplicado al tiempoEntreSpawns cuando se intensifica (menor = más rápido)")]
    public float intensidadSpawnMultiplier = 0.6f;

    [Header("Estado")]
    public bool vulnerable = false; // estado del horno (server authoritative)
    bool muerto = false;
    bool intensificado = false;

    Coroutine openTimer = null;
    Coroutine closedTimer = null;
    Coroutine spawnLoop = null;

    void Awake()
    {
        // Si no arrastraste el animator en el inspector, intenta obtenerlo del mismo GameObject
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        vidaActual = vidaMaxima;
        vulnerable = false;
        muerto = false;
        intensificado = false;

        // Notificar estado inicial a clientes
        RpcActualizarVida(vidaActual, vidaMaxima);
        RpcSetVulnerable(vulnerable);

        // arrancar loop de spawn en servidor
        if (generarEnemigos && spawnLoop == null)
            spawnLoop = StartCoroutine(LoopSpawn());

        Debug.Log("[Horno][Server] OnStartServer: horno inicializado.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Al inicio del cliente, asegurarnos de que la HUD esté oculta (será mostrada por la RPC cuando abra)
        HUD_Jefe_Sprites.Instance?.OcultarHUD();

        // Sincronizar estado visual del animator con el booleano 'vulnerable' si el animator existe
        if (animator != null)
            animator.SetBool(paramIsOpen, vulnerable);

        Debug.Log("[Horno][Client] OnStartClient: animator asignado? " + (animator != null) + " vulnerable=" + vulnerable);
    }

    IEnumerator LoopSpawn()
    {
        while (!muerto && vidaActual > 0)
        {
            float actualTiempo = tiempoEntreSpawns;
            int actualCount = intensificado ? spawnCountIntenso : spawnCount;

            yield return new WaitForSeconds(actualTiempo);

            if (prefabEnemigo == null || muerto) continue;

            for (int i = 0; i < actualCount; i++)
            {
                Vector3 pos = transform.position + new Vector3(Random.Range(-radioSpawn, radioSpawn), -1f, 0f);
                GameObject go = Instantiate(prefabEnemigo, pos, Quaternion.identity);
                ServerManager.Spawn(go);
            }
        }
    }

    // Llamar desde la lógica de ataque (debe invocarse en server)
    public void RecibirDanio(int dmg)
    {
        if (!IsServer || muerto) return;

        if (!vulnerable)
        {
            Debug.Log("[Horno] recibió ataque pero está INVULNERABLE.");
            ObserversPlayHitBlockedEffect();
            return;
        }

        vidaActual -= dmg;
        vidaActual = Mathf.Max(0, vidaActual);

        Debug.Log($"[Horno] dmg {dmg}. Vida {vidaActual}/{vidaMaxima}");
        RpcActualizarVida(vidaActual, vidaMaxima);

        // Reacción al hit (se cierra)
        OnServerHit();

        // Intensificar si baja a la mitad
        if (!intensificado && puedeIntensificar && vidaActual <= vidaMaxima / 2)
            Intensify();

        if (vidaActual <= 0)
        {
            muerto = true;
            Debug.Log("[Horno] Jefe derrotado (server)");
            RpcOcultarHUD();
            ObserversPlayDestroyed();
            ServerManager.Despawn(gameObject);
        }
    }

    void OnServerHit()
    {
        if (!IsServer) return;

        // cancelar ventana abierta
        if (openTimer != null) { StopCoroutine(openTimer); openTimer = null; }

        // reproducir anim de hit en clientes
        ObserversPlayHitAnimation();

        // cerrar e invulnerabilizar
        SetVulnerable(false);
        ObserversPlayCloseAnimation();

        // iniciar cooldown para reabrir si corresponde
        if (closedTimer != null) StopCoroutine(closedTimer);
        if (autoCycle && !muerto)
            closedTimer = StartCoroutine(ClosedCooldownAndReopen(closeCooldown));
    }

    // Llamar desde ControlNivel en servidor cuando quieras abrir el horno por primera vez
    public void AbrirHornoServidor()
    {
        if (!IsServer || muerto) return;

        Debug.Log("[Horno] AbrirHornoServidor llamado.");

        if (closedTimer != null) { StopCoroutine(closedTimer); closedTimer = null; }

        ObserversPlayOpenAnimation(); // notificar clientes para anim y HUD
        SetVulnerable(true);

        if (openTimer != null) StopCoroutine(openTimer);
        openTimer = StartCoroutine(OpenWindowTimer(openWindowDuration));
    }

    IEnumerator OpenWindowTimer(float secs)
    {
        float t = 0f;
        while (t < secs)
        {
            if (muerto) yield break;
            t += Time.deltaTime;
            yield return null;
        }

        if (!muerto)
        {
            SetVulnerable(false);
            ObserversPlayCloseAnimation();
            if (autoCycle)
                closedTimer = StartCoroutine(ClosedCooldownAndReopen(closeCooldown));
        }
        openTimer = null;
    }

    IEnumerator ClosedCooldownAndReopen(float secs)
    {
        yield return new WaitForSeconds(secs);
        if (muerto) yield break;
        if (autoCycle)
        {
            ObserversPlayOpenAnimation();
            SetVulnerable(true);
            if (openTimer != null) StopCoroutine(openTimer);
            openTimer = StartCoroutine(OpenWindowTimer(openWindowDuration));
        }
        closedTimer = null;
    }

    void Intensify()
    {
        intensificado = true;
        tiempoEntreSpawns *= intensidadSpawnMultiplier;
        openWindowDuration = Mathf.Max(1f, openWindowDuration * 0.75f);
        Debug.Log("[Horno] Intensificado! tiempoEntreSpawns ahora " + tiempoEntreSpawns);
    }

    // ---- RPCs Observers (server->clientes) ----
    [ObserversRpc]
    void RpcActualizarVida(int vida, int vidaMax)
    {
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetVida(vida, vidaMax);
        else Debug.Log("[Horno][Client] RpcActualizarVida: HUD_Jefe_Sprites no encontrado");
    }

    [ObserversRpc]
    void RpcSetVulnerable(bool v)
    {
        // HUD muestra invulnerabilidad (por ejemplo cambiar color)
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetInvulnerable(!v);
        // sincronizar parámetro animator en cliente
        if (animator != null) animator.SetBool(paramIsOpen, v);
        Debug.Log("[Horno][Client] RpcSetVulnerable: v=" + v + " animator? " + (animator != null));
    }

    [ObserversRpc]
    void ObserversPlayOpenAnimation()
    {
        Debug.Log("[Horno][Client] ObserversPlayOpenAnimation llamado");
        if (animator != null)
        {
            // SetBool para abrir (transiciones en Animator deben usar 'abierto' bool)
            animator.SetBool(paramIsOpen, true);
            // opcional: asegurar reproducción del estado
            // animator.Play(stateOpenName);
        }

        // Mostrar HUD cuando abra (si HUD root estaba activo y fondo desactivado)
        HUD_Jefe_Sprites.Instance?.MostrarHUD();
    }

    [ObserversRpc]
    void ObserversPlayCloseAnimation()
    {
        Debug.Log("[Horno][Client] ObserversPlayCloseAnimation llamado");
        if (animator != null)
        {
            animator.SetBool(paramIsOpen, false);
            // animator.Play(stateClosedName);
        }
    }

    [ObserversRpc]
    void ObserversPlayHitAnimation()
    {
        Debug.Log("[Horno][Client] ObserversPlayHitAnimation llamado");
        if (animator != null)
        {
            // usar trigger 'hit' para la animación de daño
            animator.SetTrigger(triggerHit);
            // si prefieres Play: animator.Play(stateHitName);
        }
    }

    [ObserversRpc]
    void ObserversPlayHitBlockedEffect()
    {
        // efecto local cuando golpean y está invulnerable (sonido, partículas...)
        // implementa VFX o audio aquí si quieres
    }

    [ObserversRpc]
    void ObserversPlayDestroyed()
    {
        Debug.Log("[Horno][Client] ObserversPlayDestroyed llamado");
        if (animator != null)
        {
            animator.SetTrigger(triggerDestroyed);
            // animator.Play(stateDestroyedName);
        }
        HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    [ObserversRpc]
    void RpcOcultarHUD()
    {
        HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    // Animation Events (opcionales): si usas Animation Events en clips
    public void OnOpenAnimationEvent_Server()
    {
        if (IsServer) SetVulnerable(true);
    }
    public void OnCloseAnimationEvent_Server()
    {
        if (IsServer) SetVulnerable(false);
    }

    void SetVulnerable(bool v)
    {
        if (!IsServer) return;
        vulnerable = v;
        RpcSetVulnerable(v);
    }

    // Limpieza por si se destruye
    public override void OnStopServer()
    {
        base.OnStopServer();
        if (spawnLoop != null) StopCoroutine(spawnLoop);
        if (openTimer != null) StopCoroutine(openTimer);
        if (closedTimer != null) StopCoroutine(closedTimer);
    }



#if UNITY_EDITOR
    [UnityEngine.ContextMenu("Debug: Abrir horno (si soy server)")]
    public void DebugAbrirHorno()
    {
        if (IsServer)
        {
            Debug.Log("[DEBUG] Forzando AbrirHornoServidor() (server)");
            AbrirHornoServidor();
        }
        else
        {
            Debug.Log("[DEBUG] No soy server; no puedo abrir.");
        }
    }
#endif
}