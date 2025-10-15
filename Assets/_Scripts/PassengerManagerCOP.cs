using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PassengerManagerCOP : MonoBehaviour
{
    #region OLD
    //public static PassengerManager Instance { get; private set; }

    //[Header("References")]
    //public Transform[] waitingSlots;
    //public Transform playerTransform;
    //public Transform stairsEntrancePoint;

    //[Header("Passengers")]
    //public List<PassengerController> passengers = new();

    //private bool isProcessing = false;

    //private void Awake() => Instance = this;

    //private void Start()
    //{
    //    for (int i = 0; i < passengers.Count; i++)
    //    {
    //        int slotIndex = Mathf.Min(i, waitingSlots.Length - 1);
    //        Transform slot = waitingSlots[slotIndex];

    //        passengers[i].transform.position = slot.position;
    //        passengers[i].transform.rotation = slot.rotation;
    //        passengers[i].queueIndex = i;
    //    }
    //}

    //private void OnEnable()
    //{
    //    EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
    //    EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
    //    EventBus.PassengerHandedBaggage += OnPassengerHandedBaggage;
    //}

    //private void OnDisable()
    //{
    //    EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
    //    EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
    //    EventBus.PassengerHandedBaggage -= OnPassengerHandedBaggage;
    //}

    //private void OnPlayerEnteredCircle()
    //{
    //    if (!isProcessing)
    //        StartCoroutine(ProcessQueue());
    //}

    //private void OnPlayerExitedCircle()
    //{
    //    // Player çýkýnca süreç durdurulur
    //    isProcessing = false;
    //    StopAllCoroutines();
    //}

    //private IEnumerator ProcessQueue()
    //{
    //    isProcessing = true;

    //    while (EventBus.IsPlayerInCircle && passengers.Count > 0)
    //    {
    //        var front = passengers[0];

    //        // Eðer Passenger zaten bavul veriyorsa bekle
    //        if (front.State != PassengerState.Waiting)
    //        {
    //            yield return null;
    //            continue;
    //        }

    //        if (front.State == PassengerState.IdleAtSlot && front.HasBaggage)
    //        {
    //            front.StartHandingBaggage();
    //        }

    //        bool handed = false;
    //        void OnHanded(PassengerController p)
    //        {
    //            if (p == front) handed = true;
    //        }

    //        EventBus.PassengerHandedBaggage += OnHanded;

    //        yield return new WaitUntil(() => handed || !EventBus.IsPlayerInCircle);

    //        EventBus.PassengerHandedBaggage -= OnHanded;

    //        if (!EventBus.IsPlayerInCircle) break;

    //        // Sýrayý kaydýr ve yeni Passenger’ý front yap
    //        passenger_MoveQueueForward(front);

    //        yield return new WaitForSeconds(0.3f);
    //    }

    //    isProcessing = false;
    //}


    //private void passenger_MoveQueueForward(PassengerController finished)
    //{
    //    Debug.Log($"[Queue] {finished.name} bavul verdi, merdivene gönderiliyor. Kalan: {passengers.Count}");

    //    int index = passengers.IndexOf(finished);
    //    if (index == -1)
    //    {
    //        Debug.LogWarning($"[Queue] {finished.name} listede bulunamadý!");
    //        return;
    //    }

    //    finished.MoveToStairs(stairsEntrancePoint);
    //    passengers.RemoveAt(index);
    //    Debug.Log($"[Queue] {finished.name} çýkarýldý. Yeni ön müþteri: {(passengers.Count > 0 ? passengers[0].name : "Yok")}");

    //    for (int i = 0; i < passengers.Count && i < waitingSlots.Length; i++)
    //    {
    //        passengers[i].MoveToSlot(waitingSlots[i]);
    //        passengers[i].queueIndex = i;
    //    }
    //}


    //private void OnPassengerHandedBaggage(PassengerController passenger)
    //{
    //    // Artýk sadece log veya diðer sistemler için kullanýlabilir
    //    Debug.Log($"{passenger.name} bavulunu verdi (event).");
    //}
    #endregion

    /*public static PassengerManagerCOP Instance { get; private set; }

    [Header("Queue Settings")]
    [SerializeField] private Transform queueStartPoint;
    [SerializeField] private float queueSpacing = 2f;
    [SerializeField] private Vector3 queueDirection = Vector3.forward;

    [Header("Target Settings")]
    [SerializeField] private Transform targetPoint;


    private List<PassengerControllerCOP> queuedPassengers = new List<PassengerControllerCOP>();

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

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PassengerReachedTarget += OnPassengerReachedTarget;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PassengerReachedTarget -= OnPassengerReachedTarget;
    }

    public void AddPassengerToQueue(PassengerControllerCOP passenger)
    {
        queuedPassengers.Add(passenger);
        int index = queuedPassengers.Count - 1;

        passenger.QueueIndex = index;
        passenger.SetTargetPosition(targetPoint.position);
        passenger.transform.position = GetQueuePosition(index);
        passenger.ChangeState(PassengerState.Waiting);
    }

    public void RemovePassengerFromQueue(PassengerControllerCOP passenger)
    {
        int removedIndex = passenger.QueueIndex;
        queuedPassengers.Remove(passenger);

        // Kalan passenger'larýn index'lerini güncelle
        UpdateQueueIndices(removedIndex);
    }

    public Vector3 GetQueuePosition(int index)
    {
        if (queueStartPoint == null)
        {
            Debug.LogError("Queue start point is not set!");
            return Vector3.zero;
        }

        return queueStartPoint.position + (queueDirection.normalized * queueSpacing * index);
    }

    public PassengerControllerCOP GetPassengerAtIndex(int index)
    {
        return queuedPassengers.FirstOrDefault(p => p.QueueIndex == index);
    }

    private void OnPlayerEnteredCircle()
    {
        // Player circle'a girdiðinde sýradaki passenger'larý kontrol et
        ProcessQueue();
    }

    private void ProcessQueue()
    {
        if (queuedPassengers.Count == 0)
            return;

        foreach (var passenger in queuedPassengers)
        {
            if (passenger.CurrentState == PassengerState.Waiting)
            {
                if (passenger.QueueIndex == 0)
                {
                    // Ýlk sýradaki passenger HandingBaggage'e geçer
                    passenger.ChangeState(PassengerState.HandingBaggage);
                }
                else
                {
                    // Diðerleri WalkingToSlot'a geçer
                    passenger.ChangeState(PassengerState.WalkingToSlot);
                }
            }
        }
    }

    private void OnPassengerReachedTarget(PassengerControllerCOP passenger)
    {
        RemovePassengerFromQueue(passenger);
    }

    private void UpdateQueueIndices(int fromIndex)
    {
        for (int i = fromIndex; i < queuedPassengers.Count; i++)
        {
            queuedPassengers[i].QueueIndex = i;
        }
    }

    private void OnDrawGizmos()
    {
        if (queueStartPoint == null)
            return;

        Gizmos.color = Color.yellow;

        // Ýlk 10 queue pozisyonunu göster
        for (int i = 0; i < 10; i++)
        {
            Vector3 pos = GetQueuePosition(i);
            Gizmos.DrawWireSphere(pos, 0.3f);

            if (i > 0)
            {
                Vector3 prevPos = GetQueuePosition(i - 1);
                Gizmos.DrawLine(prevPos, pos);
            }
        }

        if (targetPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPoint.position, 0.5f);
        }
    }
    */
}
