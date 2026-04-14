using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class WaveZone : MonoBehaviourPun
{
    [Header("Configuración")]
    public int indiceZona = 0;
    public string enemigoPrefab = "Enemigo";

    [Header("Oleadas")]
    public int[] enemigosXOleada = { 2, 2 };

    [Header("Spawn")]
    public Transform puntoSpawn;

    private int enemigosVivos = 0;
    private int oleadaActual = 0;
    private bool iniciado = false;
    private bool completada = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (ControlNivel.Instance == null) return;
        ControlNivel.Instance.EntrarZona(indiceZona);
    }

    public void IniciarZona()
    {
        if (iniciado || completada) return;
        iniciado = true;
        if (PhotonNetwork.IsMasterClient)
            StartCoroutine(LanzarOleada());
    }

    IEnumerator LanzarOleada()
    {
        if (oleadaActual >= enemigosXOleada.Length) yield break;

        int cantidad = enemigosXOleada[oleadaActual];
        enemigosVivos = cantidad;
        Debug.Log("Zona " + indiceZona + " - Oleada " + (oleadaActual + 1) + ": " + cantidad + " enemigos");

        for (int i = 0; i < cantidad; i++)
        {
            Vector3 pos = puntoSpawn != null
                ? puntoSpawn.position + new Vector3(Random.Range(-1f, 1f), 0f, 0f)
                : transform.position;

            GameObject go = PhotonNetwork.Instantiate(enemigoPrefab, pos, Quaternion.identity);
            IAEnemigoRed ia = go.GetComponent<IAEnemigoRed>();
            if (ia != null) ia.indiceZonaOrigen = indiceZona;

            yield return new WaitForSeconds(1.5f);
        }
    }

    public void EnemigoMuerto()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        enemigosVivos--;
        Debug.Log("Zona " + indiceZona + " | Enemigos vivos: " + enemigosVivos);

        if (enemigosVivos <= 0)
        {
            oleadaActual++;
            if (oleadaActual < enemigosXOleada.Length)
                StartCoroutine(EsperarSiguienteOleada());
            else
                ZonaTerminada();
        }
    }

    IEnumerator EsperarSiguienteOleada()
    {
        yield return new WaitForSeconds(2f);
        StartCoroutine(LanzarOleada());
    }

    void ZonaTerminada()
    {
        if (completada) return;
        completada = true;
        Debug.Log("¡Zona " + indiceZona + " completada!");

        // Sin RPC — directo. ControlNivel existe en todos los clientes.
        if (ControlNivel.Instance != null)
            ControlNivel.Instance.ZonaCompletada();
    }
}
