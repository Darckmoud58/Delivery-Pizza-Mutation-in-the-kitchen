// Assets/Scripts/MainMenuController.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using FishNet.Managing;

[Serializable]
public class RoomData { public string ip; public int port; public long created; }

public class MainMenuController : MonoBehaviour
{
    [Header("UI (TMP)")]
    public GameObject menuRoot;
    public TMP_InputField inputRoomCode;   // campo principal (puede ser código o IP)
    public TMP_InputField inputIP;         // campo alternativo (si usas otro input)
    public TMP_Text txtRoomCode;           // Text visible en el menú (opcional)
    public TMP_Text txtStatus;             // Text de estado (abajo)
    public GameObject heartsContainer;

    [Header("Firebase")]
    public string firebaseBaseUrl;

    [Header("Network")]
    public int defaultPort = 7777;
    NetworkManager netManager;

    string currentRoomCode = null;

    [Header("Panel Código Sala")]
    public GameObject panelCodigoSala;     // panel que muestra la IP/código
    public TMP_Text txtCodigoSala;         // text dentro del panel para mostrar IP/código

    void Awake()
    {
        netManager = FindObjectOfType<NetworkManager>();
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (txtRoomCode != null) txtRoomCode.text = "";
        if (txtStatus != null) txtStatus.text = "Esperando...";
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false); // start hidden
    }

    // --- HOST ---------------------------------------------------------------
    public void OnHostPressed()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }

        try
        {
            // Start server + client (host)
            netManager.ServerManager.StartConnection();
            netManager.ClientManager.StartConnection();

            SetStatus("Host iniciado localmente...");
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);

            // Construir la IP:porta y mostrarla
            string ip = GetLocalIPAddress();
            string display = ip + ":" + defaultPort;
            currentRoomCode = display;                // guardamos para copiar / reusar

            // Mostrar en el texto del menú (si existe)
            if (txtRoomCode != null)
            {
                txtRoomCode.text = "IP: " + display;
            }

            // Mostrar en el panel de código y activarlo
            if (txtCodigoSala != null) txtCodigoSala.text = "IP de la sala:\n" + display;
            if (panelCodigoSala != null) panelCodigoSala.SetActive(true);

            // Copiar automáticamente al portapapeles
            GUIUtility.systemCopyBuffer = display;

            Debug.Log("[MainMenu] Host iniciado. IP local: " + display);
            SetStatus("Sala creada. IP copiada al portapapeles.");
        }
        catch (Exception ex)
        {
            SetStatus("Error iniciando Host: " + ex.Message);
            Debug.LogException(ex);
        }

        // Si quieres también registrar en Firebase, descomenta:
        // StartCoroutine(HostRegisterFlow());
    }

    // Copiar el código/IP actual al portapapeles (botón "Copiar")
    public void CopiarCodigoAlPortapapeles()
    {
        if (!string.IsNullOrEmpty(currentRoomCode))
        {
            GUIUtility.systemCopyBuffer = currentRoomCode;
            SetStatus("Código copiado: " + currentRoomCode);
            Debug.Log("[MainMenu] Código copiado: " + currentRoomCode);
        }
        else
        {
            SetStatus("No hay código para copiar.");
            Debug.LogWarning("[MainMenu] Intento de copiar pero currentRoomCode vacío.");
        }
    }

    // Cierra el panel de código (botón "Cerrar" si lo pones)
    public void CerrarPanelCodigo()
    {
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        // Nota: no reabrimos el menú principal porque el host ya está iniciado.
        // Si quieres que cerrar vuelva al menú, descomenta la siguiente línea:
        // if (menuRoot != null) menuRoot.SetActive(true);
    }

    // --- JOIN ---------------------------------------------------------------
    public void OnJoinPressed()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }

        // Leer ambos campos (preferir inputRoomCode, fallback a inputIP)
        string codeOrIp = (inputRoomCode != null) ? inputRoomCode.text.Trim() : "";
        string ipAlt = (inputIP != null) ? inputIP.text.Trim() : "";

        Debug.Log($"[MainMenu] OnJoinPressed read inputRoomCode='{codeOrIp}' inputIP='{ipAlt}'");

        if (string.IsNullOrEmpty(codeOrIp) && !string.IsNullOrEmpty(ipAlt))
            codeOrIp = ipAlt;

        if (codeOrIp == "localhost" || codeOrIp.Contains("."))
        {
            TrySetTransportAddressAndPort(codeOrIp, defaultPort.ToString());
            netManager.ClientManager.StartConnection();
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);
            SetStatus("Conectando a " + codeOrIp + "...");
            Debug.Log("[MainMenu] Cliente iniciando conexión a " + codeOrIp);
            return;
        }

        if (!string.IsNullOrEmpty(codeOrIp))
        {
            StartCoroutine(JoinByRoomCodeCoroutine(codeOrIp));
        }
        else
        {
            SetStatus("Escribe un código o IP.");
        }
    }

    IEnumerator JoinByRoomCodeCoroutine(string code)
    {
        SetStatus("Buscando sala " + code + "...");
        string ipFound = null;
        yield return GetRoomFromFirebaseCoroutine(code, (ip, port) => { ipFound = ip; });

        if (!string.IsNullOrEmpty(ipFound))
        {
            SetStatus("Sala encontrada. Conectando...");
            TrySetTransportAddressAndPort(ipFound, defaultPort.ToString());
            netManager.ClientManager.StartConnection();
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);
        }
        else
        {
            SetStatus("Código no encontrado.");
        }
    }

    // --- FIREBASE (opcionales) ----------------------------------------------
    IEnumerator HostRegisterFlow()
    {
        yield return null;
        string localIP = GetLocalIPAddress();
        string code = UnityEngine.Random.Range(1000, 9999).ToString();

        SetStatus("Registrando sala " + code + "...");

        bool ok = false;
        yield return RegisterRoomOnFirebaseCoroutine(code, localIP, defaultPort, (success) => ok = success);

        if (ok)
        {
            currentRoomCode = code;
            if (txtRoomCode != null) txtRoomCode.text = "SALA: " + code;
            GUIUtility.systemCopyBuffer = code;
            SetStatus("Sala lista. Código copiado.");
        }
        else
        {
            SetStatus("Error al registrar en Firebase.");
        }
    }

    IEnumerator RegisterRoomOnFirebaseCoroutine(string code, string ip, int port, Action<bool> onComplete)
    {
        if (string.IsNullOrEmpty(firebaseBaseUrl)) { onComplete?.Invoke(false); yield break; }
        string url = $"{firebaseBaseUrl}/{code}.json";
        string json = JsonUtility.ToJson(new RoomData { ip = ip, port = port, created = DateTimeOffset.UtcNow.ToUnixTimeSeconds() });

        using (UnityWebRequest put = UnityWebRequest.Put(url, json))
        {
            put.method = UnityWebRequest.kHttpVerbPUT;
            yield return put.SendWebRequest();
            onComplete?.Invoke(put.result == UnityWebRequest.Result.Success);
        }
    }

    IEnumerator GetRoomFromFirebaseCoroutine(string code, Action<string, int> onResult)
    {
        if (string.IsNullOrEmpty(firebaseBaseUrl)) { onResult?.Invoke(null, 0); yield break; }
        string url = $"{firebaseBaseUrl}/{code}.json";
        using (UnityWebRequest get = UnityWebRequest.Get(url))
        {
            yield return get.SendWebRequest();
            if (get.result == UnityWebRequest.Result.Success && get.downloadHandler.text != "null")
            {
                var rd = JsonUtility.FromJson<RoomData>(get.downloadHandler.text);
                onResult?.Invoke(rd.ip, rd.port);
            }
            else { onResult?.Invoke(null, 0); }
        }
    }

    // --- UTILIDADES --------------------------------------------------------
    void HideMenu() { if (menuRoot != null) menuRoot.SetActive(false); }
    void SetStatus(string s) { if (txtStatus != null) txtStatus.text = s; Debug.Log("[MainMenu] " + s); }

    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip.ToString();
        return "127.0.0.1";
    }

    // Reflection-light way to set transport address on FishNet components:
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
                    if (p.Name.ToLower().Contains("port") && p.PropertyType == typeof(string) && p.CanWrite)
                    {
                        // some transports expose port as string
                        p.SetValue(c, portText);
                    }
                }
                catch { /* ignore reflection errors */ }
            }
        }
    }
}