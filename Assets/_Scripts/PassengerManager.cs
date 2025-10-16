using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;

public class PassengerManager : MonoBehaviour
{
    public static PassengerManager Instance;

    [SerializeField] private Vector3 startRotOffset;
    [SerializeField] private Transform queueStartPoint;
    [SerializeField] private Vector3 queueDirection;
    [SerializeField] private float queueSpacing = 2f;
    [SerializeField] private Transform exitPoint;
    [SerializeField] private Transform[] pathPoints;
    [SerializeField] private List<PassengerController> passengers = new List<PassengerController>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.PassengerReachedFront -= OnPassengerReachedFront;
        EventBus.PassengerHandedBaggage -= OnPassengerHandedBaggage;
        EventBus.PassengerReachedTarget -= OnPassengerReachedTarget;
    }

    private void OnPlayerEnteredCircle()
    {
        if (passengers.Count > 0)
        {
            Log($"🟢 Player entered area. Activating first passenger: {passengers[0].name}");
            passengers[0].MoveToFront(GetQueuePosition(0));
        }
    }

    private void OnPlayerExitedCircle()
    {
        Log("🔴 Player exited area.");
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
        Log($"🧳 {passenger.name} handed baggage. Walking to exit.");
        var path = GetPathForPassenger(passenger);
        passenger.StartWalkingPath(path);
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

    private void UpdateQueuePositions()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            p.QueueIndex = i;
            p.transform
                .DOMove(GetQueuePosition(i), 0.5f)
                .SetEase(Ease.OutQuad);
        }
    }

    private Vector3 GetQueuePosition(int index)
    {
        return queueStartPoint.position + (queueDirection.normalized * queueSpacing * index);
    }

    private List<Vector3> GetPathForPassenger(PassengerController passenger)
    {
        List<Vector3> path = new();
        foreach (var point in pathPoints)
            path.Add(point.position);
        path.Add(exitPoint.position);
        return path;
    }

    private void SortTheQueue()
    {
        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            p.QueueIndex = i;
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
