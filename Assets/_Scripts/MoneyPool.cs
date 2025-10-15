using System.Collections.Generic;
using UnityEngine;

public class MoneyPool : MonoBehaviour
{
    public static MoneyPool Instance { get; private set; }

    // Havuzlarý prefab'a göre saklýyoruz
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // Her prefab için bugüne kadar oluþturulan örnek sayýsý
    private Dictionary<GameObject, int> createdCounts = new Dictionary<GameObject, int>();

    // Her prefab için maksimum izin verilen eþzamanlý örnek sayýsý (0 = sýnýrsýz)
    private Dictionary<GameObject, int> maxInstances = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    // Havuzdan bir nesne al (yoksa instantiate et)
    // Eðer prefab için max ayarlýysa ve aktif örnek sayýsý max'a ulaþmýþsa null döner
    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        // Havuzda varsa ver
        if (queue.Count > 0)
        {
            var obj = queue.Dequeue();
            obj.SetActive(true);
            return obj;
        }

        // Havuz boþ: maksimum kontrolü yap
        if (maxInstances.TryGetValue(prefab, out var max) && max > 0)
        {
            createdCounts.TryGetValue(prefab, out var created);
            int activeCount = created - queue.Count; // queue.Count == 0 burada
            if (activeCount >= max)
            {
                // Maksimum aktif örnek sayýsýna ulaþýldý; yeni örnek oluþturma
                return null;
            }
        }

        var instance = Instantiate(prefab);
        instance.name = prefab.name + "_pooled";
        instance.SetActive(true);

        // MoneyMover varsa prefab referansýný ayarla, yoksa ekle
        var mover = instance.GetComponent<MoneyMover>();
        if (mover == null) mover = instance.AddComponent<MoneyMover>();
        mover.prefab = prefab;

        // Oluþturulan sayýyý artýr
        createdCounts.TryGetValue(prefab, out var cur);
        createdCounts[prefab] = cur + 1;

        return instance;
    }

    // Nesneyi havuza iade et
    public void Return(GameObject prefab, GameObject instance)
    {
        if (prefab == null || instance == null) return;

        instance.SetActive(false);

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        queue.Enqueue(instance);
    }

    // Bir prefab için maksimum sayýyý manuel ayarla (0 = sýnýrsýz)
    public void SetMaxInstances(GameObject prefab, int max)
    {
        if (prefab == null) return;
        if (max <= 0)
        {
            maxInstances.Remove(prefab);
            return;
        }

        maxInstances[prefab] = max;
    }

    // Bir prefab için maliyete göre maksimumu otomatik hesapla.
    // Örn: divisor = 5 -> max = Mathf.Max(1, Mathf.CeilToInt(cost / 5f))
    public void ConfigureMaxByCost(GameObject prefab, int cost, int divisor = 5)
    {
        if (prefab == null) return;
        if (divisor <= 0) divisor = 5;
        int max = Mathf.Max(1, Mathf.CeilToInt(cost / (float)divisor));
        SetMaxInstances(prefab, max);
    }
}