using System;
using UnityEngine;

/// <summary>
/// Circle türlerini tanımlayan enum
/// </summary>
public enum CircleType
{
    WelcomingCircle,    // Passenger bagaj teslimi
    BaggageUnload,      // Bagaj boşaltma - X-Ray'e
    BaggageXray,        // X-Ray işlemi
    PassengerXray       // Passenger X-Ray kontrol
}

/// <summary>
/// Event-Driven mimari için merkezi event sistemi
/// SOLID: Single Responsibility - Sadece event yönetimi
/// </summary>
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

    // ========== Baggage Events ==========
    public static event Action<GameObject> BaggageReachedUnloadEnd;
    public static event Action<GameObject> BaggageReachedPlatform;
    public static event Action<GameObject> BaggageReachedTruck;
    public static event Action<GameObject> BaggageCompletedXray; // X-Ray yolunu tamamladı

    // ========== Platform Events ==========
    public static event Action PlatformReachedTop;    // Platform en üst noktaya ulaştı
    public static event Action PlatformReachedBottom; // Platform en alt noktaya ulaştı

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

    // ========== Baggage Event Raisers ==========
    public static void RaiseBaggageReachedUnloadEnd(GameObject baggage)
        => BaggageReachedUnloadEnd?.Invoke(baggage);

    public static void RaiseBaggageReachedPlatform(GameObject baggage)
        => BaggageReachedPlatform?.Invoke(baggage);

    public static void RaiseBaggageReachedTruck(GameObject baggage)
        => BaggageReachedTruck?.Invoke(baggage);

    public static void RaiseBaggageCompletedXray(GameObject baggage)
        => BaggageCompletedXray?.Invoke(baggage);

    // ========== Platform Event Raisers ==========
    public static void RaisePlatformReachedTop()
        => PlatformReachedTop?.Invoke();

    public static void RaisePlatformReachedBottom()
        => PlatformReachedBottom?.Invoke();
}
