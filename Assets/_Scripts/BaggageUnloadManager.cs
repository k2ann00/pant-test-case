using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class BaggageUnloadManager : MonoBehaviour
{
    public static BaggageUnloadManager Instance { get; private set; }

    [Header("Unload Path Settings")]
    [SerializeField] private Transform baggageUnloadStartPoint;
    [SerializeField] private Transform baggageUnloadJumpPoint;
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

    private Stack<GameObject> baggageStack = new Stack<GameObject>(); // Stack: LIFO (Last In First Out - en üstteki önce)
    private Stack<GameObject> loadedBaggages = new Stack<GameObject>(); // Stack: LIFO (Last In First Out - en üstteki önce)
    private int loadedBaggageCount = 0; // Kamyonete yüklenen bagaj sayısı (tracking için)
    private bool isUnloading = false;
    private int totalBaggageCount = 0; // Toplam bagaj sayısı
    private Coroutine stackingCoroutine; // Track stacking coroutine
    private bool isPlayerInCircle = false; // Track if player is in BaggageUnload circle
    private bool isPlayerInBaggageXray = false; // Track if player is in BaggageXray circle

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
        EventBus.BaggageReachedTruck += OnBaggageReachedTruck;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.BaggageReachedUnloadEnd -= OnBaggageReachedUnloadEnd;
        EventBus.BaggageReachedPlatform -= OnBaggageReachedPlatform;
        EventBus.BaggageReachedTruck -= OnBaggageReachedTruck;
    }

    private void OnPlayerEnteredCircle(CircleType circleType)
    {
        // WelcomingCircle'da bagajlar zaten player'a geliyor (PassengerController hallediyor)
        // BaggageUnload circle'a girince bagajları stack ediyoruz
        if (circleType == CircleType.BaggageUnload)
        {
            Debug.Log("[BaggageUnloadManager] Player entered BaggageUnload circle. Stacking baggages...");
            isPlayerInCircle = true;
            Debug.Log($"[BaggageUnloadManager] isPlayerInCircle SET to: {isPlayerInCircle}");
            StartStackingBaggages();
        }
        // BaggageXray circle'a girince conveyor hareketini başlat veya devam ettir
        else if (circleType == CircleType.BaggageXray)
        {
            Debug.Log("[BaggageUnloadManager] Player entered BaggageXray circle. Resuming conveyor movement...");
            isPlayerInBaggageXray = true;
            Debug.Log($"[BaggageUnloadManager] isPlayerInBaggageXray SET to: {isPlayerInBaggageXray}");

            // Eğer stack'te bagaj varsa
            if (baggageStack.Count > 0)
            {
                // İlk girişte totalBaggageCount set et
                if (totalBaggageCount == 0)
                {
                    totalBaggageCount = baggageStack.Count;
                    Debug.Log($"[BaggageUnloadManager] Set totalBaggageCount = {totalBaggageCount}");
                }

                Debug.Log($"[BaggageUnloadManager] {baggageStack.Count} baggages remaining in stack. Sending next baggage...");
                SendNextBaggageToConveyor();
            }
            else
            {
                Debug.Log("[BaggageUnloadManager] No baggages remaining in stack.");
            }
        }
    }

    private void OnPlayerExitedCircle(CircleType circleType)
    {
        if (circleType == CircleType.BaggageUnload)
        {
            Debug.Log("[BaggageUnloadManager] Player exited BaggageUnload circle. Stopping operations...");
            isPlayerInCircle = false;
            Debug.Log($"[BaggageUnloadManager] isPlayerInCircle SET to: {isPlayerInCircle}");

            // Stop stacking coroutine if running
            if (stackingCoroutine != null)
            {
                StopCoroutine(stackingCoroutine);
                stackingCoroutine = null;
                isUnloading = false;
                Debug.Log("[BaggageUnloadManager] Stacking coroutine STOPPED");
            }
        }
        else if (circleType == CircleType.BaggageXray)
        {
            Debug.Log("[BaggageUnloadManager] Player exited BaggageXray circle. No new baggages will be sent...");
            isPlayerInBaggageXray = false;
            Debug.Log($"[BaggageUnloadManager] isPlayerInBaggageXray SET to: {isPlayerInBaggageXray}");

            // DON'T stop coroutines or tweens - let active baggages finish their animation
            // Only prevent NEW baggages from being sent (handled by isPlayerInBaggageXray checks)
            Debug.Log("[BaggageUnloadManager] Active baggages will complete their animation, but no new baggages will be sent");
        }
    }

    private void StartStackingBaggages()
    {
        if (isUnloading)
        {
            Debug.LogWarning("[BaggageUnloadManager] Already stacking!");
            return;
        }

        if (playerBaggageHolder == null)
        {
            Debug.LogError("[BaggageUnloadManager] PlayerBaggageHolder reference is null!");
            return;
        }

        stackingCoroutine = StartCoroutine(StackBaggagesSequentially());
    }


    private IEnumerator StackBaggagesSequentially()
    {
        isUnloading = true;
        // DON'T reset index - it should continue from where it left off
        // index is reset only when starting fresh or when conveyor movement starts

        Debug.Log($"[BaggageUnloadManager] Starting stacking from index: {index}");

        while (isPlayerInCircle) // Only continue while player is in circle
        {
            GameObject baggage = playerBaggageHolder.RemoveBaggage();

            if (baggage == null)
            {
                Debug.Log($" [BaggageUnloadManager] All baggages stacked! Total in stack: {baggageStack.Count}");
                break;
            }

            Debug.Log($"  [BaggageUnloadManager] Stacking baggage: {baggage.name} at index: {index}");

            // BaggageMover component ekle (henüz hareket ettirme)
            if (baggage.GetComponent<BaggageMover>() == null)
                baggage.AddComponent<BaggageMover>();

            baggageStack.Push(baggage); // Stack'e ekle (LIFO - en üstteki önce çıkar)

            // Stack pozisyonuna zıplat (Y ekseninde üst üste - Z eksenine bakacak şekilde)
            Vector3 stackPosition = baggageUnloadStartPoint.position + new Vector3(0, index * 0.35f, 0);
            baggage.transform
                .DOJump(stackPosition, 1f, 1, 0.6f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    baggage.transform.SetParent(baggageUnloadStartPoint);
                    // Conveyor rotasyonu ile aynı - Z eksenine bakacak
                    baggage.transform.localRotation = Quaternion.Euler(0, 90, 90);
                });

            index++;

            // Bir sonraki bavul için kısa delay
            yield return new WaitForSeconds(0.5f);
        }

        isUnloading = false;
        stackingCoroutine = null;

        // If player exited early, log it
        if (!isPlayerInCircle)
        {
            Debug.Log($"[BaggageUnloadManager] Stacking stopped - player exited circle. Current stack count: {baggageStack.Count}, index: {index}");
        }
    }


    private void StartConveyorMovement()
    {
        if (baggageStack.Count == 0)
        {
            Debug.LogWarning("[BaggageUnloadManager] No baggages to move on conveyor!");
            return;
        }

        // Set total baggage count for this conveyor session (ONLY if not set)
        if (totalBaggageCount == 0)
        {
            totalBaggageCount = baggageStack.Count;
            Debug.Log($"[BaggageUnloadManager] Starting NEW conveyor session for {totalBaggageCount} baggages (top to bottom)");
        }
        else
        {
            Debug.Log($"[BaggageUnloadManager] RESUMING conveyor session. Total: {totalBaggageCount}, Loaded: {loadedBaggages.Count}, Remaining: {baggageStack.Count}");
        }

        // En üstteki bagajı gönder
        SendNextBaggageToConveyor();
    }


    private void SendNextBaggageToConveyor()
    {
        if (baggageStack.Count == 0)
        {
            Debug.Log(" [BaggageUnloadManager] All baggages sent to conveyor!");
            // Stack boşaldı, index'i sıfırla (yeni session için)
            index = 0;
            Debug.Log($"[BaggageUnloadManager] Stack empty - index reset to 0");
            return;
        }

        GameObject baggage = baggageStack.Pop(); // En üsttekini al (LIFO)

        if (baggage == null)
        {
            // Null ise bir sonrakini dene
            SendNextBaggageToConveyor();
            return;
        }

        Debug.Log($"  [BaggageUnloadManager] Sending {baggage.name} to conveyor (from top of stack)");

        // Parent'ı kaldır
        baggage.transform.SetParent(null);

        // Önce conveyor start pozisyonuna zıpla, sonra conveyor hareketini başlat
        Quaternion conveyorRotation = Quaternion.Euler(0f, 90f, 90f);

        baggage.transform
            .DOJump(baggageUnloadJumpPoint.position, 0.5f, 1, 0.4f)
            .SetEase(Ease.OutQuad)
            .OnStart(() =>
            {
                // Zıplarken rotasyonu ayarla
                baggage.transform.rotation = conveyorRotation;
            })
            .OnComplete(() =>
            {
                // Zıplama bitince conveyor hareketini başlat
                BaggageMover mover = baggage.GetComponent<BaggageMover>();
                if (mover != null)
                {
                    mover.Initialize(baggageUnloadJumpPoint.position, baggageUnloadEndPoint.position, conveyorSpeed, conveyorRotation);
                }
            });
    }


    private void OnBaggageReachedUnloadEnd(GameObject baggage)
    {
        Debug.Log($"[BaggageUnloadManager] {baggage.name} reached unload end. Jumping to platform...");

        // Platform'a jump
        BaggageMover mover = baggage.GetComponent<BaggageMover>();
        if (mover != null)
        {
            mover.JumpToPlatform(platformTarget, jumpHeight, jumpDuration);
        }
    }

    private void OnBaggageReachedPlatform(GameObject baggage)
    {
        Debug.Log($"[BaggageUnloadManager] {baggage.name} reached platform. Starting platform rise...");

        // Platform'u yukarı gönder
        if (PlatformMover.Instance != null)
        {
            PlatformMover.Instance.MoveToTop();
        }

        // Platform yukarı çıkınca bu bagaj kamyona zıplayacak
        // Coroutine player çıksa bile bu bagajın işlemini tamamlar
        StartCoroutine(WaitForPlatformTopAndJumpToTruck(baggage));
    }

    private IEnumerator WaitForPlatformTopAndJumpToTruck(GameObject baggage)
    {
        // Platform en tepeye gelene kadar bekle (player çıksa bile bu bagaj işini tamamlamalı)
        while (PlatformMover.Instance != null && !PlatformMover.Instance.IsAtTop)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"  [BaggageUnloadManager] Platform at top! Jumping {baggage.name} to truck...");

        // Kamyonete jump (bu bagaj işini tamamlamalı)
        JumpBaggageToTruck(baggage);

        // Platform'u geri aşağı gönder (bu bagaj için)
        yield return new WaitForSeconds(0.5f);
        if (PlatformMover.Instance != null)
        {
            PlatformMover.Instance.MoveToBottom();
        }

        // Platform tamamen aşağı inene kadar bekle
        while (PlatformMover.Instance != null && !PlatformMover.Instance.IsAtBottom)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($" [BaggageUnloadManager] Platform returned to bottom. Checking if next baggage should be sent...");

        // Bir sonraki bagajı conveyor'a gönder - SADECE player hala circle içindeyse
        if (isPlayerInBaggageXray)
        {
            Debug.Log($"[BaggageUnloadManager] Player still in circle - Sending next baggage");
            SendNextBaggageToConveyor();
        }
        else
        {
            Debug.Log($"[BaggageUnloadManager] Player exited circle - NOT sending next baggage. Remaining in stack: {baggageStack.Count}");
        }
    }

    private void JumpBaggageToTruck(GameObject baggage)
    {
        BaggageMover mover = baggage.GetComponent<BaggageMover>();
        if (mover != null)
        {
            // Truck'da stack pozisyon hesapla (0.35f aralıklarla üst üste)
            Vector3 targetPosition = truckLoadPoint.position + new Vector3(0, loadedBaggageCount * 0.35f, 0);

            mover.JumpToTruck(targetPosition, 1f, 0.8f);
            // NOT: loadedBaggageCount artırımı OnBaggageReachedTruck'ta yapılacak

            Debug.Log($" [BaggageUnloadManager] {baggage.name} jumping to truck position {loadedBaggageCount}");
        }
    }

    private void OnBaggageReachedTruck(GameObject baggage)
    {
        loadedBaggageCount++;
        Debug.Log($" [BaggageUnloadManager] {baggage.name} reached truck! Total loaded: {loadedBaggageCount}/{totalBaggageCount}");

        // Eğer tüm bagajlar yüklendiyse truck'ı hareket ettir
        CheckIfAllBaggagesLoaded();
    }

    private void CheckIfAllBaggagesLoaded()
    {
        // Toplam bagaj sayısı ile yüklenen bagaj sayısını karşılaştır
        if (loadedBaggageCount >= totalBaggageCount && totalBaggageCount > 0)
        {
            Debug.Log($"[BaggageUnloadManager] All {loadedBaggageCount}/{totalBaggageCount} baggages loaded! Moving truck...");
            EventBus.RaiseAllBaggagesLoadedToTruck();

            // Reset for next session
            totalBaggageCount = 0;
            loadedBaggageCount = 0;
            Debug.Log("[BaggageUnloadManager] Session complete - counters reset");
        }
    }

    private void Start()
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
