using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// X-Ray sistemini yöneten Singleton Manager - Event-driven ve senkronize
/// SOLID: Single Responsibility - Sadece X-Ray akışını yönetir
/// Pattern: Singleton, Event-Driven
///
/// Akış:
/// 1. Player BaggageXray circle'a girer
/// 2. Player'ın bavulları XRayStartStack'e yüklenir
/// 3. Platform animasyonu başlar
/// 4. Platform TOP'a her ulaştığında → En alttaki bavul X-Ray'e gönderilir
/// 5. Bavul X-Ray yolunu tamamlarken Platform aşağı iner (senkronize)
/// 6. Bavul X-Ray'den çıkınca TruckStack'e yüklenir
/// 7. Tüm bavullar bitene kadar devam eder
/// </summary>
public class BaggageXrayManager : MonoBehaviour
{
    public static BaggageXrayManager Instance { get; private set; }

    [Header("Stack Positions")]
    [SerializeField] private BaggageStack xrayStartStack; // Bavulların başlangıçta stacklendiği yer
    [SerializeField] private BaggageStack truckStack;      // Kamyonetteki stack

    [Header("X-Ray Path")]
    [SerializeField] private Transform[] xrayPathPoints; // X-Ray yolu waypoints
    [SerializeField] private float xrayPathDuration = 4f; // X-Ray yolu süresi (Platform cycle ile senkronize!)

    [Header("Platform")]
    [SerializeField] private PlatformMover platform; // Yukarı aşağı inen platform

    [Header("References")]
    [SerializeField] private PlayerBaggageHolder playerBaggageHolder;

    private bool isProcessing = false;
    private int processedBaggageCount = 0;

    private void Awake()
    {
        // Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"Multiple BaggageXrayManager instances! Destroying {name}");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.PlatformReachedTop += OnPlatformReachedTop;
        EventBus.BaggageCompletedXray += OnBaggageCompletedXray;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.PlatformReachedTop -= OnPlatformReachedTop;
        EventBus.BaggageCompletedXray -= OnBaggageCompletedXray;
    }

    private void OnPlayerEnteredCircle(CircleType circleType)
    {
        // Sadece BaggageXray circle için çalış
        if (circleType != CircleType.BaggageXray)
            return;

        Debug.Log("🔵 [BaggageXrayManager] Player entered BaggageXray circle. Starting X-Ray process...");
        StartXrayProcess();
    }

    private void OnPlayerExitedCircle(CircleType circleType)
    {
        if (circleType == CircleType.BaggageXray)
        {
            Debug.Log("🔴 [BaggageXrayManager] Player exited BaggageXray circle.");
        }
    }

    /// <summary>
    /// X-Ray işlemini başlat
    /// </summary>
    private void StartXrayProcess()
    {
        if (isProcessing)
        {
            Debug.LogWarning("[BaggageXrayManager] Already processing!");
            return;
        }

        // Validation
        if (!ValidateReferences())
            return;

        // Platform cycle süresi ile X-Ray path süresini senkronize et
        float platformCycleDuration = platform.GetCycleDuration();
        if (Mathf.Abs(xrayPathDuration - platformCycleDuration) > 0.1f)
        {
            Debug.LogWarning($"[BaggageXrayManager] X-Ray duration ({xrayPathDuration}s) != Platform cycle ({platformCycleDuration}s). Adjusting...");
            xrayPathDuration = platformCycleDuration;
        }

        StartCoroutine(UnloadBaggagesToStack());
    }

    /// <summary>
    /// Player'ın bavullarını XRayStartStack'e yükle
    /// </summary>
    private IEnumerator UnloadBaggagesToStack()
    {
        isProcessing = true;
        processedBaggageCount = 0;

        Debug.Log("📦 [BaggageXrayManager] Unloading baggages to XRay start stack...");

        int baggageCount = 0;

        // Tüm bavulları stack'e yükle
        while (true)
        {
            GameObject baggage = playerBaggageHolder.RemoveBaggage();

            if (baggage == null)
                break;

            xrayStartStack.AddBaggage(baggage);
            baggageCount++;

            yield return new WaitForSeconds(0.2f); // Görsel için kısa delay
        }

        Debug.Log($"✅ [BaggageXrayManager] {baggageCount} baggages loaded to start stack");

        if (baggageCount > 0)
        {
            // Platform animasyonunu başlat
            Debug.Log("🎬 [BaggageXrayManager] Starting platform animation...");
            platform.MoveToTop(); // İlk hareketi manuel başlat
        }
        else
        {
            Debug.LogWarning("[BaggageXrayManager] No baggages to process!");
            isProcessing = false;
        }
    }

