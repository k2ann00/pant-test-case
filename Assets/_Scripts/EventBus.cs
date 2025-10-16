using System;

public static class EventBus
{
    public static event Action PlayerEnteredCircle;
    public static event Action PlayerExitedCircle;
    public static event Action<PassengerController> PassengerHandedBaggage;
    public static event Action<PassengerController> PassengerReachedTarget;
    public static event Action<PassengerController> PassengerStateChanged;
    public static event Action<PassengerController> PassengerReachedFront; // 🔹 eklendi

    public static bool IsPlayerInCircle { get; private set; } = false;

    public static void RaisePlayerEnteredCircle()
    {
        IsPlayerInCircle = true;
        PlayerEnteredCircle?.Invoke();
    }

    public static void RaisePlayerExitedCircle()
    {
        IsPlayerInCircle = false;
        PlayerExitedCircle?.Invoke();
    }

    public static void RaisePassengerHandedBaggage(PassengerController passenger)
        => PassengerHandedBaggage?.Invoke(passenger);

    public static void RaisePassengerReachedTarget(PassengerController passenger)
        => PassengerReachedTarget?.Invoke(passenger);

    public static void RaisePassengerStateChanged(PassengerController passenger)
        => PassengerStateChanged?.Invoke(passenger);

    public static void RaisePassengerReachedFront(PassengerController passenger)
        => PassengerReachedFront?.Invoke(passenger);
}
