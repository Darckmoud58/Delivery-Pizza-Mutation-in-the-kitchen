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
    public TMP_InputField inputRoomCode;
    public TMP_InputField inputIP;
    public TMP_Text txtRoomCode;  // El que dice "New Text" arriba
    public TMP_Text txtStatus;    // El que dice "New Text" abajo
    public GameObject heartsContainer;

    [Header("Firebase")]
    public string firebaseBaseUrl;

    [Header("Network")]
    public int defaultPort = 7777;
    NetworkManager netManager;

    string currentRoomCode = null;

    void Awake()
    {
        netManager = FindObjectOfType<NetworkManager>();
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (txtRoomCode != null) txtRoomCode.text = "";
        if (txtStatus != null) txtStatus.text = "Esperando...";
    }

    public void OnHostPressed()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }

        try
        {
            // Inicia servidor + cliente local (host)
            netManager.ServerManager.StartConnection();
            netManager.ClientManager.StartConnection();

            SetStatus("Host iniciado localmente...");
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);

            // Mostrar la IP local y copiarla al portapapeles para pruebas rápidas
            string ip = GetLocalIPAddress();
            if (txtRoomCode != null)
            {
                txtRoomCode.text = "IP: " + ip + ":" + defaultPort;
                GUIUtility.systemCopyBuffer = ip;
            }

            Debug.Log("[MainMenu] Host iniciado. IP local: " + ip + ":" + defaultPort);
        }
        catch (Exception ex)
        {
            SetStatus("Error iniciando Host: " + ex.Message);
            Debug.LogException(ex);
        }

        // Opcional: si quieres mantener el registro en Firebase en background,
        // puedes StartCoroutine(HostRegisterFlow());
    }

    public void OnJoinPressed()
    {
        if (netManager == null) return;
        string codeOrIp = (inputRoomCode != null) ? inputRoomCode.text.Trim() : "";

        if (codeOrIp == "localhost" || codeOrIp.Contains("."))
        {
            TrySetTransportAddressAndPort(codeOrIp, defaultPort.ToString());
            netManager.ClientManager.StartConnection();
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);
            return;
        }

        if (!string.IsNullOrEmpty(codeOrIp))
        {
            StartCoroutine(JoinByRoomCodeCoroutine(codeOrIp));
        }
        else { SetStatus("Escribe un código o IP."); }
    }

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
        else { SetStatus("Error al registrar en Firebase."); }
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
        else { SetStatus("Código no encontrado."); }
    }

    // --- FIREBASE METHODS ---
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

    void HideMenu() { if (menuRoot != null) menuRoot.SetActive(false); }
    void SetStatus(string s) { if (txtStatus != null) txtStatus.text = s; Debug.Log(s); }
    string GetLocalIPAddress()
    {
        var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
        foreach (var ip in host.AddressList) if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) return ip.ToString();
        return "127.0.0.1";
    }

    void TrySetTransportAddressAndPort(string ip, string portText)
    {
        Component[] comps = netManager.GetComponents<Component>();
        foreach (var c in comps)
        {
            var props = c.GetType().GetProperties();
            foreach (var p in props)
            {
                if (p.Name.ToLower().Contains("address") && p.PropertyType == typeof(string) && p.CanWrite) p.SetValue(c, ip);
            }
        }
    }
}