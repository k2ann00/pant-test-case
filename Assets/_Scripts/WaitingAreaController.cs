using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaitingAreaController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.green;

            EventBus.RaisePlayerEnteredCircle();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red;

            EventBus.RaisePlayerExitedCircle();
        }
    }
}
