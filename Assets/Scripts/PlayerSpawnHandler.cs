using UnityEngine;
using FishNet.Object;

public class PlayerSpawnHandler : NetworkBehaviour
{
    public override void OnStartClient()
    {
        base.OnStartClient();

        // Solo el jugador local debe informar a la cámara
        if (IsOwner)
        {
            CameraFollow.Instance?.SetTarget(transform, true);
        }
    }
}