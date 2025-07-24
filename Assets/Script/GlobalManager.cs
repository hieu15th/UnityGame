using UnityEngine;

public class GlobalManager : MonoBehaviour
{
    public GameObject socketManagerPrefab;
    private static bool hasSpawned = false;

    void Awake()
    {
        if (!hasSpawned)
        {
            DontDestroyOnLoad(gameObject);
            Instantiate(socketManagerPrefab);
            hasSpawned = true;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
