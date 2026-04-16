// Assets/Scripts/MainMenuController.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;

public class MainMenuController : MonoBehaviour
{
    [Header("UI (TMP)")]
    public GameObject menuRoot;
    public TMP_InputField inputRoomCode;
    public TMP_InputField inputIP;
    public TMP_Text txtRoomCode;
    public TMP_Text txtStatus;
    public GameObject heartsContainer;

    [Header("Network Settings")]
    public int defaultPort = 7770; // FishNet usa 7770 por defecto en Tugboat
    private NetworkManager netManager;

    [Header("Panel Código Sala")]
    public GameObject panelCodigoSala;
    public TMP_Text txtCodigoSala;
    public CanvasGroup panelCanvasGroup;

    private string currentRoomCode = null;
    private bool hostPrepared = false;
    private bool hostStarted = false;

    void Awake()
    {
        netManager = InstanceFinder.NetworkManager;
        if (heartsContainer != null) heartsContainer.SetActive(false);
        if (txtRoomCode != null) txtRoomCode.text = "";
        if (txtStatus != null) txtStatus.text = "Esperando...";
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        hostPrepared = false;
        hostStarted = false;
    }

    // --- HOST: Preparar sala ---
    public void OnHostPressed()
    {
        if (netManager == null) { SetStatus("NetworkManager no encontrado."); return; }

        string ip = GetLocalIPAddress();
        ushort actualPort = (ushort)defaultPort;

        // Intentamos leer el puerto real del transporte
        if (netManager.TransportManager != null && netManager.TransportManager.Transport != null)
        {
            actualPort = netManager.TransportManager.Transport.GetPort();
        }

        string display = ip + ":" + actualPort;
        currentRoomCode = display;

        if (txtRoomCode != null) txtRoomCode.text = "IP: " + display;

        if (panelCodigoSala != null)
        {
            panelCodigoSala.SetActive(true);
            if (txtCodigoSala != null) txtCodigoSala.text = "IP de la sala:\n" + display;
        }

        if (panelCanvasGroup != null) 
        { 
            panelCanvasGroup.alpha = 1f; 
            panelCanvasGroup.interactable = true; 
            panelCanvasGroup.blocksRaycasts = true; 
        }

        GUIUtility.systemCopyBuffer = display;
        hostPrepared = true;
        hostStarted = false;

        SetStatus("Sala preparada. IP copiada. Pulsa 'Iniciar partida' para arrancar.");
    }

    // --- HOST: Iniciar realidad el servidor ---
    public void ConfirmStartHost()
    {
        if (netManager == null) return;
        if (!hostPrepared) { SetStatus("Primero prepara la sala."); return; }
        
        try
        {
            netManager.ServerManager.StartConnection();
            netManager.ClientManager.StartConnection();
            hostStarted = true;
            
            HideMenu();
            if (heartsContainer != null) heartsContainer.SetActive(true);
            SetStatus("Host iniciado. Esperando jugadores...");
        }
        catch (Exception ex)
        {
            SetStatus("Error al iniciar: " + ex.Message);
        }
    }

    // --- CLIENTE: Unirse a una sala ---
    public void OnJoinPressed()
    {
        if (netManager == null) return;

        string codeOrIp = (inputRoomCode != null) ? inputRoomCode.text.Trim() : "";
        if (string.IsNullOrEmpty(codeOrIp) && inputIP != null) codeOrIp = inputIP.text.Trim();

        if (string.IsNullOrEmpty(codeOrIp))
        {
            SetStatus("Introduce una IP válida.");
            return;
        }

        string targetIP = codeOrIp;
        string targetPort = defaultPort.ToString();

        // Si el usuario puso IP:PUERTO
        if (codeOrIp.Contains(":"))
        {
            string[] parts = codeOrIp.Split(':');
            targetIP = parts[0];
            targetPort = parts[1];
        }

        // --- CONFIGURACIÓN DEL TRANSPORTE ---
        ConfigureTransport(targetIP, targetPort);

        // --- CONECTAR ---
        netManager.ClientManager.StartConnection();

        HideMenu();
        if (panelCodigoSala != null) panelCodigoSala.SetActive(false);
        if (heartsContainer != null) heartsContainer.SetActive(true);

        SetStatus("Conectando a " + targetIP + "...");
    }

    private void ConfigureTransport(string ip, string portText)
    {
        if (netManager.TransportManager == null || netManager.TransportManager.Transport == null) return;

        var transport = netManager.TransportManager.Transport;

        // Caso Tugboat (Estándar de Fish-Net)
        if (transport is FishNet.Transporting.Tugboat.Tugboat tug)
        {
            tug.SetClientAddress(ip);
            if (ushort.TryParse(portText, out ushort p)) tug.SetPort(p);
            return;
        }

        // Caso Genérico por Reflection (por si usas otro)
        try
        {
            var type = transport.GetType();
            var addrProp = type.GetProperty("ClientAddress") ?? type.GetProperty("Address");
            if (addrProp != null) addrProp.SetValue(transport, ip);

            var portProp = type.GetProperty("Port");
            if (portProp != null && ushort.TryParse(portText, out ushort p)) portProp.SetValue(transport, p);
        }
        catch (Exception e) { Debug.LogWarning("Error configurando transporte: " + e.Message); }
    }

    // --- ÚTILES ---
    void HideMenu() { if (menuRoot != null) menuRoot.SetActive(false); }
    void SetStatus(string s) { if (txtStatus != null) txtStatus.text = s; Debug.Log("[Status] " + s); }

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
}