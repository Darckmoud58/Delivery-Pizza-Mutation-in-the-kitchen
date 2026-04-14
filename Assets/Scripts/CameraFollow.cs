using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance;

    [Header("Movimiento")]
    public float suavizado = 5f;

    [Header("Límites (opcional)")]
    public bool usarLimites = true;
    public BoxCollider2D limiteCollider;

    Vector2 limiteMin;
    Vector2 limiteMax;

    Camera cam;
    float camHalfWidth;
    float camHalfHeight;

    Transform objetivo;
    bool centrarAlSetear = true;

    void Awake()
    {
        Instance = this;
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
        CalcularMediosCamara();
        if (limiteCollider != null) CalcularLimitesDesdeCollider();
    }

    void LateUpdate()
    {
        // Si no tenemos objetivo buscamos localmente (fallback)
        if (objetivo == null)
        {
            BuscarJugadorLocal();
            if (objetivo == null) return;
        }

        // Si pedimos centrar ahora, colocamos la camara en la posición objetivo antes de lerpear
        if (centrarAlSetear)
        {
            transform.position = new Vector3(objetivo.position.x, objetivo.position.y, transform.position.z);
            centrarAlSetear = false;
            return;
        }

        // Lerp hacia el objetivo
        float x = Mathf.Lerp(transform.position.x, objetivo.position.x, Time.deltaTime * suavizado);
        float y = Mathf.Lerp(transform.position.y, objetivo.position.y, Time.deltaTime * suavizado);

        if (usarLimites && limiteCollider != null)
        {
            x = Mathf.Clamp(x, limiteMin.x, limiteMax.x);
            y = Mathf.Clamp(y, limiteMin.y, limiteMax.y);
        }

        transform.position = new Vector3(x, y, transform.position.z);
    }

    public void SetTarget(Transform t, bool centrarAhora = false)
    {
        objetivo = t;
        centrarAlSetear = centrarAhora;
        if (centrarAhora && objetivo != null)
        {
            // mover inmediatamente sin esperar al siguiente LateUpdate opcional (puedes comentar la siguiente línea si prefieres hacerlo en LateUpdate)
            transform.position = new Vector3(objetivo.position.x, objetivo.position.y, transform.position.z);
        }
    }

    void BuscarJugadorLocal()
    {
        // Busca jugador local (esto es un fallback, lo ideal es que PlayerSpawnHandler llame SetTarget)
        var jugadores = FindObjectsOfType<LogicaJugadorRed>();
        foreach (var j in jugadores)
        {
            // Usamos IsOwner para detectar el jugador local
            if (j.IsOwner)
            {
                objetivo = j.transform;
                centrarAlSetear = true;
                break;
            }
        }
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

        if (minX > maxX) { float cx = (b.min.x + b.max.x) / 2f; minX = maxX = cx; }
        if (minY > maxY) { float cy = (b.min.y + b.max.y) / 2f; minY = maxY = cy; }

        limiteMin = new Vector2(minX, minY);
        limiteMax = new Vector2(maxX, maxY);
    }
}