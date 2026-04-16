// Assets/Scripts/NetworkStateWatcher.cs
using UnityEngine;
using FishNet.Managing;

[RequireComponent(typeof(MonoBehaviour))]
public class NetworkStateWatcher : MonoBehaviour
{
    NetworkManager netManager;
    bool wasServer = false;
    bool wasClient = false;
    MainMenuController menuCtrl;

    void Awake()
    {
        netManager = FindObjectOfType<NetworkManager>();
        menuCtrl = FindObjectOfType<MainMenuController>();
        wasServer = netManager != null && netManager.IsServer;
        wasClient = netManager != null && netManager.IsClient;
    }

    void Update()
    {
        if (netManager == null) return;

        bool nowServer = netManager.IsServer;
        bool nowClient = netManager.IsClient;

        if (!wasServer && nowServer)
        {
            Debug.Log("[NetworkStateWatcher] Server started");
            if (menuCtrl != null) menuCtrl.OnServerStarted();
        }

        if (!wasClient && nowClient)
        {
            Debug.Log("[NetworkStateWatcher] Client connected");
            if (menuCtrl != null) menuCtrl.OnClientConnected();
        }

        wasServer = nowServer;
        wasClient = nowClient;
    }
}