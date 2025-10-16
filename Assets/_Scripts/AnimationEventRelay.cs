using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    public PassengerController passenger;

    private void Start()
    {
        passenger = GetComponentInParent<PassengerController>();
        if (passenger == null) Debug.LogError("PassangerController couldnt find");
    }
    public void OnHandAnimationFinishedForRelay()
    {
        Debug.Log("Bu artýk gereksiz bir script");
    }
}
