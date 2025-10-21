using System;
using UnityEngine;


[RequireComponent(typeof(Collider))]
public class WaitingAreaController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private CircleType circleType;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            spriteRenderer.color = Color.red;

        // Tag'den CircleType parse et
        circleType = ParseCircleTypeFromTag();
        Debug.Log($" [{name}] WaitingAreaController initialized as: {circleType}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.green;

            EventBus.RaisePlayerEnteredCircle(circleType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red;

            EventBus.RaisePlayerExitedCircle(circleType);
        }
    }

    private CircleType ParseCircleTypeFromTag()
    {
        if (Enum.TryParse<CircleType>(gameObject.tag, out CircleType result))
        {
            return result;
        }

        Debug.LogWarning($"[{name}] Unknown tag '{gameObject.tag}', defaulting to WelcomingCircle");
        return CircleType.WelcomingCircle;
    }
}
