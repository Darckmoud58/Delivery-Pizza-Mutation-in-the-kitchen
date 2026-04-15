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
    public GameObject palaGuia;

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

    void Start()
    {
        if (palaGuia != null) palaGuia.SetActive(false);

        foreach (GameObject p in paredes)
            if (p != null) p.SetActive(true);
    }

    public void EntrarZona(int indice)
    {
        if (indice != zonaActual) return;

        if (palaGuia != null) palaGuia.SetActive(false);
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
            if (palaGuia != null) palaGuia.SetActive(true);
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

        if (palaGuia != null) palaGuia.SetActive(false);
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

    [ObserversRpc]
    void RpcMostrarPala(bool estado)
    {
        if (palaGuia != null)
            palaGuia.SetActive(estado);
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
    }
}