using UnityEngine;
using FishNet.Object;

public class IAEnemigoRed : NetworkBehaviour
{
    public float velocidad = 3f;
    public int vida = 30;
    Transform objetivo;
    Rigidbody2D rb;

    public override void OnStartServer()
    {
        base.OnStartServer();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!IsServer) return;

        BuscarObjetivo();
        if (objetivo != null)
        {
            Vector2 dir = (objetivo.position - transform.position).normalized;
            rb.velocity = dir * velocidad;
        }
    }

    void BuscarObjetivo()
    {
        GameObject j = GameObject.FindWithTag("Player");
        if (j != null) objetivo = j.transform;
    }

    public void RecibirDanio(int dmg)
    {
        vida -= dmg;
        if (vida <= 0) Despawn();
    }
}