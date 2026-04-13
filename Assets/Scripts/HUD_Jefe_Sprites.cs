using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // <-- Añadir esto para usar Image, Text, etc.

public class HUD_Jefe_Sprites : MonoBehaviour
{
    public static HUD_Jefe_Sprites Instance;

    [Header("Referencias UI")]
    public Image imagenBarra;        // La Image que usará Filled (hija RellenoVida)
    public Sprite spriteVerde;
    public Sprite spriteNaranja;
    public Sprite spriteRojo;

    [Header("Configuración")]
    public float velocidadSuavizado = 5f;
    public float sacudirIntensidad = 5f;
    public int sacudirVeces = 5;

    // Estado interno
    float vidaObjetivo = 1f;

    void Awake()
    {
        // Singleton simple
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        // Si no asignaste la imagen en el inspector, intenta obtenerla del objeto o hijos
        if (imagenBarra == null)
        {
            imagenBarra = GetComponentInChildren<Image>();
            // Si el objeto principal es la propia imagen, GetComponent<Image>() la devolverá
            if (imagenBarra == null) imagenBarra = GetComponent<Image>();
        }

        // Asegurarnos de un estado inicial visible/inicializado
        if (imagenBarra != null)
        {
            imagenBarra.type = Image.Type.Filled;
            imagenBarra.fillMethod = Image.FillMethod.Horizontal;
            imagenBarra.fillOrigin = (int)Image.OriginHorizontal.Left;
            imagenBarra.fillAmount = 1f;
        }

        // Ocultamos por defecto si se desea (opcional)
        // gameObject.SetActive(false);
    }

    void Update()
    {
        if (imagenBarra == null) return;

        // Suavizado de la barra
        imagenBarra.fillAmount = Mathf.Lerp(imagenBarra.fillAmount, vidaObjetivo, Time.deltaTime * velocidadSuavizado);

        // Cambiar sprite según porcentaje actual (suavizado visual)
        ActualizarSprite(imagenBarra.fillAmount);
    }

    // Llamar desde el Horno para actualizar la HUD
    public void SetVida(int vidaActual, int vidaMax)
    {
        lifeClampAndShow(vidaActual, vidaMax);
    }

    void lifeClampAndShow(int vidaActual, int vidaMax)
    {
        if (vidaMax <= 0) vidaMax = 1;
        vidaObjetivo = Mathf.Clamp01((float)vidaActual / (float)vidaMax);

        // Aseguramos que el panel esté visible
        if (!gameObject.activeSelf) gameObject.SetActive(true);

        // Pequeña sacudida visual al recibir daño
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

    public void OcultarHUD()
    {
        gameObject.SetActive(false);
    }

    public void MostrarHUD()
    {
        gameObject.SetActive(true);
    }
}