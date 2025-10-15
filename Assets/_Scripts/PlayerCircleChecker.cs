using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PlayerCircleChecker : MonoBehaviour
{
    private void Awake()
    {
        if (!GetComponent<Collider>().isTrigger)
            Debug.LogWarning("PlayerCircleChecker collider should be trigger.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            EventBus.RaisePlayerEnteredCircle();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            EventBus.RaisePlayerExitedCircle();
    }
}
