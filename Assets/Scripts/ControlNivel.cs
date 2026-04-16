// Assets/Scripts/ControlNivel.cs
using System.Collections;
using UnityEngine;
using FishNet.Object;

public class ControlNivel : NetworkBehaviour
{
    public static ControlNivel Instance;

    [Header("Zonas (en orden)")]
    public WaveZone[] zonas;

    [Header("Jefe Final")]
    public GameObject prefabHorno;
    public Transform spawnHorno;

    [Header("Pala / Flecha avanzar")]
    [Tooltip("Prefab local de la pala que cada cliente instanciará")]
    public GameObject palaPrefab;
    // instancia local creada en OnStartClient (no networked)
    GameObject palaGuiaInstance;

    [Header("Pared invisible entre zonas")]
    public GameObject[] paredes;

    [Header("Configuración de Jefe")]
    [Tooltip("Índice de la pared que al desactivarse debe abrir el horno (hacerlo vulnerable)")]
    public int indiceParedJefe = -1;

    // referencia al horno instanciado (server)
    GameObject hornoInstanciado;

    private int zonaActual = 0;
    private bool jefeLanzado = false;

    void Awake()
    {
        Instance = this;
    }

    // Inicialización en cliente: instanciamos la pala local si hay prefab
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (palaPrefab != null && palaGuiaInstance == null)
        {
            // Crear instancia local (visual) para cada cliente
            palaGuiaInstance = Instantiate(palaPrefab);
            palaGuiaInstance.name = "Pala_Guia_Instance";
            palaGuiaInstance.SetActive(false); // empieza oculta hasta que el servidor la pida

            // organización opcional
            palaGuiaInstance.transform.SetParent(this.transform, true);

            // asegurar que se renderiza por encima
            var sr = palaGuiaInstance.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 100;

            Debug.Log("[ControlNivel][Client] Pala instanciada localmente.");
        }
    }

    void Start()
    {
        // Compatibility: si pusiste manualmente una instancia del prefab en escena (por ejemplo arrastraste Pala_Net),
        // intenta encontrarla y usarla en lugar de la instanciada.
        if (palaGuiaInstance == null)
        {
            GameObject existing = GameObject.Find("Pala_Guia_Instance");
            if (existing == null) existing = GameObject.Find("Pala_Net");
            if (existing != null)
            {
                palaGuiaInstance = existing;
                palaGuiaInstance.SetActive(false);
                var sr = palaGuiaInstance.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 100;
                Debug.Log("[ControlNivel] Usando instancia manual de pala en escena.");
            }
        }

        foreach (GameObject p in paredes)
            if (p != null) p.SetActive(true);
    }

    public void EntrarZona(int indice)
    {
        if (indice != zonaActual) return;

        // ocultar pala mientras la zona inicia
        if (palaGuiaInstance != null) palaGuiaInstance.SetActive(false);
        RpcMostrarPala(false);

        Debug.Log("Iniciando zona " + indice);

        if (zonas != null && indice >= 0 && indice < zonas.Length && zonas[indice] != null)
            zonas[indice].IniciarZona();
    }

    public void ZonaCompletada()
    {
        if (!IsServerInitialized) return;
        StartCoroutine(SiguientePasoCo());
    }

    IEnumerator SiguientePasoCo()
    {
        yield return new WaitForSeconds(1.5f);

        // Guardamos el índice de la pared que vamos a desactivar (zonaActual actual)
        int paredIndex = zonaActual;

        if (paredIndex < paredes.Length && paredes[paredIndex] != null)
        {
            Collider2D col = paredes[paredIndex].GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            paredes[paredIndex].SetActive(false);
            RpcDesactivarPared(paredIndex);

            Debug.Log("Pared " + paredIndex + " eliminada");

            // Si esa pared es la que protege al jefe, y ya tenemos instanciado el horno, lo abrimos (server)
            if (paredIndex == indiceParedJefe && hornoInstanciado != null)
            {
                HornoController hc = hornoInstanciado.GetComponent<HornoController>();
                if (hc != null)
                {
                    Debug.Log("ControlNivel: se abrió la pared del jefe. Llamando AbrirHornoServidor()");
                    hc.AbrirHornoServidor();
                }
            }
        }

        zonaActual++;
        Debug.Log("Zona actual ahora: " + zonaActual + " / Total zonas: " + zonas.Length);

        if (zonaActual < zonas.Length)
        {
            // Mostramos la pala en clientes (servidor pide a todos los observers)
            if (palaGuiaInstance != null) palaGuiaInstance.SetActive(true);
            RpcMostrarPala(true);
        }
        else
        {
            Debug.Log("Todas las zonas completadas, lanzando jefe en 2s...");
            yield return new WaitForSeconds(2f);
            LanzarJefe();
        }
    }

    void LanzarJefe()
    {
        if (!IsServerInitialized || jefeLanzado) return;
        jefeLanzado = true;

        if (palaGuiaInstance != null) palaGuiaInstance.SetActive(false);
        RpcMostrarPala(false);

        if (prefabHorno != null && spawnHorno != null)
        {
            GameObject go = Instantiate(prefabHorno, spawnHorno.position, Quaternion.identity);
            ServerManager.Spawn(go);
            hornoInstanciado = go;
            Debug.Log("¡JEFE LANZADO!");

            // Abrir inmediatamente (server)
            HornoController hc = go.GetComponent<HornoController>();
            if (hc != null)
            {
                Debug.Log("[ControlNivel] AbrirHornoServidor() inmediatamente tras spawn (prueba).");
                hc.AbrirHornoServidor();
            }
        }
        else
        {
            Debug.LogError("Falta prefabHorno o spawnHorno en ControlNivel.");
        }
    }

    // El ObserversRpc afectará a todos los clientes: usará la instancia local palaGuiaInstance
    [ObserversRpc]
    void RpcMostrarPala(bool estado)
    {
        Debug.Log("[ControlNivel] RpcMostrarPala llamado con estado: " + estado);
        if (palaGuiaInstance != null)
            palaGuiaInstance.SetActive(estado);
    }

    [ObserversRpc]
    void RpcDesactivarPared(int indice)
    {
        if (indice >= 0 && indice < paredes.Length && paredes[indice] != null)
        {
            Collider2D col = paredes[indice].GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            paredes[indice].SetActive(false);
        }

        if (indice == indiceParedJefe && hornoInstanciado != null)
        {
            HornoController hc = hornoInstanciado.GetComponent<HornoController>();
            if (hc != null)
            {
                hc.SetVulnerable(true);
                Debug.Log("Se abrió la puerta del horno: ahora es vulnerable (server).");
            }
        }
    }
}