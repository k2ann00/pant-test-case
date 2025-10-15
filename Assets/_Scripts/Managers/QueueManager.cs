using System.Collections.Generic;
using UnityEngine;

public class QueueManager : MonoBehaviour
{
    //public static QueueManager Instance { get; private set; }

    //[Header("Queue Settings")]
    //public List<PassengerController> passengers;
    //public Transform StairsTarget;

    //public PassengerController CurrentFrontPassenger { get; private set; }

    //private void Awake()
    //{
    //    if (Instance != null) Debug.LogWarning("QueueManager: Multiple instances!");
    //    Instance = this;

    //    if (passengers.Count > 0)
    //        CurrentFrontPassenger = passengers[0];
    //}

    //public void AdvanceQueue()
    //{
    //    int nextIndex = CurrentFrontPassenger.queueIndex + 1;
    //    if (nextIndex < passengers.Count)
    //    {
    //        CurrentFrontPassenger = passengers[nextIndex];

    //        // Yeni front Passenger sýraya yürüsün
    //        Transform nextSlot = passengers[nextIndex].transform; // slot transform'ý sahneden al
    //        CurrentFrontPassenger.MoveToSlot(nextSlot);
    //    }
    //    else
    //    {
    //        CurrentFrontPassenger = null;
    //        Debug.Log("Queue tamamlandý.");
    //    }
    //}

    //public PassengerController GetPassengerByIndex(int index)
    //{
    //    if (index >= 0 && index < passengers.Count)
    //        return passengers[index];
    //    return null;
    //}
}
