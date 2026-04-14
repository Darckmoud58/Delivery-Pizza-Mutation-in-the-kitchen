using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GestorPhoton : MonoBehaviourPunCallbacks
{
    [Header("Prefabs jugadores")]
    public string prefabChef = "Chef";
    public string prefabRepartidor = "Repartidor";

    [Header("Spawn jugadores (inicio del nivel)")]
    public Vector3 spawnChef = new Vector3(-6f, -2f, 0f);
    public Vector3 spawnRepartidor = new Vector3(-4f, -2f, 0f);

    public override void OnJoinedRoom()
    {
        bool soyMaster = PhotonNetwork.IsMasterClient;
        string prefab = soyMaster ? prefabChef : prefabRepartidor;
        Vector3 pos = soyMaster ? spawnChef : spawnRepartidor;
        PhotonNetwork.Instantiate(prefab, pos, Quaternion.identity);
    }

    public override void OnDisconnected(Photon.Realtime.DisconnectCause cause)
    {
        Debug.LogWarning("Desconectado: " + cause);
        SceneManager.LoadScene(0);
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        Debug.Log("Nuevo MasterClient: " + newMasterClient.NickName);
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
