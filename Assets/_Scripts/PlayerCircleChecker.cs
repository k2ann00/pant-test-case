using System;
using UnityEngine;

/// <summary>
/// Player circle detection - Tag bazlı çalışır
/// SOLID: Single Responsibility - Sadece circle detection
/// SOLID: Open-Closed - Yeni circle tipleri için kod değişikliği gerekmez
/// </summary>
[RequireComponent(typeof(Collider))]
public class PlayerCircleChecker : MonoBehaviour
{
    private CircleType circleType;

    private void Awake()
    {
        // Collider kontrolü
        if (!GetComponent<Collider>().isTrigger)
            Debug.LogWarning($"[{name}] PlayerCircleChecker collider should be trigger.");

        // Tag'den CircleType'ı çözümle
        circleType = ParseCircleTypeFromTag();
        Debug.Log($"✅ [{name}] Circle initialized as: {circleType}");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"🔵 Player entered: {circleType}");
            EventBus.RaisePlayerEnteredCircle(circleType);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"🔴 Player exited: {circleType}");
            EventBus.RaisePlayerExitedCircle(circleType);
        }
    }

    /// <summary>
    /// GameObject tag'ini CircleType enum'a çevirir
    /// </summary>
    private CircleType ParseCircleTypeFromTag()
    {
        // Tag direkt CircleType enum ismi olmalı (WelcomingCircle, BaggageUnload, etc.)
        if (Enum.TryParse<CircleType>(gameObject.tag, out CircleType result))
        {
            return result;
        }

        // Fallback - default WelcomingCircle
        Debug.LogWarning($"[{name}] Unknown tag '{gameObject.tag}', defaulting to WelcomingCircle");
        return CircleType.WelcomingCircle;
    }
}
