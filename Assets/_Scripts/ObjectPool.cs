using System.Collections.Generic;
using UnityEngine;


public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    // Havuzları prefab'a göre saklıyoruz
    private Dictionary<GameObject, Queue<GameObject>> pools = new Dictionary<GameObject, Queue<GameObject>>();

    // Her prefab için bugüne kadar oluşturulan örnek sayısı
    private Dictionary<GameObject, int> createdCounts = new Dictionary<GameObject, int>();

    // Her prefab için maksimum izin verilen eşzamanlı örnek sayısı (0 = sınırsız)
    private Dictionary<GameObject, int> maxInstances = new Dictionary<GameObject, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


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

        // Havuz boş: maksimum kontrolü yap
        if (maxInstances.TryGetValue(prefab, out var max) && max > 0)
        {
            createdCounts.TryGetValue(prefab, out var created);
            int activeCount = created - queue.Count;
            if (activeCount >= max)
            {
                return null; // Maksimum aktif örnek sayısına ulaşıldı
            }
        }

        // Yeni instance oluştur
        var instance = Instantiate(prefab);
        instance.name = prefab.name + "_pooled";
        instance.SetActive(true);

        // MoneyMover varsa prefab referansını ayarla (sadece para için)
        var mover = instance.GetComponent<MoneyMover>();
        if (mover != null)
        {
            mover.prefab = prefab;
        }

        // Oluşturulan sayıyı artır
        createdCounts.TryGetValue(prefab, out var cur);
        createdCounts[prefab] = cur + 1;

        return instance;
    }

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

    public void ReturnToPool(GameObject instance)
    {
        if (instance == null) return;

        // MoneyMover varsa prefab referansını kullan
        var mover = instance.GetComponent<MoneyMover>();
        if (mover != null && mover.prefab != null)
        {
            Return(mover.prefab, instance);
            return;
        }

        // Bagaj veya diğer objeler için direkt deaktive et
        instance.SetActive(false);

        // Eğer ismi "_pooled" içeriyorsa, orijinal prefab ismini bul
        string originalName = instance.name.Replace("_pooled", "").Replace("(Clone)", "").Trim();

        // Tüm pool'larda ara
        foreach (var kvp in pools)
        {
            if (kvp.Key.name == originalName)
            {
                kvp.Value.Enqueue(instance);
                return;
            }
        }

        // Pool bulunamadıysa yok et
        Debug.LogWarning($"[ObjectPool] No pool found for {instance.name}, destroying instead.");
        Destroy(instance);
    }

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


    public void ConfigureMaxByCost(GameObject prefab, int cost, int divisor = 5)
    {
        if (prefab == null) return;
        if (divisor <= 0) divisor = 5;
        int max = Mathf.Max(1, Mathf.CeilToInt(cost / (float)divisor));
        SetMaxInstances(prefab, max);
    }
}

public class MoneyPool : ObjectPool { }