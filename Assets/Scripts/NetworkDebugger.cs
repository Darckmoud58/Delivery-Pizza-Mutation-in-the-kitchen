using UnityEngine;
using FishNet.Managing;

public class NetworkDebugger : MonoBehaviour
{
    NetworkManager nm;

    void Awake()
    {
        nm = FindObjectOfType<NetworkManager>();
    }

    void Update()
    {
        if (nm == null) return;

        // Estado server/client
        Debug.Log($"[DBG] IsServer={nm.IsServer} IsClient={nm.IsClient}");

        // Conteo de clientes en server (si existe)
        try
        {
            var clientsCount = nm.ServerManager.Clients.Count;
            Debug.Log("[DBG] ServerManager.Clients.Count = " + clientsCount);
        }
        catch { }

        // Intento seguro de leer puerto del transport (GetPort() o Port)
        ushort port = 0;
        try
        {
            var transport = nm.TransportManager?.Transport;
            if (transport != null)
            {
                var mt = transport.GetType();
                var m = mt.GetMethod("GetPort");
                if (m != null) { port = (ushort)(m.Invoke(transport, null) ?? 0); }
                else
                {
                    var p = mt.GetProperty("Port");
                    if (p != null) port = (ushort)(p.GetValue(transport) ?? 0);
                }
                Debug.Log("[DBG] Transport type: " + transport.GetType().FullName + " Port=" + port);
            }
            else Debug.Log("[DBG] Transport is null");
        }
        catch (System.Exception ex) { Debug.Log("[DBG] Error reading transport port: " + ex.Message); }
    }
}