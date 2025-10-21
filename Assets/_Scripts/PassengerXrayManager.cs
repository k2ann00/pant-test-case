using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


public class PassengerXrayManager : MonoBehaviour
{
    public static PassengerXrayManager Instance { get; private set; }

    [Header("XRay Inspection Settings")]
    [SerializeField] private Transform inspectionPoint; // Inspection pozisyonu (Inspector'dan ayarlanacak)
    [SerializeField] private Transform exitPoint; // Inspection sonrası gidecek nokta (Inspector'dan ayarlanacak)
    [SerializeField] private int moneyPerPassenger = 2; // Her passenger için kaç money spawn olacak

    [Header("UI Settings")]
    [SerializeField] private TextMeshPro passengerCountText; // "Passengers: X" text componenti

    [Header("References")]
    [SerializeField] private MoneyStackManager moneyStackManager;
    [SerializeField] private Queue<PassengerController> waitingQueue = new Queue<PassengerController>();

    private PassengerController currentInspecting;
    private bool isInspecting = false;
    private int totalPassengersProcessed = 0;
    private bool isPlayerInCircle = false;
    private Coroutine inspectionRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.PassengerReachedUpperQueue += OnPassengerReachedUpperQueue;
        EventBus.PassengerCompletedXRayInspection += OnPassengerCompletedInspection;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.PassengerReachedUpperQueue -= OnPassengerReachedUpperQueue;
        EventBus.PassengerCompletedXRayInspection -= OnPassengerCompletedInspection;
    }

    private void OnPlayerEnteredCircle(CircleType circleType)
    {
        if (circleType != CircleType.PassengerXray)
            return;

        Log($"@@@ KUYRUK SAYISII: {waitingQueue.Count}");
        Log("Player entered PassengerXray circle. Starting inspection process...");
        isPlayerInCircle = true;
        Debug.Log($"[PassengerXrayManager] isPlayerInCircle SET to: {isPlayerInCircle}");
        ProcessNextPassenger();
    }

    private void OnPlayerExitedCircle(CircleType circleType)
    {
        if (circleType != CircleType.PassengerXray)
            return;
        Log($"@@@ KUYRUK SAYISI: {waitingQueue.Count}");
        Log("🔴 Player exited PassengerXray circle. Active passenger will finish, but no new passengers will start...");
        isPlayerInCircle = false;
        Debug.Log($"[PassengerXrayManager] isPlayerInCircle SET to: {isPlayerInCircle}");

        // DON'T stop the current inspection coroutine - let it finish
        // DON'T reset isInspecting - let current passenger complete
        // Only new passengers won't start (handled by isPlayerInCircle checks in ProcessNextPassenger and InspectionRoutine)
        Log("[PassengerXrayManager] Current inspection will complete if running");
    }

    private void OnPassengerReachedUpperQueue(PassengerController passenger)
    {
        // Passenger XRay kuyruğuna eklendi
        if (!waitingQueue.Contains(passenger))
        {
            waitingQueue.Enqueue(passenger);
            Log($" {passenger.name} added to XRay queue | Queue size: {waitingQueue.Count}");

            // Eğer player circle içindeyse ve inspection yapılmıyorsa, hemen başlat
            if (isPlayerInCircle && !isInspecting)
            {
                ProcessNextPassenger();
            }
        }
    }

    private void ProcessNextPassenger()
    {
        // Player circle'da değilse işlem yapma
        if (!isPlayerInCircle)
        {
            Log("⏸️ Player not in PassengerXray circle. Waiting...");
            return;
        }

        if (isInspecting)
        {
            Log("⏸️ Already inspecting a passenger. Waiting...");
            return;
        }

        if (waitingQueue.Count == 0)
        {
            Log("⏸️ No passengers in XRay queue. Waiting...");
            return;
        }

        // Sıraya göre bir sonraki passenger'ı al
        currentInspecting = waitingQueue.Dequeue();
        Log($"🔍 Starting inspection for {currentInspecting.name} | Remaining: {waitingQueue.Count}");

        inspectionRoutine = StartCoroutine(InspectionRoutine(currentInspecting));
    }

    private IEnumerator InspectionRoutine(PassengerController passenger)
    {
        isInspecting = true;

        // 1. Passenger'ı inspection point'e taşı
        passenger.currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(passenger);

        List<Vector3> pathToInspection = new List<Vector3>
        {
            passenger.transform.position,
            inspectionPoint.position
        };

        passenger.StartWalkingPathGeneric(pathToInspection, PassengerPathType.ToInspectionPoint);

        // Passenger inspection point'e ulaşana kadar bekle (player çıksa bile devam etmeli)
        float timeout = 5f;
        float elapsed = 0f;

        while (Vector3.Distance(passenger.transform.position, inspectionPoint.position) > 0.5f && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (elapsed >= timeout)
        {
            Log($"⚠️ {passenger.name} timed out reaching inspection point");
            isInspecting = false;
            inspectionRoutine = null;
            yield break;
        }

        Log($"🎯 {passenger.name} reached inspection point");

        // 2. Inspection başladı - event tetikle
        EventBus.RaisePassengerStartedXRayInspection(passenger);

        // 3. Money spawn et (inspection sırasında) - XRay'den geldiği için 3x scale
        // ÖNEMLI: isPlayerInCircle kontrolünü loop'tan ÇIKAR - para vermesi garanti olsun
        Log($"💰 ğğStarting money spawn for {passenger.name} - Total: {moneyPerPassenger} moneys");
        for (int i = 0; i < moneyPerPassenger; i++)
        {
            if (moneyStackManager != null)
            {
                Vector3 spawnPos = passenger.transform.position + Vector3.up * 1.5f;
                moneyStackManager.SpawnAndStackMoney(spawnPos, isFromXray: true); // 3x scale için
                Log($"💰 ğğSpawned money {i + 1}/{moneyPerPassenger} for {passenger.name}");
            }

            yield return new WaitForSeconds(0.1f); // Money'ler arasında çok kısa delay
        }
        Log($"💰 ğğFinished spawning {moneyPerPassenger} moneys for {passenger.name}");

        // 4. Inspection süresi yok - direkt devam et
        // yield return new WaitForSeconds(inspectionDuration); // KALDIRILDI

        // 5. UI Text'i güncelle (toplam işlenen passenger sayısı)
        totalPassengersProcessed++;
        UpdatePassengerCountUI();

        Log($" {passenger.name} completed inspection | Total processed: {totalPassengersProcessed}");

        // 6. Inspection tamamlandı - event tetikle
        EventBus.RaisePassengerCompletedXRayInspection(passenger);

        inspectionRoutine = null;
    }

    private void OnPassengerCompletedInspection(PassengerController passenger)
    {
        Log($"🚪 {passenger.name} leaving inspection area");

        // Exit point'e gönder, sonra pool/destroy et
        if (exitPoint != null)
        {
            StartCoroutine(SendPassengerToExit(passenger));
        }
        else
        {
            // Exit point yoksa direkt pool/destroy
            ReturnOrDestroyPassenger(passenger);
        }

        // Inspection bitti, bir sonraki passenger'a geç
        isInspecting = false;
        StartCoroutine(ProcessNextAfterDelay(0.1f)); // Daha hızlı geçiş
    }

    private IEnumerator SendPassengerToExit(PassengerController passenger)
    {
        // Exit point'e gitmek için path oluştur
        List<Vector3> pathToExit = new List<Vector3>
        {
            passenger.transform.position,
            exitPoint.position
        };

        passenger.currentState = PassengerState.WalkingToTarget;
        passenger.StartWalkingPathGeneric(pathToExit, PassengerPathType.ToExit);

        // Exit point'e ulaşana kadar bekle
        float timeout = 5f;
        float elapsed = 0f;

        while (Vector3.Distance(passenger.transform.position, exitPoint.position) > 0.5f && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        Log($" {passenger.name} reached exit point");

        // Pool'a gönder veya destroy et
        ReturnOrDestroyPassenger(passenger);
    }

    private void ReturnOrDestroyPassenger(PassengerController passenger)
    {
        if (PassengerPool.Instance != null)
        {
            PassengerPool.Instance.ReturnPassenger(passenger.gameObject);
            Log($"♻️ {passenger.name} returned to pool");
        }
        else
        {
            Destroy(passenger.gameObject);
            Log($"🗑️ {passenger.name} destroyed");
        }
    }

    private IEnumerator ProcessNextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ProcessNextPassenger();
    }

    private void UpdatePassengerCountUI()
    {
        if (passengerCountText != null)
        {
            passengerCountText.text = $"{totalPassengersProcessed} / 6";
            Log($"📊 UI Updated - Passengers: {totalPassengersProcessed}");
        }
    }

    private void Log(string msg)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[PassengerXrayManager] {msg}");
    }
}
