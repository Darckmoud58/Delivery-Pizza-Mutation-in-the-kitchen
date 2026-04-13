using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class GestorPhoton : MonoBehaviourPunCallbacks
{
    [Header("Nombres exactos de los prefabs en Resources")]
    public string prefabChef = "Chef";
    public string prefabRepartidor = "Repartidor";

    [Header("Puntos de aparición")]
    public Vector3 spawnChef = new Vector3(-4f, -3f, 0f);
    public Vector3 spawnRepartidor = new Vector3(4f, -3f, 0f);

    public override void OnJoinedRoom()
    {
        bool soyMaster = PhotonNetwork.IsMasterClient;
        string prefab = soyMaster ? prefabChef : prefabRepartidor;
        Vector3 pos = soyMaster ? spawnChef : spawnRepartidor;

        PhotonNetwork.Instantiate(prefab, pos, Quaternion.identity);

        // --- NUEVO: El Master crea el horno ---
        if (soyMaster)
        {
            // Pon aquí la posición donde quieres que aparezca el horno
            Vector3 posHorno = new Vector3(-2.19f, 1.42f, 0f);
            PhotonNetwork.Instantiate("Horno", posHorno, Quaternion.identity);
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
