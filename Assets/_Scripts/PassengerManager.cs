using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;

    [SerializeField] private Vector3 startRotOffset;
    [SerializeField] private Transform queueStartPoint;
    [SerializeField] private Vector3 queueDirection;
    [SerializeField] private float queueSpacing = 2f;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform welcomingCircleCenter; // WelcomingCircle'ın merkez noktası (sinematik look-at için)
    [SerializeField] private bool hasStairs = true;
    [SerializeField] private Transform xrayQueueStart;
    [SerializeField] private Vector3 xrayQueueDirection;
    [SerializeField] private Transform[] toStairsPathPoints;
    [SerializeField] private Transform[] toXRayPathPoints;
    [SerializeField] private Transform[] upperQueuePoints;
    [SerializeField] private Transform[] stairSteps;
    [SerializeField] private List<PassengerController> passengers = new List<PassengerController>();
    private List<PassengerController> xrayQueue = new();
    public Vector3 lastStepPos;
    private CircleType currentCircleType;


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        EventBus.HasStairs = hasStairs;
    }

    private void Start()
    {
        SortTheQueue();
        lastStepPos = passengers[0].GetLastStepPosition();
    }

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.PassengerReachedFront += OnPassengerReachedFront;
        EventBus.PassengerHandedBaggage += OnPassengerHandedBaggage;
        EventBus.PassengerReachedTarget += OnPassengerReachedTarget;
        EventBus.PassengerStateChanged += OnPassengerStateChanged;
        EventBus.PassengerReachedStairs += OnPassengerReachedStairs;
        EventBus.PassengerReachedTopStairs += OnPassengerReachedTopStairs;
        EventBus.PassengerReachedXRayEnd += OnPassengerReachedXRayEnd;
        EventBus.PassengerReachedUpperQueue += OnPassengerReachedUpperQueue;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.PassengerReachedFront -= OnPassengerReachedFront;
        EventBus.PassengerHandedBaggage -= OnPassengerHandedBaggage;
        EventBus.PassengerReachedTarget -= OnPassengerReachedTarget;
        EventBus.PassengerStateChanged -= OnPassengerStateChanged;
        EventBus.PassengerReachedStairs -= OnPassengerReachedStairs;
        EventBus.PassengerReachedTopStairs -= OnPassengerReachedTopStairs;
        EventBus.PassengerReachedXRayEnd -= OnPassengerReachedXRayEnd;
        EventBus.PassengerReachedUpperQueue -= OnPassengerReachedUpperQueue;
    }



    private void OnPlayerEnteredCircle(CircleType circleType)
    {
        // Sadece WelcomingCircle için passenger sistemini çalıştır
        if (circleType != CircleType.WelcomingCircle)
            return;

        currentCircleType = circleType;
        Debug.Log($"[PassengerManager] currentCircleType SET to: {currentCircleType}");

        if (passengers.Count > 0)
        {
            // İlk passenger'ın state'ini kontrol et
            var firstPassenger = passengers[0];
            Log($"🟢 Player entered WelcomingCircle. Checking first passenger: {firstPassenger.name} (state: {firstPassenger.currentState})");

            // Eğer passenger Waiting state'deyse (henüz aktive edilmemişse), aktive et
            if (firstPassenger.currentState == PassengerState.Waiting)
            {
                Log($"✅ First passenger {firstPassenger.name} is Waiting. Activating...");

                // Player'ı sinematik olarak center'a yerleştir ve passenger sırasına baktır
                var playerController = FindObjectOfType<PlayerController>();
                if (playerController != null && welcomingCircleCenter != null && queueStartPoint != null)
                {
                    playerController.CinematicLookAt(
                        welcomingCircleCenter.position,
                        queueStartPoint.position,
                        duration: 0.8f // 0.8 saniye
                    );
                    Log($"🎬 Starting cinematic look-at to passenger queue");
                }

                // Passenger'ı aktive et (cinematic sırasında veya sonrasında)
                StartCoroutine(ActivatePassengerAfterCinematic(0.8f));
            }
            else
            {
                // Passenger zaten harekete geçmiş (bagaj veriyor veya yürüyor), sadece devam etsin
                Log($"⏭️ First passenger {firstPassenger.name} is already active (state: {firstPassenger.currentState}). Continuing without re-activation.");
            }
        }
    }

    private IEnumerator ActivatePassengerAfterCinematic(float delay)
    {
        yield return new WaitForSeconds(delay);
        ActivateNextPassenger();
    }
    private void ActivateNextPassenger()
    {
        if (passengers.Count > 0 && currentCircleType == CircleType.WelcomingCircle)
        {
            var next = passengers[0];

            // Sadece passenger Waiting state'deyse aktive et
            if (next.currentState == PassengerState.Waiting)
            {
                Log($"🎯 Next passenger: {next.name} - Activating (state: {next.currentState})");
                next.MoveToFront(GetQueuePosition(0));
            }
            else
            {
                Log($"🎯 Next passenger: {next.name} - Already active (state: {next.currentState}), skipping activation");
            }
        }
    }


    private void OnPlayerExitedCircle(CircleType circleType)
    {
        // Sadece WelcomingCircle için log
        if (circleType == CircleType.WelcomingCircle)
        {
            Log("🔴 Player exited WelcomingCircle. Current passengers will continue, but no new activations.");
            currentCircleType = CircleType.None; // Reset circle type to prevent NEW passenger activations
            Debug.Log($"[PassengerManager] currentCircleType SET to: {currentCircleType}");
            // Note: Passengers already moving/handing baggage will continue their actions
        }
    }

    private void OnPassengerStateChanged(PassengerController passenger)
    {
    }

    private void OnPassengerReachedFront(PassengerController passenger)
    {
        if (passenger.QueueIndex == 0)
        {
            Log($"  {passenger.name} reached front. Starting baggage handoff.");
            passenger.StartHandingBaggage();
        }
    }

    private void OnPassengerHandedBaggage(PassengerController passenger)
    {
        Log($"🧳 {passenger.name} handed baggage. Walking to stairs.");

        // Mevcut passenger'ı ToStairsPath'e yönlendir (bu devam etmeli - player circle'da olsun olmasın)
        passenger.StartWalkingPathGeneric(GetPathForType(PassengerPathType.ToStairs), PassengerPathType.ToStairs);

        // Diğerleri sırada bir adım öne gelsin (bu da devam etmeli)
        StartCoroutine(UpdateQueueAfterDelay(0.5f));
    }

    private IEnumerator UpdateQueueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateQueuePositions();

        // Sıra güncellendikten sonra sadece player circle içindeyse yeni passenger'ı aktive et
        if (passengers.Count > 0 && currentCircleType == CircleType.WelcomingCircle)
        {
            yield return new WaitForSeconds(0.1f);
            ActivateNextPassenger();
        }
    }

    

    private void OnPassengerReachedStairs(PassengerController passenger)
    {
        Log($"🧗 {passenger.name} reached stairs. Starting climb...");
        passenger.StartClimbingRoutine();

        //  Climbing başladığında bir sonraki passenger'ı tetikle
        if (passengers.Contains(passenger))
        {
            passengers.Remove(passenger);
            Log($"🔄 {passenger.name} removed from queue. Remaining: {passengers.Count}");

            //  Kuyruğu güncelle - diğerleri öne kayar ve index güncellenir
            UpdateQueuePositions();

            //  Kuyruk güncellemesi bittikten sonra bir sonraki passenger'ı tetikle (sadece player hala circle içindeyse)
            if (passengers.Count > 0 && currentCircleType == CircleType.WelcomingCircle)
            {
                StartCoroutine(ActivateNextAfterQueueUpdate());
            }
        }
    }

    private IEnumerator ActivateNextAfterQueueUpdate()
    {
        yield return new WaitForSeconds(0.6f); // UpdateQueuePositions DOMove süresi 0.5s

        if (passengers.Count > 0 && currentCircleType == CircleType.WelcomingCircle)
        {
            Log($"🎯 Activating next passenger: {passengers[0].name}");
            ActivateNextPassenger();
        }
    }

    private void OnPassengerReachedTopStairs(PassengerController passenger)
    {
        Log($"🧗 {passenger.name} finished climbing. Going to XRay path...");
        passenger.rb.isKinematic = true;

        // XRay path'ini al ve kontrol et
        List<Vector3> xrayPath = GetPathForType(PassengerPathType.ToXRay);

        if (xrayPath == null || xrayPath.Count == 0)
        {
            Debug.LogError($"{passenger.name} - ToXRay path is EMPTY! Check toXRayPathPoints in Inspector.");
            return;
        }

        Log($" {passenger.name} - XRay path has {xrayPath.Count} points. Starting walk...");
        passenger.StartWalkingPathGeneric(xrayPath, PassengerPathType.ToXRay);
    }


    private void OnPassengerReachedXRayEnd(PassengerController passenger)
    {
            Log($"{passenger.name} finished XRay path. Moving to upper queue position...");

        // PermanentOrder indexine göre xrayQueueStart + direction + spacing kullanarak sıralama
        int permanentIndex = passenger.PermanentOrder;
        Vector3 targetPos = GetXRayQueuePosition(permanentIndex);

        //  DOPath için minimum 2 nokta gerekli (mevcut + hedef)
        List<Vector3> pathToQueue = new List<Vector3>
        {
            passenger.transform.position,  // Başlangıç
            targetPos                       // Hedef
        };

        Log($"🚶 {passenger.name} walking to upper queue | Distance: {Vector3.Distance(passenger.transform.position, targetPos)}");

        passenger.StartWalkingPathGeneric(
            pathToQueue,
            PassengerPathType.ToUpperQueue
        );
    }


    private void OnPassengerReachedTarget(PassengerController passenger)
    {
        Log($"🚶 {passenger.name} left queue. Removing...");

        passengers.Remove(passenger);
        UpdateQueuePositions();

        if (passengers.Count > 0 && currentCircleType == CircleType.WelcomingCircle)
        {
            var nextPassenger = passengers[0];

            // Sadece passenger Waiting state'deyse aktive et
            if (nextPassenger.currentState == PassengerState.Waiting)
            {
                Log($"🎯 Next passenger: {nextPassenger.name} - Activating (state: {nextPassenger.currentState})");
                nextPassenger.MoveToFront(GetQueuePosition(0));
            }
            else
            {
                Log($"🎯 Next passenger: {nextPassenger.name} - Already active (state: {nextPassenger.currentState}), skipping");
            }
        }
        else
        {
            Log("🚫 No passengers left or player not in range.");
        }
    }

    private void OnPassengerReachedUpperQueue(PassengerController passenger)
    {
        Log($" {passenger.name} reached upper queue. Waiting...");

        passenger.rb.isKinematic = true;
        passenger.currentState = PassengerState.Waiting;
        EventBus.RaisePassengerStateChanged(passenger);

        //  Sonraki passenger zaten OnPassengerReachedStairs'da tetiklendi
        // Burada tekrar tetiklemeye gerek yok
    }



    private void UpdateQueuePositions()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];

            //  Sadece Waiting state'deki yolcuları güncelle (yürüyenlerle çakışma olmasın)
            if (p.currentState != PassengerState.Waiting)
            {
                Log($"⏭️ Skipping {p.name} - State: {p.currentState}");
                continue;
            }

            p.QueueIndex = i;

            // Eğer hareket mesafesi çok kısa ise animasyon çalıştırma
            float distance = Vector3.Distance(p.transform.position, GetQueuePosition(i));
            if (distance < 0.1f)
            {
                // Mesafe çok kısa, animasyon gerekmez
                continue;
            }

            // Closure problemi için local variable
            var passenger = p;

            // Animasyonu hemen başlat (DOMove zaten çalışmaya başlıyor)
            if (passenger.animator != null)
                passenger.animator.SetBool("IsMoving", true);

            passenger.transform
                .DOMove(GetQueuePosition(i), 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Hareket bittiğinde animasyonu durdur
                    if (passenger.animator != null)
                        passenger.animator.SetBool("IsMoving", false);
                });
        }
    }
    private void AddToXRayQueue(PassengerController passenger)
    {
        if (!xrayQueue.Contains(passenger))
            xrayQueue.Add(passenger);

        xrayQueue = xrayQueue.OrderBy(p => p.PermanentOrder).ToList();

        for (int i = 0; i < xrayQueue.Count; i++)
        {
            var p = xrayQueue[i];
            Vector3 targetPos = GetXRayQueuePosition(i);
            p.QueueIndex = i;
            p.currentState = PassengerState.WalkingToTarget;
            p.rb.isKinematic = true;
            EventBus.RaisePassengerStateChanged(p);
            p.transform
                .DOMove(targetPos, 0.6f)
                .SetDelay(i * 0.2f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    p.currentState = PassengerState.Waiting;
                    EventBus.RaisePassengerStateChanged(p);

                    if (p == passenger)
                        OnPassengerFinishedFullCycle(p);
                });
        }
    }

    private void OnPassengerFinishedFullCycle(PassengerController passenger)
    {
        // Yolcuyu ana sıradan çıkar
        passengers.Remove(passenger);
        UpdateQueuePositions();

        // 🔹 Sonraki yolcuyu başlat
        ActivateNextPassenger();
    }


    private Vector3 GetXRayQueuePosition(int index)
    {
        return xrayQueueStart.position+ (xrayQueueDirection.normalized * queueSpacing * index);
    }

    private Vector3 GetQueuePosition(int index)
    {
        return queueStartPoint.position + (queueDirection.normalized * queueSpacing * index);
    }

    private List<Vector3> GetPathForPassenger(PassengerController passenger)
    {
        List<Vector3> path = new();
        if (toStairsPathPoints != null)
        {
            foreach (var point in toStairsPathPoints)
                path.Add(point.position);
            path.Add(exitPoint.position);

            if (!hasStairs)
                path.Add(exitPoint.position); // merdiven yoksa direkt çıkış
        }

        else if (toXRayPathPoints != null)
        {
            foreach (var point in toXRayPathPoints)
                path.Add(point.position);
            path.Add(exitPoint.position);
        }
            return path;
    }


    private void MovePassengerAlongPath(PassengerController passenger, PassengerPathType pathType)
    {
        List<Vector3> path = GetPathForType(pathType);
        passenger.StartWalkingPathGeneric(path, pathType);
    }

    private List<Vector3> GetPathForType(PassengerPathType pathType)
    {
        List<Vector3> path = new();

        switch (pathType)
        {
            case PassengerPathType.ToStairs:
                if (toStairsPathPoints != null)
                    foreach (var p in toStairsPathPoints)
                        path.Add(p.position);
                break;

            case PassengerPathType.ToXRay:
                if (toXRayPathPoints != null)
                    foreach (var p in toXRayPathPoints)
                        path.Add(p.position);
                break;

            case PassengerPathType.ToUpperQueue:
                if (upperQueuePoints != null)
                    foreach (var p in upperQueuePoints)
                        path.Add(p.position);
                break;

            case PassengerPathType.ToExit:
                if (exitPoint != null)
                    path.Add(exitPoint.position);
                break;
        }

        return path;
    }



    private void SortTheQueue()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            p.QueueIndex = i;
            p.SetPermanentOrder(i); // 🔹 yeni fonksiyon
            p.transform.position = GetQueuePosition(i);
            p.transform.rotation = Quaternion.Euler(startRotOffset);
        }
    }



    private void Log(string msg)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
                Debug.Log($"[PassengerManager] {msg}");
    }
}
