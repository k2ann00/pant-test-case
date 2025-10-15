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
    [SerializeField] private List<PassengerController> passengers = new List<PassengerController>();
    [SerializeField] private Transform[] pathPoints; // masanın etrafındaki noktalar

    public bool IsPlayerInRange;
    private bool isProcessingFrontPassenger = false;
    #region Singelton
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion

    private void Start()
    {
        SortTheQueue();
        IsPlayerInRange = false;

    }
    
    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredeCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.PassengerReachedTarget += OnPassengerReachedTarget;
        EventBus.PassengerHandedBaggage += OnPassengerHandedBaggage;

    }


    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredeCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.PassengerReachedTarget -= OnPassengerReachedTarget;
        EventBus.PassengerHandedBaggage -= OnPassengerHandedBaggage;
    }

    private void OnPlayerExitedCircle() => IsPlayerInRange = false;
    private void OnPlayerEnteredeCircle() => IsPlayerInRange = true;

    // 🔹 Passenger bavulu verince çağrılır
    private void OnPassengerHandedBaggage(PassengerController passenger)
    {
        Log($"🧳 {passenger.name} baggage handed. QueueIndex={passenger.QueueIndex}");
        isProcessingFrontPassenger = false;
    }


    // 🔹 Passenger yürüyüp sahneyi terk edince çağrılır
    private void OnPassengerReachedTarget(PassengerController passenger)
    {
        Log($"🚶 {passenger.name} reached target! Removing from queue...");

        passengers.Remove(passenger);
        Log($"Remaining passengers: {string.Join(", ", passengers.Select(p => p.name))}");

        for (int i = 0; i < passengers.Count; i++)
        {
            var p = passengers[i];
            p.QueueIndex = i;

            // Sadece bekleyenleri kaydır
            if (p.CurrentState == PassengerState.Waiting)
            {
                Vector3 newPos = GetQueuePosition(i);
                DOTween.Kill(p.transform);
                p.transform.DOMove(newPos, 0.5f).SetEase(Ease.OutQuad);

                Log($"↔ {p.name} moved to queue pos {i} ({newPos})");
            }
            else
            {
                Log($"⏭ {p.name} skipped (state={p.CurrentState})");
            }
        }

        StartCoroutine(ActivateNextPassengerDelayed());
    }


    private IEnumerator ActivateNextPassengerDelayed()
    {
        yield return new WaitForSeconds(0.6f);

        if (passengers.Count == 0)
        {
            Log("🚫 No passengers left in queue.");
            yield break;
        }

        var front = passengers[0];
        Vector3 frontPos = GetQueuePosition(0);

        Log($"🎯 Next passenger {front.name} will move to front and start sequence.");
        front.MoveToFrontAndStartSequence(frontPos);
    }



    private void SortTheQueue()
    {
        foreach (PassengerController passenger in passengers)
        {
            passenger.QueueIndex = passengers.IndexOf(passenger);
            passenger.transform.position = queueStartPoint.position + (queueDirection * passenger.QueueIndex * queueSpacing);
            passenger.transform.rotation = Quaternion.Euler(startRotOffset);
            passenger.stateText.text = passengers.IndexOf(passenger).ToString();
        }
    }
    public List<Vector3> GetPathForPassenger(PassengerController passenger)
    {
        List<Vector3> path = new();

        // Örnek: masanın etrafında dolanacaksa
        foreach (var p in pathPoints)
            path.Add(p.position);

        // En sonda “çıkış hedefi” varsa ekle
        path.Add(exitPoint.position);

        return path;
    }

    public bool CanProcessFrontPassenger()
    {
        return !isProcessingFrontPassenger;
    }

    public void SetProcessingFrontPassenger(bool value)
    {
        isProcessingFrontPassenger = value;
    }

    public Vector3 GetQueuePosition(int index)
    {
        return queueStartPoint.position + (queueDirection.normalized * queueSpacing * index);
    }

    public PassengerController GetPassengerAtIndex(int index)
    {
        return passengers.FirstOrDefault(p => p.QueueIndex == index);
    }

    #region GizmosForBetterSceneView
    private void OnDrawGizmos()
    {
        if (queueDirection != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(queueStartPoint.position, queueStartPoint.position + queueDirection);
        }

        if (queueStartPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(queueStartPoint.position, 1f);
        }

        if (pathPoints != null && pathPoints.Length > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < pathPoints.Length - 1; i++)
                Gizmos.DrawLine(pathPoints[i].position, pathPoints[i + 1].position);
        }

    }

    private void Log(string message)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[{name}] {message}");
    }
    #endregion
}
