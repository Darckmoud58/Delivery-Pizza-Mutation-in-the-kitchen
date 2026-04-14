using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Jefe_Sprites : MonoBehaviour
{
    public static HUD_Jefe_Sprites Instance;

    [Header("Referencias UI")]
    public Image imagenBarra;        // Image tipo Filled
    public Sprite spriteVerde;
    public Sprite spriteNaranja;
    public Sprite spriteRojo;

    // Nuevo: icono de bloqueo / invulnerable (opcional)
    public Image iconLock;
    public Color colorNormal = Color.white;
    public Color colorInvulnerable = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Configuración")]
    public float velocidadSuavizado = 5f;
    public float sacudirIntensidad = 5f;
    public int sacudirVeces = 5;

    float vidaObjetivo = 1f;
    bool invulnerable = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        if (imagenBarra == null)
        {
            imagenBarra = GetComponentInChildren<Image>();
            if (imagenBarra == null) imagenBarra = GetComponent<Image>();
        }

        if (imagenBarra != null)
        {
            imagenBarra.type = Image.Type.Filled;
            imagenBarra.fillMethod = Image.FillMethod.Horizontal;
            imagenBarra.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenBarra.fillAmount = 1f;
        }
    }

    void Update()
    {
        if (imagenBarra == null) return;
        imagenBarra.fillAmount = Mathf.Lerp(imagenBarra.fillAmount, vidaObjetivo, Time.deltaTime * velocidadSuavizado);
        ActualizarSprite(imagenBarra.fillAmount);

        // Aplica color por invulnerabilidad
        if (imagenBarra != null)
            imagenBarra.color = invulnerable ? colorInvulnerable : colorNormal;

        if (iconLock != null)
            iconLock.gameObject.SetActive(invulnerable);
    }

    public void SetVida(int vidaActual, int vidaMax)
    {
        if (vidaMax <= 0) vidaMax = 1;
        vidaObjetivo = Mathf.Clamp01((float)vidaActual / (float)vidaMax);

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(SacudirBarra());
    }

    void ActualizarSprite(float porcentaje)
    {
        if (spriteVerde == null || spriteNaranja == null || spriteRojo == null || imagenBarra == null) return;

        if (porcentaje > 0.6f)
            imagenBarra.sprite = spriteVerde;
        else if (porcentaje > 0.3f)
            imagenBarra.sprite = spriteNaranja;
        else
            imagenBarra.sprite = spriteRojo;
    }

    IEnumerator SacudirBarra()
    {
        if (sacudirVeces <= 0 || sacudirIntensidad <= 0f) yield break;
        RectTransform rt = transform as RectTransform;
        if (rt == null) yield break;

        Vector3 original = rt.localPosition;
        for (int i = 0; i < sacudirVeces; i++)
        {
            rt.localPosition = original + (Vector3)(Random.insideUnitCircle * sacudirIntensidad);
            yield return new WaitForSeconds(0.03f);
        }
        rt.localPosition = original;
    }

    public void OcultarHUD() => gameObject.SetActive(false);
    public void MostrarHUD() => gameObject.SetActive(true);

    // Nuevo: cambia visual de invulnerabilidad
    public void SetInvulnerable(bool estado)
    {
        invulnerable = estado;
        // si quieres además esconder la barra cuando invulnerable:
        // imagenBarra.gameObject.SetActive(!estado);
        if (estado)
        {
            // opcional: efecto visual
            StartCoroutine(SacudirBarra());
        }
    }
}