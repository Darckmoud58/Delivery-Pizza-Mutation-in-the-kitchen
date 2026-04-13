using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class GestiondeRed : MonoBehaviourPunCallbacks
{
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Conectando a Photon...");
        PhotonNetwork.AutomaticallySyncScene = true;   // opcional, pero útil
        PhotonNetwork.ConnectUsingSettings();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Conectado al Master. Uniéndose/creando sala...");
        RoomOptions opciones = new RoomOptions();
        opciones.MaxPlayers = 2;

        PhotonNetwork.JoinOrCreateRoom("SalaPizza", opciones, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
       Debug.Log("Entré a la sala. Jugadores: " + PhotonNetwork.CurrentRoom.PlayerCount);
        // Como de momento usas una sola escena, no cargamos otra.
        // Cuando tengas la escena final, aquí usarías PhotonNetwork.LoadLevel("EscenaCocina");
    }
}
