using System;
using UnityEngine;

/// <summary>
/// Circle türlerini tanımlayan enum
/// </summary>
public enum CircleType
{
    None,               // No circle active
    WelcomingCircle,    // Passenger bagaj teslimi
    BaggageUnload,      // Bagaj boşaltma - X-Ray'e
    BaggageXray,        // X-Ray işlemi
    PassengerXray       // Passenger X-Ray kontrol
}


public static class EventBus
{
    public static bool HasStairs = false;

    // ========== Circle Events (Tag-based) ==========
    public static event Action<CircleType> PlayerEnteredCircle;
    public static event Action<CircleType> PlayerExitedCircle;

    // ========== Passenger Events ==========
    public static event Action<PassengerController> PassengerHandedBaggage;
    public static event Action<PassengerController> PassengerReachedTarget;
    public static event Action<PassengerController> PassengerStateChanged;
    public static event Action<PassengerController> PassengerReachedFront;
    public static event Action<PassengerController> PassengerReachedStairs;
    public static event Action<PassengerController> PassengerReachedTopStairs;
    public static event Action<PassengerController> PassengerReachedXRayEnd;
    public static event Action<PassengerController> PassengerReachedUpperQueue;
    public static event Action<PassengerController> PassengerStartedXRayInspection; // Passenger XRay inspection point'e ulaştı
    public static event Action<PassengerController> PassengerCompletedXRayInspection; // Passenger XRay inspection tamamlandı

    // ========== Baggage Events ==========
    public static event Action<GameObject> BaggageReachedUnloadEnd;
    public static event Action<GameObject> BaggageReachedPlatform;
    public static event Action<GameObject> BaggageReachedTruck;
    public static event Action<GameObject> BaggageCompletedXray; // X-Ray yolunu tamamladı
    public static event Action BaggageReachedEnd;
    public static event Action PlayerEnteredBaggageXray;
    public static event Action PlayerExitedBaggageXray;
    public static event Action AllBaggagesLoadedToTruck; // Tüm bagajlar truck'a yüklendi


    // ========== Platform Events ==========
    public static event Action PlatformReachedTop;    // Platform en üst noktaya ulaştı
    public static event Action PlatformReachedBottom; // Platform en alt noktaya ulaştı

    // ========== Truck Events ==========
    public static event Action TruckReachedDestination; // Truck hedefe ulaştı

    // ========== Stairs Events ==========
    public static event Action<GameObject> StairsUnlocked; // Merdiven unlock oldu (parent object gönderilir)

    // ========== Board Events ==========
    public static event Action<Transform> BoardUnlocked; // Board unlock oldu (board transform gönderilir)

    // ========== State Tracking ==========
    public static bool IsPlayerInCircle { get; private set; } = false;
    public static CircleType? CurrentCircleType { get; private set; } = null;

    // ========== Circle Event Raisers ==========
    public static void RaisePlayerEnteredCircle(CircleType circleType)
    {
        IsPlayerInCircle = true;
        CurrentCircleType = circleType;
        PlayerEnteredCircle?.Invoke(circleType);
    }

    public static void RaisePlayerExitedCircle(CircleType circleType)
    {
        IsPlayerInCircle = false;
        CurrentCircleType = null;
        PlayerExitedCircle?.Invoke(circleType);
    }

    public static void RaisePassengerHandedBaggage(PassengerController passenger)
        => PassengerHandedBaggage?.Invoke(passenger);

    public static void RaisePassengerReachedTarget(PassengerController passenger)
        => PassengerReachedTarget?.Invoke(passenger);

    public static void RaisePassengerStateChanged(PassengerController passenger)
        => PassengerStateChanged?.Invoke(passenger);

    public static void RaisePassengerReachedFront(PassengerController passenger)
        => PassengerReachedFront?.Invoke(passenger);

    public static void RaisePassengerReachedStairs(PassengerController p)
        => PassengerReachedStairs?.Invoke(p);

    public static void RaisePassengerReachedTopStairs(PassengerController p)
        => PassengerReachedTopStairs?.Invoke(p);

    public static void RaisePassengerReachedXRayEnd(PassengerController p)
    => PassengerReachedXRayEnd?.Invoke(p);

    public static void RaisePassengerReachedUpperQueue(PassengerController p)
        => PassengerReachedUpperQueue?.Invoke(p);

    public static void RaisePassengerStartedXRayInspection(PassengerController p)
        => PassengerStartedXRayInspection?.Invoke(p);

    public static void RaisePassengerCompletedXRayInspection(PassengerController p)
        => PassengerCompletedXRayInspection?.Invoke(p);

    // ========== Baggage Event Raisers ==========
    public static void RaiseBaggageReachedUnloadEnd(GameObject baggage)
        => BaggageReachedUnloadEnd?.Invoke(baggage);

    public static void RaiseBaggageReachedPlatform(GameObject baggage)
        => BaggageReachedPlatform?.Invoke(baggage);

    public static void RaiseBaggageReachedTruck(GameObject baggage)
        => BaggageReachedTruck?.Invoke(baggage);

    public static void RaiseBaggageCompletedXray(GameObject baggage)
        => BaggageCompletedXray?.Invoke(baggage);

    public static void RaiseBaggageReachedEnd()
        => BaggageReachedEnd?.Invoke();

    public static void RaisePlayerEnteredBaggageXray()
        => PlayerEnteredBaggageXray?.Invoke();

    public static void RaisePlayerExitedBaggageXray()
        => PlayerExitedBaggageXray?.Invoke();

    public static void RaiseAllBaggagesLoadedToTruck()
        => AllBaggagesLoadedToTruck?.Invoke();

    // ========== Platform Event Raisers ==========
    public static void RaisePlatformReachedTop()
        => PlatformReachedTop?.Invoke();

    public static void RaisePlatformReachedBottom()
        => PlatformReachedBottom?.Invoke();

    // ========== Truck Event Raisers ==========
    public static void RaiseTruckReachedDestination()
        => TruckReachedDestination?.Invoke();

    // ========== Stairs Event Raisers ==========
    public static void RaiseStairsUnlocked(GameObject stairsObject)
        => StairsUnlocked?.Invoke(stairsObject);

    // ========== Board Event Raisers ==========
    public static void RaiseBoardUnlocked(Transform boardTransform)
        => BoardUnlocked?.Invoke(boardTransform);

}
