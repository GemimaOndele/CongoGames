using System.Collections.Generic;
using UnityEngine;

namespace CongoGames.Perf
{
    public class ObjectPoolManager : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private int initialSize = 16;

        private readonly Queue<GameObject> pool = new Queue<GameObject>();

        private void Start()
        {
            for (int i = 0; i < initialSize; i++)
            {
                GameObject instance = Instantiate(prefab, transform);
                instance.SetActive(false);
                pool.Enqueue(instance);
            }
        }

        public GameObject Get()
        {
            GameObject item = pool.Count > 0 ? pool.Dequeue() : Instantiate(prefab, transform);
            item.SetActive(true);
            return item;
        }

        public void Release(GameObject item)
        {
            item.SetActive(false);
            pool.Enqueue(item);
        }
    }
}