    /// <summary>
    /// Platform TOP'a ulaştığında çağrılır
    /// </summary>
    private void OnPlatformReachedTop()
    {
        if (!isProcessing)
            return;

        Debug.Log("🔝 [BaggageXrayManager] Platform reached TOP - Sending next baggage to X-Ray...");
        SendNextBaggageToXray();
    }

    /// <summary>
    /// Stack'ten bir bavul al ve X-Ray yoluna gönder
    /// </summary>
    private void SendNextBaggageToXray()
    {
        // En alttaki bavulu al (FIFO)
        GameObject baggage = xrayStartStack.RemoveFromBottom();

        if (baggage == null)
        {
            Debug.Log("✅ [BaggageXrayManager] All baggages processed!");
            isProcessing = false;
            return;
        }

        Debug.Log($"🔍 [{baggage.name}] Sending to X-Ray path...");

        // BaggageXrayMover component ekle
        BaggageXrayMover mover = baggage.GetComponent<BaggageXrayMover>();
        if (mover == null)
        {
            mover = baggage.AddComponent<BaggageXrayMover>();
        }

        // X-Ray path waypoints'leri array'e çevir
        Vector3[] pathPoints = new Vector3[xrayPathPoints.Length];
        for (int i = 0; i < xrayPathPoints.Length; i++)
        {
            pathPoints[i] = xrayPathPoints[i].position;
        }

        // X-Ray yolunu başlat (Platform cycle süresi ile senkronize)
        mover.StartXrayPath(pathPoints, xrayPathDuration);

        // Platform'u aşağı gönder (Bavul X-Ray'deyken platform aşağı inecek)
        platform.MoveToBottom();
    }

    /// <summary>
    /// Bavul X-Ray yolunu tamamladığında çağrılır
    /// </summary>
    private void OnBaggageCompletedXray(GameObject baggage)
    {
        Debug.Log($"✅ [{baggage.name}] Completed X-Ray. Loading to truck...");

        // Truck stack'ine ekle
        truckStack.AddBaggage(baggage);
        processedBaggageCount++;

        // Hala bavul var mı?
        if (!xrayStartStack.IsEmpty)
        {
            Debug.Log($"⏳ [BaggageXrayManager] {xrayStartStack.Count} baggage(s) remaining. Platform moving to top...");
            // Platform tekrar yukarı çıksın
            platform.MoveToTop();
        }
        else
        {
            Debug.Log($"🎉 [BaggageXrayManager] All {processedBaggageCount} baggages processed successfully!");
            isProcessing = false;
        }
    }

    /// <summary>
    /// Referans kontrolü
    /// </summary>
    private bool ValidateReferences()
    {
        if (xrayStartStack == null)
        {
            Debug.LogError("[BaggageXrayManager] xrayStartStack is not assigned!");
            return false;
        }

        if (truckStack == null)
        {
            Debug.LogError("[BaggageXrayManager] truckStack is not assigned!");
            return false;
        }

        if (platform == null)
        {
            Debug.LogError("[BaggageXrayManager] platform is not assigned!");
            return false;
        }

        if (xrayPathPoints == null || xrayPathPoints.Length == 0)
        {
            Debug.LogError("[BaggageXrayManager] xrayPathPoints is empty!");
            return false;
        }

        if (playerBaggageHolder == null)
        {
            Debug.LogError("[BaggageXrayManager] playerBaggageHolder is not assigned!");
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        ValidateReferences();

        if (platform != null)
        {
            float platformCycle = platform.GetCycleDuration();
            if (Mathf.Abs(xrayPathDuration - platformCycle) > 0.1f)
            {
                Debug.LogWarning($"[BaggageXrayManager] ⚠️ X-Ray duration ({xrayPathDuration}s) should match Platform cycle ({platformCycle}s) for sync!");
            }
        }
    }
}
