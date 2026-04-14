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

    private int zonaActual = 0;
    private bool jefeLanzado = false;

    public int indiceParedJefe = -1; // arrástralo en el inspector al índice de la pared que protege el horno
    GameObject hornoInstanciado; // campo de clase

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

        if (zonaActual < paredes.Length && paredes[zonaActual] != null)
        {
            Collider2D col = paredes[zonaActual].GetComponent<Collider2D>();
            if (col != null) col.enabled = false;

            paredes[zonaActual].SetActive(false);
            RpcDesactivarPared(zonaActual);

            Debug.Log("Pared " + zonaActual + " eliminada");
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

            // NO vulnerable inicialmente hasta que se abra la puerta (opcional)
            HornoController hc = go.GetComponent<HornoController>();
            if (hc != null) hc.SetVulnerable(false);
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