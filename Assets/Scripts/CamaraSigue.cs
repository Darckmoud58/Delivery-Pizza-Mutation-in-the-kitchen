using UnityEngine;

public class CamaraSigue : MonoBehaviour
{
    public Transform objetivo;
    
    void LateUpdate()
    {
        if (objetivo == null)
        {
            // Busca al jugador que te pertenece
            foreach(var player in GameObject.FindGameObjectsWithTag("Player"))
            {
                if(player.GetComponent<LogicaJugadorRed>().IsOwner)
                {
                    objetivo = player.transform;
                    break;
                }
            }
        }

        if (objetivo != null)
            transform.position = new Vector3(objetivo.position.x, objetivo.position.y, -10f);
    }
}