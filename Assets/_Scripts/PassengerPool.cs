using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PassengerPool : MonoBehaviour
{
    public static PassengerPool Instance { get; private set; }

    [Header("Pool Settings")]
    [SerializeField] private GameObject passengerPrefab; // Passenger prefab (Inspector'dan atanacak)
    [SerializeField] private int initialPoolSize = 10; // Başlangıç pool boyutu
    [SerializeField] private Transform poolParent; // Pool objelerinin parenti (hiyerarşide düzen için)

    private Queue<GameObject> availablePassengers = new Queue<GameObject>();
    private HashSet<GameObject> activePassengers = new HashSet<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializePool();
    }


    private void InitializePool()
    {
        if (passengerPrefab == null)
        {
            Debug.LogError("[PassengerPool] Passenger prefab is not assigned!");
            return;
        }

        if (poolParent == null)
        {
            GameObject parent = new GameObject("PassengerPool");
            poolParent = parent.transform;
            poolParent.SetParent(transform);
        }

        // Başlangıç pool'unu oluştur
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject passenger = Instantiate(passengerPrefab, poolParent);
            passenger.name = $"Passenger_Pooled_{i}";
            passenger.SetActive(false);
            availablePassengers.Enqueue(passenger);
        }

        Log($" Passenger pool initialized with {initialPoolSize} passengers");
    }


    public GameObject GetPassenger()
    {
        GameObject passenger;

        if (availablePassengers.Count > 0)
        {
            // Pool'dan mevcut passenger'ı al
            passenger = availablePassengers.Dequeue();
            passenger.SetActive(true);
            Log($"📤 Retrieved passenger from pool | Available: {availablePassengers.Count}");
        }
        else
        {
            // Pool boş - yeni passenger oluştur
            passenger = Instantiate(passengerPrefab, poolParent);
            passenger.name = $"Passenger_Pooled_Dynamic_{activePassengers.Count}";
            passenger.SetActive(true);
            Log($"➕ Created new passenger (pool was empty) | Active: {activePassengers.Count + 1}");
        }

        // Aktif liste'ye ekle
        activePassengers.Add(passenger);

        // PassengerController'ı reset et (state ve pozisyon)
        var controller = passenger.GetComponent<PassengerController>();
        if (controller != null)
        {
            controller.currentState = PassengerState.Waiting;
            controller.rb.isKinematic = true;
        }

        return passenger;
    }


    public void ReturnPassenger(GameObject passenger)
    {
        if (passenger == null)
        {
            Debug.LogWarning("[PassengerPool] Tried to return null passenger");
            return;
        }

        // Aktif listeden çıkar
        if (activePassengers.Contains(passenger))
        {
            activePassengers.Remove(passenger);
        }

        // Reset işlemleri - tüm hareketleri durdur
        var controller = passenger.GetComponent<PassengerController>();
        if (controller != null)
        {
            // Tüm hareketleri, tweenleri, coroutineleri durdur
            controller.StopAllMovement();
        }

        // Pool'a geri koy
        passenger.SetActive(false);
        passenger.transform.SetParent(poolParent);
        availablePassengers.Enqueue(passenger);

        Log($"📥 Returned passenger to pool | Available: {availablePassengers.Count} | Active: {activePassengers.Count}");
    }

    public void ReturnAllPassengers()
    {
        // ToArray kullan çünkü loop sırasında collection değişiyor
        foreach (var passenger in activePassengers.ToArray())
        {
            ReturnPassenger(passenger);
        }

        Log($"🧹 All passengers returned to pool | Available: {availablePassengers.Count}");
    }


    public int GetActiveCount()
    {
        return activePassengers.Count;
    }


    public int GetAvailableCount()
    {
        return availablePassengers.Count;
    }

    private void Log(string msg)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[PassengerPool] {msg}");
    }
}
