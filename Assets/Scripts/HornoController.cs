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
    public Animator animator;                 // arrastra el Animator del prefab
    public string paramIsOpen = "abierto";    // parámetro booleano para abrir/cerrar
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
    public float intensidadSpawnMultiplier = 0.6f; // reduce tiempoEntreSpawns al intensificarse

    [Header("Estado")]
    public bool vulnerable = false;
    bool muerto = false;
    bool intensificado = false;

    Coroutine openTimer = null;
    Coroutine closedTimer = null;
    Coroutine spawnLoop = null;

    void Awake() { }

    public override void OnStartServer()
    {
        base.OnStartServer();
        vidaActual = vidaMaxima;
        vulnerable = false;
        muerto = false;
        intensificado = false;

        RpcActualizarVida(vidaActual, vidaMaxima);
        RpcSetVulnerable(vulnerable);

        if (generarEnemigos && spawnLoop == null)
            spawnLoop = StartCoroutine(LoopSpawn());
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

    public void RecibirDanio(int dmg)
    {
        if (!IsServer || muerto) return;

        if (!vulnerable)
        {
            Debug.Log("Horno: recibió ataque pero está INVULNERABLE.");
            ObserversPlayHitBlockedEffect();
            return;
        }

        vidaActual -= dmg;
        vidaActual = Mathf.Max(0, vidaActual);

        Debug.Log($"Horno: dmg {dmg}. Vida {vidaActual}/{vidaMaxima}");
        RpcActualizarVida(vidaActual, vidaMaxima);

        OnServerHit();

        if (!intensificado && puedeIntensificar && vidaActual <= vidaMaxima / 2)
            Intensify();

        if (vidaActual <= 0)
        {
            muerto = true;
            Debug.Log("Horno: Jefe derrotado (server)");
            RpcOcultarHUD();
            ObserversPlayDestroyed();
            ServerManager.Despawn(gameObject);
        }
    }

    void OnServerHit()
    {
        if (!IsServer) return;

        if (openTimer != null) { StopCoroutine(openTimer); openTimer = null; }

        ObserversPlayHitAnimation();

        SetVulnerable(false);
        ObserversPlayCloseAnimation();

        if (closedTimer != null) StopCoroutine(closedTimer);
        if (autoCycle && !muerto)
            closedTimer = StartCoroutine(ClosedCooldownAndReopen(closeCooldown));
    }

    public void AbrirHornoServidor()
    {
        if (!IsServer || muerto) return;

        if (closedTimer != null) { StopCoroutine(closedTimer); closedTimer = null; }

        ObserversPlayOpenAnimation();
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
        Debug.Log("Horno: Intensificado! tiempoEntreSpawns ahora " + tiempoEntreSpawns);
    }

    // ---- RPCs Observers (server->clientes) ----
    [ObserversRpc]
    void RpcActualizarVida(int vida, int vidaMax)
    {
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetVida(vida, vidaMax);
    }

    [ObserversRpc]
    void RpcSetVulnerable(bool v)
    {
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetInvulnerable(!v);
    }

    [ObserversRpc]
    void ObserversPlayOpenAnimation()
    {
        if (animator != null) animator.SetBool(paramIsOpen, true);
        HUD_Jefe_Sprites.Instance?.MostrarHUD();
    }

    [ObserversRpc]
    void ObserversPlayCloseAnimation()
    {
        if (animator != null) animator.SetBool(paramIsOpen, false);
    }

    [ObserversRpc]
    void ObserversPlayHitAnimation()
    {
        if (animator != null)
        {
            // En lugar de Play, disparamos el Trigger que configuraste
            animator.SetTrigger("hit");
        }
    }

    [ObserversRpc]
    void ObserversPlayHitBlockedEffect()
    {
        // sonido o efecto local si quieres
    }

    [ObserversRpc]
    void ObserversPlayDestroyed()
    {
        if (animator != null)
        {
            // En lugar de Play, disparamos el Trigger de destrucción
            animator.SetTrigger("destruido");
        }
        HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    [ObserversRpc]
    void RpcOcultarHUD()
    {
        HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    // Animation Events (opcionales)
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
}