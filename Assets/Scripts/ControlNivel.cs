using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ControlNivel : MonoBehaviourPun
{
    public static ControlNivel Instance;

    [Header("Zonas (en orden)")]
    public WaveZone[] zonas;

    [Header("Jefe Final")]
    public string prefabHorno = "Horno";
    public Transform spawnHorno;

    [Header("Pala / Flecha avanzar")]
    public GameObject palaGuia;

    [Header("Pared invisible entre zonas")]
    public GameObject[] paredes;

    private int zonaActual = 0;
    private bool jefeLanzado = false;

    void Awake() { Instance = this; }

    void Start()
    {
        if (palaGuia != null) palaGuia.SetActive(false);
        foreach (var p in paredes) if (p != null) p.SetActive(true);
        // NO iniciamos zona aquí — el trigger OnTriggerEnter2D del WaveZone lo hace
    }

    public void EntrarZona(int indice)
    {
        if (indice != zonaActual) return;
        if (palaGuia != null) palaGuia.SetActive(false);
        Debug.Log("Iniciando zona " + indice);
        zonas[indice].IniciarZona();
    }

    public void ZonaCompletada()
    {
        StartCoroutine(SiguientePasoCo());
    }

    IEnumerator SiguientePasoCo()
    {
        yield return new WaitForSeconds(1.5f);

        // Quitar pared de la zona actual
        if (zonaActual < paredes.Length && paredes[zonaActual] != null)
        {
            Collider2D col = paredes[zonaActual].GetComponent<Collider2D>();
            if (col != null) Destroy(col);
            paredes[zonaActual].SetActive(false);
            Debug.Log("Pared " + zonaActual + " eliminada");
        }

        zonaActual++;
        Debug.Log("Zona actual ahora: " + zonaActual + " / Total zonas: " + zonas.Length);

        if (zonaActual < zonas.Length)
        {
            // Hay más zonas — mostrar pala para avanzar
            if (palaGuia != null) palaGuia.SetActive(true);
        }
        else
        {
            // Todas las zonas limpias → jefe
            Debug.Log("Todas las zonas completadas, lanzando jefe en 2s...");
            yield return new WaitForSeconds(2f);
            LanzarJefe();
        }
    }

    void LanzarJefe()
    {
        if (jefeLanzado) return;
        jefeLanzado = true;
        if (palaGuia != null) palaGuia.SetActive(false);

        if (PhotonNetwork.IsMasterClient && spawnHorno != null && PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.Instantiate(prefabHorno, spawnHorno.position, Quaternion.identity);
            Debug.Log("¡JEFE LANZADO!");
        }
    }
}
