using UnityEngine;
using FishNet.Object;

public class IAEnemigoRed : NetworkBehaviour
{
    [Header("Movimiento")]
    public float velocidad = 3f;

    [Header("Vida")]
    public int vida = 30;

    [Header("Ataque")]
    public int danio = 10;
    public float distanciaAtaque = 1.1f;
    public float tiempoEntreAtaques = 1.2f;

    [Header("Zona")]
    public int indiceZonaOrigen = -1;

    Rigidbody2D rb;
    Transform objetivo;
    float siguienteAtaque = 0f;
    bool muriendo = false;

    public override void OnStartServer()
    {
        base.OnStartServer();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!IsServerInitialized || muriendo) return;

        BuscarObjetivoMasCercano();

        if (objetivo == null)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }

        float dist = Vector2.Distance(transform.position, objetivo.position);

        if (dist > distanciaAtaque)
        {
            Vector2 dir = (objetivo.position - transform.position).normalized;
            if (rb != null)
                rb.velocity = dir * velocidad;
        }
        else
        {
            if (rb != null)
                rb.velocity = Vector2.zero;

            if (Time.time >= siguienteAtaque)
            {
                siguienteAtaque = Time.time + tiempoEntreAtaques;
                LogicaJugadorRed jugador = objetivo.GetComponent<LogicaJugadorRed>();
                if (jugador != null)
                    jugador.RecibirDanio(danio);
            }
        }
    }

    void BuscarObjetivoMasCercano()
    {
        LogicaJugadorRed[] jugadores = FindObjectsOfType<LogicaJugadorRed>();
        float mejorDist = float.MaxValue;
        objetivo = null;

        foreach (LogicaJugadorRed j in jugadores)
        {
            if (j == null || j.EstaMuerto) continue;

            float d = Vector2.Distance(transform.position, j.transform.position);
            if (d < mejorDist)
            {
                mejorDist = d;
                objetivo = j.transform;
            }
        }
    }

    public void RecibirDanio(int dmg)
    {
        if (!IsServerInitialized || muriendo) return;

        vida -= dmg;

        if (vida <= 0)
        {
            muriendo = true;
            NotificarMuerteZona();
            ServerManager.Despawn(gameObject);
        }
    }

    void NotificarMuerteZona()
    {
        if (ControlNivel.Instance == null || ControlNivel.Instance.zonas == null) return;

        for (int i = 0; i < ControlNivel.Instance.zonas.Length; i++)
        {
            WaveZone zona = ControlNivel.Instance.zonas[i];
            if (zona != null && zona.indiceZona == indiceZonaOrigen)
            {
                zona.EnemigoMuerto();
                return;
            }
        }
    }
}