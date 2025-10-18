using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bagaj boşaltma sistemini yöneten Singleton Manager
/// SOLID: Single Responsibility - Sadece bagaj unload akışını yönetir
/// Pattern: Singleton - Tek instance
/// Pattern: Event-Driven - EventBus üzerinden iletişim
/// </summary>
public class BaggageUnloadManager : MonoBehaviour
{
    public static BaggageUnloadManager Instance { get; private set; }

    [Header("Unload Path Settings")]
    [SerializeField] private Transform baggageUnloadStartPoint;
    [SerializeField] private Transform baggageUnloadEndPoint;
    [SerializeField] private float conveyorSpeed = 2f;

    [Header("Platform Settings")]
    [SerializeField] private Transform platformTarget; // Yukarı aşağı inen platform
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float jumpDuration = 0.5f;

    [Header("Truck Settings")]
    [SerializeField] private Transform truckLoadPoint; // Kamyonet kasası içindeki nokta
    [SerializeField] private Vector3 truckBaggageSpacing = new Vector3(0.5f, 0, 0); // Bavullar arası mesafe
    private int index = 0;
    [Header("References")]
    [SerializeField] private PlayerBaggageHolder playerBaggageHolder;

    private Queue<GameObject> baggageQueue = new Queue<GameObject>();
    private List<GameObject> loadedBaggages = new List<GameObject>(); // Kamyonete yüklenenler
    private bool isUnloading = false;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"Multiple BaggageUnloadManager instances detected! Destroying {name}");
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.BaggageReachedUnloadEnd += OnBaggageReachedUnloadEnd;
        EventBus.BaggageReachedPlatform += OnBaggageReachedPlatform;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.BaggageReachedUnloadEnd -= OnBaggageReachedUnloadEnd;
        EventBus.BaggageReachedPlatform -= OnBaggageReachedPlatform;
    }

    private void OnPlayerEnteredCircle(CircleType circleType)
    {
        // Sadece BaggageUnload circle için çalış
        if (circleType != CircleType.BaggageUnload)
            return;

        Debug.Log("🔵 [BaggageUnloadManager] Player entered BaggageUnload circle. Starting unload process...");
        StartUnloadingBaggages();
    }

    private void OnPlayerExitedCircle(CircleType circleType)
    {
        if (circleType == CircleType.BaggageUnload)
        {
            Debug.Log("🔴 [BaggageUnloadManager] Player exited BaggageUnload circle.");
        }
    }

    /// <summary>
    /// Player'ın elindeki bavulları sırayla boşaltmaya başlar
    /// </summary>
    private void StartUnloadingBaggages()
    {
        if (isUnloading)
        {
            Debug.LogWarning("[BaggageUnloadManager] Already unloading!");
            return;
        }

        if (playerBaggageHolder == null)
        {
            Debug.LogError("[BaggageUnloadManager] PlayerBaggageHolder reference is null!");
            return;
        }

        StartCoroutine(UnloadBaggagesSequentially());
    }

    /// <summary>
    /// Bavulları sırayla player'dan alır ve conveyor belt'e gönderir
    /// </summary>
    private IEnumerator UnloadBaggagesSequentially()
    {
        isUnloading = true;

        while (true)
        {
            GameObject baggage = playerBaggageHolder.RemoveBaggage();

            if (baggage == null)
            {
                Debug.Log("✅ [BaggageUnloadManager] All baggages unloaded!");
                break;
            }

            Debug.Log($"📦 [BaggageUnloadManager] Unloading baggage: {baggage.name}");

            // BaggageMover component ekle ve başlat
            BaggageMover mover = baggage.AddComponent<BaggageMover>();
            // mover.Initialize(baggageUnloadStartPoint.position, baggageUnloadEndPoint.position, conveyorSpeed);

            baggageQueue.Enqueue(baggage);
            index++;
            BaggageStackHandler(baggage);
            // Bir sonraki bavul için kısa delay
            yield return new WaitForSeconds(0.5f);
        }
        isUnloading = false;
    }

    private void BaggageStackHandler(GameObject baggage)
    {
        Vector3 stackPosition = baggageUnloadStartPoint.position + new Vector3(0, index * 0.35f, 0);

        baggage.transform
            .DOJump(stackPosition, 1f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                baggage.transform.SetParent(baggageUnloadStartPoint);
                //baggage.transform.localPosition = new Vector3(0, baggageQueue.Count * 0.2f, 0);
                baggage.transform.localRotation = Quaternion.Euler(0, 0, 90); // ✨
            });

        
    }

    /// <summary>
    /// Bavul conveyor belt sonuna ulaştığında
    /// </summary>
    private void OnBaggageReachedUnloadEnd(GameObject baggage)
    {
        Debug.Log($"🎯 [BaggageUnloadManager] {baggage.name} reached unload end. Jumping to platform...");

        // Platform'a jump
        BaggageMover mover = baggage.GetComponent<BaggageMover>();
        if (mover != null)
        {
            mover.JumpToPlatform(platformTarget, jumpHeight, jumpDuration);
        }
    }

    /// <summary>
    /// Bavul platform'a ulaştığında
    /// </summary>
    private void OnBaggageReachedPlatform(GameObject baggage)
    {
        Debug.Log($"🎯 [BaggageUnloadManager] {baggage.name} reached platform. Waiting for platform to reach top...");

        // Platform en üste çıktığında kamyonete yüklenecek (PlatformMover trigger edecek)
        StartCoroutine(WaitForPlatformTop(baggage));
    }

    /// <summary>
    /// Platform en üst noktaya geldiğinde bavulu kamyonete yükler
    /// </summary>
    private IEnumerator WaitForPlatformTop(GameObject baggage)
    {
        // PlatformMover'dan bildirim bekle (daha iyi bir yaklaşım event olur ama basit tutalım)
        yield return new WaitForSeconds(2f); // Platform cycle süresi

        Debug.Log($"📦 [BaggageUnloadManager] Loading {baggage.name} to truck...");

        // Kamyonete yükle
        LoadBaggageToTruck(baggage);
    }

    /// <summary>
    /// Bavulu kamyonet kasasına yükler
    /// </summary>
    private void LoadBaggageToTruck(GameObject baggage)
    {
        BaggageMover mover = baggage.GetComponent<BaggageMover>();
        if (mover != null)
        {
            // Yeni pozisyon hesapla (sırayla yerleştir)
            Vector3 targetPosition = truckLoadPoint.position + (truckBaggageSpacing * loadedBaggages.Count);

            mover.MoveToTruck(targetPosition);
            loadedBaggages.Add(baggage);

            Debug.Log($"✅ [BaggageUnloadManager] {baggage.name} loaded to truck. Total loaded: {loadedBaggages.Count}");
        }
    }

    /// <summary>
    /// Inspector validation
    /// </summary>
    private void OnValidate()
    {
        if (baggageUnloadStartPoint == null)
            Debug.LogWarning("[BaggageUnloadManager] baggageUnloadStartPoint is not assigned!");

        if (baggageUnloadEndPoint == null)
            Debug.LogWarning("[BaggageUnloadManager] baggageUnloadEndPoint is not assigned!");

        if (platformTarget == null)
            Debug.LogWarning("[BaggageUnloadManager] platformTarget is not assigned!");

        if (truckLoadPoint == null)
            Debug.LogWarning("[BaggageUnloadManager] truckLoadPoint is not assigned!");

        if (playerBaggageHolder == null)
            Debug.LogWarning("[BaggageUnloadManager] playerBaggageHolder is not assigned!");
    }
}
