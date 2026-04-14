using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUD_Jugador : MonoBehaviour
{
    public static HUD_Jugador Instance;

    public Image[] corazones;
    public Sprite corazonLleno;
    public Sprite corazonMedio;
    public Color colorVacio = new Color(0.1f, 0.1f, 0.1f, 1f);

    void Awake()
    {
        Instance = this;
    }

    public void ActualizarVida(int vidaActual, int vidaMax)
    {
        int mediosMaximos = corazones.Length * 2;
        int mediosActuales = Mathf.Clamp(Mathf.CeilToInt((vidaActual / (float)vidaMax) * mediosMaximos), 0, mediosMaximos);

        for (int i = 0; i < corazones.Length; i++)
        {
            int valorCorazon = i * 2;

            if (mediosActuales >= valorCorazon + 2)
            {
                corazones[i].sprite = corazonLleno;
                corazones[i].color = Color.white;
            }
            else if (mediosActuales == valorCorazon + 1)
            {
                corazones[i].sprite = corazonMedio;
                corazones[i].color = Color.white;
            }
            else
            {
                corazones[i].sprite = corazonLleno;
                corazones[i].color = colorVacio;
            }
        }
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
