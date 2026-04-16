// Assets/Scripts/MainMenuController.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using FishNet.Managing;
using FishNet.Transporting; // Necesario para acceder al Transport

[Serializable]
public class RoomData { public string ip; public int port; public long created; }

public class MainMenuController : MonoBehaviour
{
    [Header("UI (TMP)")]
    public GameObject menuRoot;
    public TMP_InputField inputRoomCode;
    public TMP_InputField inputIP;
    public TMP_Text txtRoomCode;
    public TMP_Text txtStatus;
    public GameObject heartsContainer;

    [Header("Firebase (opcional)")]
    public string firebaseBaseUrl;

    [Header("Network")]
    public int defaultPort = 7777;
    NetworkManager netManager;

    string currentRoomCode = null;
    bool hostPrepared = false;       
    bool hostStarted = false;        

    [Header("Panel Código Sala")]
    public GameObject panelCodigoSala;
    public TMP_Text txtCodigoSala;
    public CanvasGroup panelCanvasGroup; 

    void Awake()
    {
        netManager = FindObjectOfType<NetworkManager>();
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (txtRoomCode != null) txtRoomCode.text = "";
        if (txtStatus != null) txtStatus.text = "Esperando...";
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        hostPrepared = false;
        hostStarted = false;
    }

    // --- HOST: preparar panel (NO inicia host) ---
    public void OnHostPressed()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }

        string ip = GetLocalIPAddress();
        
        // --- ADAPTACIÓN PARA PUERTO REAL ---
        ushort actualPort = (ushort)defaultPort;
        if (netManager.TransportManager != null && netManager.TransportManager.Transport != null)
        {
            // Leemos el puerto configurado en el componente Tugboat o Telepathy
            actualPort = netManager.TransportManager.Transport.GetPort();
        }
        
        string display = ip + ":" + actualPort;
        currentRoomCode = display;
        // ------------------------------------

        if (txtRoomCode != null) txtRoomCode.text = "IP: " + display;

        Canvas targetCanvas = null;
        if (menuRoot != null) targetCanvas = menuRoot.GetComponentInParent<Canvas>();
        if (targetCanvas == null)
        {
            Canvas anyCanvas = FindObjectOfType<Canvas>();
            if (anyCanvas != null) targetCanvas = anyCanvas;
        }

        if (panelCodigoSala != null && targetCanvas != null)
        {
            panelCodigoSala.transform.SetParent(targetCanvas.transform, false);
            panelCodigoSala.transform.SetAsLastSibling();
        }

        if (txtCodigoSala != null) txtCodigoSala.text = "IP de la sala:\n" + display;
        if (panelCodigoSala != null) panelCodigoSala.SetActive(true);
        if (panelCanvasGroup != null) { panelCanvasGroup.alpha = 1f; panelCanvasGroup.interactable = true; panelCanvasGroup.blocksRaycasts = true; }

        GUIUtility.systemCopyBuffer = display;
        HideMenu();

        hostPrepared = true;
        hostStarted = false;

        SetStatus("Sala preparada. IP copiada. Pulsa 'Iniciar partida' para arrancar.");
        Debug.Log("[MainMenu] Sala creada. Usando puerto real del transporte: " + actualPort);
    }

    public void ConfirmStartHost()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }
        if (!hostPrepared) { SetStatus("Primero pulsa 'Crear partida'."); return; }
        if (hostStarted) { SetStatus("Host ya iniciado."); return; }

        try
        {
            netManager.ServerManager.StartConnection();
            netManager.ClientManager.StartConnection();

            hostStarted = true;
            SetStatus("Host iniciado localmente...");
            if (txtCodigoSala != null) txtCodigoSala.text = "Host iniciado.\nEsperando jugadores...\nIP: " + currentRoomCode;
            if (heartsContainer != null) heartsContainer.SetActive(true);
        }
        catch (Exception ex)
        {
            SetStatus("Error iniciando Host: " + ex.Message);
            Debug.LogException(ex);
        }
    }

    public void OnClientConnected()
    {
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        HideMenu();
        if (heartsContainer != null) heartsContainer.SetActive(true);
        SetStatus("Conectado al host. ¡Bienvenido!");
    }

    public void OnServerStarted()
    {
        SetStatus("Servidor listo. Esperando jugadores...");
    }

    public void OnJoinPressed()
    {
        if (netManager == null) return;

        string codeOrIp = (inputRoomCode != null) ? inputRoomCode.text.Trim() : "";
        string ipAlt = (inputIP != null) ? inputIP.text.Trim() : "";

        if (string.IsNullOrEmpty(codeOrIp) && !string.IsNullOrEmpty(ipAlt))
            codeOrIp = ipAlt;

        // Si el código incluye el puerto (ej: 172.16.10.119:7770), lo separamos
        string targetIP = codeOrIp;
        string targetPort = defaultPort.ToString();

        if (codeOrIp.Contains(":"))
        {
            string[] parts = codeOrIp.Split(':');
            targetIP = parts[0];
            targetPort = parts[1];
        }

        TrySetTransportAddressAndPort(targetIP, targetPort);
        netManager.ClientManager.StartConnection();

        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        HideMenu();
        if (heartsContainer != null) heartsContainer.SetActive(true);

        SetStatus("Intentando conectar a " + targetIP + ":" + targetPort + "...");
    }

    // --- UTILIDADES --------------------------------------------------------
    void HideMenu() { if (menuRoot != null) menuRoot.SetActive(false); }

    void SetStatus(string s) { if (txtStatus != null) txtStatus.text = s; }

    string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }

    void TrySetTransportAddressAndPort(string ip, string portText)
    {
        if (netManager == null) return;
        Component[] comps = netManager.GetComponents<Component>();
        foreach (var c in comps)
        {
            var props = c.GetType().GetProperties();
            foreach (var p in props)
            {
                try
                {
                    if (p.Name.ToLower().Contains("address") && p.PropertyType == typeof(string) && p.CanWrite)
                        p.SetValue(c, ip);
                    if (p.Name.ToLower().Contains("port") && p.PropertyType == typeof(int) && p.CanWrite)
                    {
                        if (int.TryParse(portText, out int port)) p.SetValue(c, port);
                    }
                    if (p.Name.ToLower().Contains("port") && p.PropertyType == typeof(ushort) && p.CanWrite)
                    {
                        if (ushort.TryParse(portText, out ushort port)) p.SetValue(c, port);
                    }
                }
                catch { }
            }
        }
    }
}