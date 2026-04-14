using UnityEngine;
using FishNet.Object;

[RequireComponent(typeof(Camera))]
public class CamaraSigue : MonoBehaviour
{
    public float suavizado = 5f;
    public bool usarLimites = true;
    public BoxCollider2D limiteCollider;

    private Vector2 limiteMin;
    private Vector2 limiteMax;

    private Camera cam;
    private float camHalfWidth;
    private float camHalfHeight;

    private Transform objetivo;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        CalcularMediosCamara();
        if (limiteCollider != null) CalcularLimitesDesdeCollider();
    }

    void LateUpdate()
    {
        if (objetivo == null)
        {
            BuscarJugadorLocal();
            if (objetivo == null) return;
        }

        SeguirObjetivo();
    }

    void BuscarJugadorLocal()
    {
        LogicaJugadorRed[] jugadores = FindObjectsOfType<LogicaJugadorRed>();
        foreach (LogicaJugadorRed j in jugadores)
        {
            if (j.IsOwner)
            {
                objetivo = j.transform;
                break;
            }
        }
    }

    void SeguirObjetivo()
    {
        float x = Mathf.Lerp(transform.position.x, objetivo.position.x, Time.deltaTime * suavizado);
        float y = Mathf.Lerp(transform.position.y, objetivo.position.y, Time.deltaTime * suavizado);

        if (usarLimites && limiteCollider != null)
        {
            x = Mathf.Clamp(x, limiteMin.x, limiteMax.x);
            y = Mathf.Clamp(y, limiteMin.y, limiteMax.y);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }

    void CalcularMediosCamara()
    {
        if (cam == null) return;
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;
    }

    void CalcularLimitesDesdeCollider()
    {
        if (limiteCollider == null) return;

        Bounds b = limiteCollider.bounds;

        float minX = b.min.x + camHalfWidth;
        float maxX = b.max.x - camHalfWidth;
        float minY = b.min.y + camHalfHeight;
        float maxY = b.max.y - camHalfHeight;

        if (minX > maxX)
        {
            float centroX = (b.min.x + b.max.x) / 2f;
            minX = maxX = centroX;
        }
        if (minY > maxY)
        {
            float centroY = (b.min.y + b.max.y) / 2f;
            minY = maxY = centroY;
        }

        limiteMin = new Vector2(minX, minY);
        limiteMax = new Vector2(maxX, maxY);
    }
}