// Assets/Scripts/HUD_Jefe_Sprites.cs
using UnityEngine;
using UnityEngine.UI;

public class HUD_Jefe_Sprites : MonoBehaviour
{
    public static HUD_Jefe_Sprites Instance;

    [Header("Referencias UI")]
    [Tooltip("Panel/objeto que contiene la barra (marco + relleno). Debe estar desactivado por defecto.")]
    public GameObject fondoBarra;

    [Tooltip("Image del relleno (Image Type = Filled, Fill Method = Horizontal, Fill Origin = Left)")]
    public Image imagenBarra;

    [Header("Sprites por rangos")]
    public Sprite spriteVerde;
    public Sprite spriteNaranja;
    public Sprite spriteRojo;

    [Header("Opcionales")]
    [Tooltip("Imagen que sirve de 'candado' o icono cuando está invulnerable (opcional).")]
    public Image iconLock;

    [Tooltip("Color normal del relleno (por defecto blanco)")]
    public Color colorNormal = Color.white;

    [Tooltip("Color cuando está invulnerable (ej. gris claro)")]
    public Color colorInvulnerable = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Suavizado")]
    public float velocidadSuavizado = 5f;

    // estado interno
    float vidaObjetivo = 1f; // 0..1
    bool esInvulnerableVis = false;

    void Awake()
    {
        // Singleton simple
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        // Asegurar que la barra empieza oculta si hay un fondoBarra asignado
        if (fondoBarra != null) fondoBarra.SetActive(false);

        // Asegurar imagenBarra en estado inicial
        if (imagenBarra != null)
        {
            imagenBarra.type = Image.Type.Filled;
            imagenBarra.fillMethod = Image.FillMethod.Horizontal;
            imagenBarra.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenBarra.fillAmount = 1f;
            imagenBarra.color = colorNormal;
        }

        if (iconLock != null) iconLock.gameObject.SetActive(false);
    }

    void Update()
    {
        if (imagenBarra == null) return;

        // suavizado del fill amount
        imagenBarra.fillAmount = Mathf.Lerp(imagenBarra.fillAmount, vidaObjetivo, Time.deltaTime * velocidadSuavizado);

        // opcional: cambio de sprite según el porcentaje visual actual (puedes usar vidaObjetivo en su lugar)
        ActualizarSpriteSegunPorcentaje(imagenBarra.fillAmount);
    }

    void ActualizarSpriteSegunPorcentaje(float porcentaje)
    {
        if (imagenBarra == null) return;

        // evita cambiar si no se asignaron sprites
        if (spriteVerde == null && spriteNaranja == null && spriteRojo == null) return;

        if (porcentaje > 0.66f && spriteVerde != null) imagenBarra.sprite = spriteVerde;
        else if (porcentaje > 0.33f && spriteNaranja != null) imagenBarra.sprite = spriteNaranja;
        else if (spriteRojo != null) imagenBarra.sprite = spriteRojo;
    }

    // --- Métodos públicos usados por HornoController ---

    // Mostrar el HUD (activar el panel que contiene la barra)
    public void MostrarHUD()
    {
        if (fondoBarra != null) fondoBarra.SetActive(true);
        else Debug.LogWarning("[HUD_Jefe] MostrarHUD: fondoBarra no asignado.");
    }

    // Ocultar el HUD
    public void OcultarHUD()
    {
        if (fondoBarra != null) fondoBarra.SetActive(false);
        else Debug.LogWarning("[HUD_Jefe] OcultarHUD: fondoBarra no asignado.");
    }

    // Actualizar vida desde el servidor (valores absolutos)
    public void SetVida(int vidaActual, int vidaMax)
    {
        if (vidaMax <= 0) return;
        vidaObjetivo = Mathf.Clamp01((float)vidaActual / vidaMax);

        // si la vida es 0 ocultamos HUD
        if (vidaActual <= 0) OcultarHUD();
    }

    // Mostrar/ocultar visual de invulnerable (HornoController llama con !v)
    public void SetInvulnerable(bool invulnerable)
    {
        esInvulnerableVis = invulnerable;

        if (imagenBarra != null)
        {
            imagenBarra.color = invulnerable ? colorInvulnerable : colorNormal;
        }

        if (iconLock != null)
            iconLock.gameObject.SetActive(invulnerable);
    }

    // Helper (útil en Edición): forzar barra full
    public void MostrarHUDFull()
    {
        MostrarHUD();
        vidaObjetivo = 1f;
        if (imagenBarra != null) imagenBarra.fillAmount = 1f;
    }
}