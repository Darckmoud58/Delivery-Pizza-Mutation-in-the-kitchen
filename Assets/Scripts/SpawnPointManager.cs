using UnityEngine;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance;
    public Transform[] spawnPoints;
    int nextIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    public Transform GetNextSpawn()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;
        Transform t = spawnPoints[nextIndex];
        nextIndex = (nextIndex + 1) % spawnPoints.Length;
        return t;
    }
}