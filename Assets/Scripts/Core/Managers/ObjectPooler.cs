using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple singleton object pooler.
/// </summary>
public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance;

    [SerializeField] private List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // DontDestroyOnLoad(gameObject); // Optional

        // Initialize pools
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                obj.transform.SetParent(transform);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
            return null;
        }

        GameObject objectToSpawn;
        
        if (poolDictionary[tag].Count > 0)
        {
             objectToSpawn = poolDictionary[tag].Dequeue();
        }
        else
        {
            // Pool empty, create new one (Expandable pool)
            // Ideally we should find the prefab from 'pools' list
            Pool poolInfo = pools.Find(p => p.tag == tag);
            if(poolInfo != null && poolInfo.prefab != null) {
                 objectToSpawn = Instantiate(poolInfo.prefab);
                 objectToSpawn.transform.SetParent(transform);
            } else {
                 return null;
            }
        }
        
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        return objectToSpawn;
    }

    public void ReturnToPool(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
             // Create queue if not exists? Or just warning.
             // Usually warning.
             Debug.LogWarning($"Pool with tag {tag} doesn't exist.");
             return;
        }

        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }
}
