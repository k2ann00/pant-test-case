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
    [SerializeField] private bool hasStairs = true;
    [SerializeField] private Transform xrayQueueStart;
    [SerializeField] private Vector3 xrayQueueDirection;
    [SerializeField] private Transform[] toStairsPathPoints;
    [SerializeField] private Transform[] toXRayPathPoints;
    [SerializeField] private Transform[] upperQueuePoints;
    [SerializeField] private Transform[] stairSteps;
    [SerializeField] private List<PassengerController> passengers = new List<PassengerController>();
    private List<PassengerController> xrayQueue = new();


    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        EventBus.HasStairs = hasStairs;
    }

    private void Start()
    {
        SortTheQueue();
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



    private void OnPlayerEnteredCircle()
    {
        if (passengers.Count > 0)
        {
            Log($"🟢 Player entered area. Activating first passenger: {passengers[0].name}");
            ActivateNextPassenger();

            //passengers[0].MoveToFront(GetQueuePosition(0));
        }
    }
    private void ActivateNextPassenger()
    {
        if (passengers.Count > 0 && EventBus.IsPlayerInCircle)
        {
            var next = passengers[0];
            Debug.Log($"🎯 Next passenger: {next.name}");
            next.MoveToFront(GetQueuePosition(0));
        }
    }


    private void OnPlayerExitedCircle()
    {
        Log("🔴 Player exited area.");
    }

    private void OnPassengerStateChanged(PassengerController passenger)
    {
        passenger.UpdateStateText();
    }

    private void OnPassengerReachedFront(PassengerController passenger)
    {
        if (passenger.QueueIndex == 0)
        {
            Log($"📦 {passenger.name} reached front. Starting baggage handoff.");
            passenger.StartHandingBaggage();
        }
    }

    private void OnPassengerHandedBaggage(PassengerController passenger)
    {
        Log($"🧳 {passenger.name} handed baggage. Walking to stairs.");

        // Mevcut passenger’ı ToStairsPath’e yönlendir
        passenger.StartWalkingPathGeneric(GetPathForType(PassengerPathType.ToStairs), PassengerPathType.ToStairs);

        // Diğerleri sırada bir adım öne gelsin
        StartCoroutine(UpdateQueueAfterDelay(0.5f));
    }

    private IEnumerator UpdateQueueAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateQueuePositions();
    }



    private void OnPassengerReachedStairs(PassengerController passenger)
    {
        Debug.Log($"🧗 {passenger.name} reached stairs. Starting climb...");
        passenger.StartClimbingRoutine();

        // ✅ Climbing başladığında bir sonraki passenger'ı tetikle
        if (passengers.Contains(passenger))
        {
            passengers.Remove(passenger);
            Debug.Log($"🔄 {passenger.name} removed from queue. Remaining: {passengers.Count}");

            // ✅ Kuyruğu güncelle - diğerleri öne kayar ve index güncellenir
            UpdateQueuePositions();

            // ✅ Kuyruk güncellemesi bittikten sonra bir sonraki passenger'ı tetikle
            if (passengers.Count > 0 && EventBus.IsPlayerInCircle)
            {
                StartCoroutine(ActivateNextAfterQueueUpdate());
            }
        }
    }

    private IEnumerator ActivateNextAfterQueueUpdate()
    {
        yield return new WaitForSeconds(0.6f); // UpdateQueuePositions DOMove süresi 0.5s

        if (passengers.Count > 0 && EventBus.IsPlayerInCircle)
        {
            Debug.Log($"🎯 Activating next passenger: {passengers[0].name}");
            ActivateNextPassenger();
        }
    }

    private void OnPassengerReachedTopStairs(PassengerController passenger)
    {
        Debug.Log($"🧗 {passenger.name} finished climbing. Going to XRay path...");
        passenger.rb.isKinematic = true;

        // XRay path'ini al ve kontrol et
        List<Vector3> xrayPath = GetPathForType(PassengerPathType.ToXRay);

        if (xrayPath == null || xrayPath.Count == 0)
        {
            Debug.LogError($"❌ {passenger.name} - ToXRay path is EMPTY! Check toXRayPathPoints in Inspector.");
            return;
        }

        Debug.Log($"✅ {passenger.name} - XRay path has {xrayPath.Count} points. Starting walk...");
        passenger.StartWalkingPathGeneric(xrayPath, PassengerPathType.ToXRay);
    }


    private void OnPassengerReachedXRayEnd(PassengerController passenger)
    {
        Debug.Log($"{passenger.name} finished XRay path. Moving to upper queue position...");

        // PermanentOrder indexine göre xrayQueueStart + direction + spacing kullanarak sıralama
        int permanentIndex = passenger.PermanentOrder;
        Vector3 targetPos = GetXRayQueuePosition(permanentIndex);

        // ✅ DOPath için minimum 2 nokta gerekli (mevcut + hedef)
        List<Vector3> pathToQueue = new List<Vector3>
        {
            passenger.transform.position,  // Başlangıç
            targetPos                       // Hedef
        };

        Debug.Log($"🚶 {passenger.name} walking to upper queue | Distance: {Vector3.Distance(passenger.transform.position, targetPos)}");

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

        if (passengers.Count > 0 && EventBus.IsPlayerInCircle)
        {
            Log($"🎯 Next passenger: {passengers[0].name}");
            passengers[0].MoveToFront(GetQueuePosition(0));
        }
        else
        {
            Log("🚫 No passengers left or player not in range.");
        }
    }

    private void OnPassengerReachedUpperQueue(PassengerController passenger)
    {
        Debug.Log($"✅ {passenger.name} reached upper queue. Waiting...");

        passenger.rb.isKinematic = true;
        passenger.currentState = PassengerState.Waiting;
        EventBus.RaisePassengerStateChanged(passenger);

        // ✅ Sonraki passenger zaten OnPassengerReachedStairs'da tetiklendi
        // Burada tekrar tetiklemeye gerek yok
    }



    private void UpdateQueuePositions()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];

            // ✅ Sadece Waiting state'deki yolcuları güncelle (yürüyenlerle çakışma olmasın)
            if (p.currentState != PassengerState.Waiting)
            {
                Debug.Log($"⏭️ Skipping {p.name} - State: {p.currentState}");
                continue;
            }

            p.QueueIndex = i;
            p.transform
                .DOMove(GetQueuePosition(i), 0.5f)
                .SetEase(Ease.OutQuad);
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
