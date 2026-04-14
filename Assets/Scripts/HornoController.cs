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

    [Header("Animaciones / Timing")]
    public Animator animator;                 // arrastra el Animator del prefab
    public string paramIsOpen = "isOpen";     // si usas un parámetro booleano
    public float duracionAbrirAnim = 1.0f;    // duración aproximada de la animación "abrir"

    [Header("Estado")]
    public bool vulnerable = false; // empieza según lo indique ControlNivel al spawnear

    bool muerto = false;
    bool spawnIniciado = false;

    public override void OnStartServer()
    {
        base.OnStartServer();

        vidaActual = vidaMaxima;

        // Informar estado inicial a clientes
        RpcActualizarVida(vidaActual, vidaMaxima);
        RpcSetVulnerable(vulnerable);

        if (generarEnemigos && !spawnIniciado)
        {
            spawnIniciado = true;
            StartCoroutine(LoopSpawn());
        }
    }

    IEnumerator LoopSpawn()
    {
        while (!muerto && vidaActual > 0)
        {
            yield return new WaitForSeconds(tiempoEntreSpawns);

            if (prefabEnemigo == null) continue;

            Vector3 pos = transform.position + new Vector3(Random.Range(-radioSpawn, radioSpawn), -1f, 0f);
            GameObject go = Instantiate(prefabEnemigo, pos, Quaternion.identity);
            ServerManager.Spawn(go);
            Debug.Log("Horno: spawn enemigo en " + pos);
        }
    }

    // Método público que puede llamar LogicaJugadorRed (server) al atacar
    public void RecibirDanio(int dmg)
    {
        if (!IsServer || muerto) return;

        if (!vulnerable)
        {
            Debug.Log("Horno: recibió ataque pero está INVULNERABLE.");
            return;
        }

        vidaActual -= dmg;
        vidaActual = Mathf.Max(0, vidaActual);

        // Actualizar HUD en todos los clientes
        RpcActualizarVida(vidaActual, vidaMaxima);

        if (vidaActual <= 0)
        {
            muerto = true;
            Debug.Log("Jefe derrotado (server)");
            RpcOcultarHUD();
            ServerManager.Despawn(gameObject);
        }
    }

    // Server-side: pedir que el horno se abra (reproduce anim y tras duración activa vulnerabilidad)
    public void AbrirHornoServidor()
    {
        if (!IsServer) return;
        ObserversPlayOpenAnimation(); // todos reproducen la anim de apertura
        StartCoroutine(EsperarYSetVulnerable(duracionAbrirAnim));
    }

    IEnumerator EsperarYSetVulnerable(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        SetVulnerable(true);
        Debug.Log("Servidor: horno ahora VULNERABLE");
    }

    // Server-side: cerrar horno inmediatamente (opcional)
    public void CerrarHornoServidor()
    {
        if (!IsServer) return;
        SetVulnerable(false);
        ObserversPlayCloseAnimation();
        Debug.Log("Servidor: horno CERRADO / invulnerable");
    }

    // Cambia el estado en servidor y notifica a clientes
    public void SetVulnerable(bool v)
    {
        if (!IsServer) return;
        vulnerable = v;
        RpcSetVulnerable(v);
    }

    // ---- RPCs / Observers para actualizar clientes ----

    // Actualiza la HUD en clientes con vida actual
    [ObserversRpc]
    void RpcActualizarVida(int vida, int vidaMax)
    {
        // Llamar al HUD local si existe
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetVida(vida, vidaMax);
    }

    // Notifica invulnerabilidad a clientes
    [ObserversRpc]
    void RpcSetVulnerable(bool v)
    {
        var hud = FindObjectOfType<HUD_Jefe_Sprites>();
        if (hud != null) hud.SetInvulnerable(!v); // HUD espera "invulnerable" visual = !v
    }

    // Reproducir animación de apertura en todos los clientes (ObserversRpc)
    [ObserversRpc]
    void ObserversPlayOpenAnimation()
    {
        if (animator != null)
        {
            // Si usas parámetro booleano:
            animator.SetBool(paramIsOpen, true);
            // O si prefieres un trigger:
            // animator.SetTrigger("Open");
        }

        // Mostrar HUD cuando abra
        HUD_Jefe_Sprites.Instance?.MostrarHUD();
    }

    // Reproducir animación de cierre
    [ObserversRpc]
    void ObserversPlayCloseAnimation()
    {
        if (animator != null)
        {
            animator.SetBool(paramIsOpen, false);
            // animator.SetTrigger("Close");
        }

        // Opcional: puedes ocultar HUD al cerrar
        // HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    [ObserversRpc]
    void RpcOcultarHUD()
    {
        HUD_Jefe_Sprites.Instance?.OcultarHUD();
    }

    // (Opcional) si prefieres usar Animation Events en el prefab, puedes dejar estos métodos públicos
    // y llamarlos desde los Animation Events. Sin embargo preferible: servidor controla timing.
    public void OnOpenAnimationEvent_Server()
    {
        if (IsServer)
        {
            // Si se usa Animation Event en la instancia de servidor, activa vulnerabilidad.
            SetVulnerable(true);
        }
    }

    public void OnCloseAnimationEvent_Server()
    {
        if (IsServer)
        {
            SetVulnerable(false);
        }
    }
}