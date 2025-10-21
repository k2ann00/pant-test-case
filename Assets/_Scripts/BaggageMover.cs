using DG.Tweening;
using UnityEngine;

public class BaggageMover : MonoBehaviour
{
    private enum BaggageState
    {
        Idle,
        MovingOnConveyor,   // Conveyor belt üzerinde +X yönünde hareket
        JumpingToPlatform,  // Platform'a zıplama
        OnPlatform,         // Platform üzerinde (yukarı aşağı hareket)
        MovingToTruck,      // Kamyonete taşınma
        InTruck             // Kamyonette
    }

    private BaggageState currentState = BaggageState.Idle;
    private Transform platformParent;
    private Tween activeTween;

    /// <summary>
    /// Conveyor belt hareketini başlatır
    /// </summary>
    public void Initialize(Vector3 startPos, Vector3 endPos, float speed, Quaternion? rotation = null)
    {
        // Başlangıç pozisyonuna ışınlan
        transform.position = startPos;

        // Eğer rotation verilmişse onu kullan, yoksa mevcut rotasyonu koru
        if (rotation.HasValue)
        {
            transform.rotation = rotation.Value;
        }
        // Rotasyon verilmemişse mevcut rotasyonu koruyoruz (değiştirmiyoruz)

        currentState = BaggageState.MovingOnConveyor;

        // +X yönünde hareket (DOTween ile)
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / speed;

        Debug.Log($"[{name}] Moving on conveyor from {startPos} to {endPos} | Duration: {duration}s");

        activeTween = transform.DOMove(endPos, duration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log($"[{name}] Reached conveyor end");
                currentState = BaggageState.Idle;
                EventBus.RaiseBaggageReachedUnloadEnd(gameObject);
            });
    }
    
    /// <summary>
    /// Platform'a jump hareketi
    /// </summary>
    public void JumpToPlatform(Transform platform, float jumpHeight, float jumpDuration)
    {
        if (platform == null)
        {
            Debug.LogError($"[{name}] Platform target is null!");
            return;
        }

        platformParent = platform;
        currentState = BaggageState.JumpingToPlatform;

        // DOTween Jump
        Debug.Log($"[{name}] Jumping to platform at {platform.position}");

        activeTween = transform.DOJump(platform.position, jumpHeight, 1, jumpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                Debug.Log($"[{name}] Landed on platform");

                // Platform'a parent yap (yukarı aşağı hareket için)
                transform.SetParent(platform);
                currentState = BaggageState.OnPlatform;

                EventBus.RaiseBaggageReachedPlatform(gameObject);
            });
    }

    /// <summary>
    /// Kamyonete taşıma hareketi
    /// </summary>
    public void MoveToTruck(Vector3 truckPosition)
    {
        // Platform parent'ı kaldır
        transform.SetParent(null);

        currentState = BaggageState.MovingToTruck;

        Debug.Log($"[{name}] Moving to truck at {truckPosition}");

        // Kamyonete taşı (DOTween ile)
        activeTween = transform.DOMove(truckPosition, 0.5f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                Debug.Log($"[{name}] Loaded to truck");
                currentState = BaggageState.InTruck;

                EventBus.RaiseBaggageReachedTruck(gameObject);
            });
    }

    /// <summary>
    /// Truck'a jump hareketi (platform'dan sonra)
    /// </summary>
    public void JumpToTruck(Vector3 truckPosition, float jumpHeight, float jumpDuration)
    {
        // Platform parent'ı kaldır
        transform.SetParent(null);

        currentState = BaggageState.MovingToTruck;

        Debug.Log($"[{name}] Jumping to truck at {truckPosition}");

        // Truck'a zıpla (DOTween Jump)
        activeTween = transform.DOJump(truckPosition, jumpHeight, 1, jumpDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                Debug.Log($"[{name}] Landed in truck");
                currentState = BaggageState.InTruck;

                EventBus.RaiseBaggageReachedTruck(gameObject);
            });
    }

    /// <summary>
    /// Acil durdurma (gerekirse)
    /// </summary>
    public void Stop()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }

        currentState = BaggageState.Idle;
    }

    private void OnDestroy()
    {
        // Cleanup
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }

    // Debug için
    private void OnDrawGizmos()
    {
        // State bazlı renk
        switch (currentState)
        {
            case BaggageState.MovingOnConveyor:
                Gizmos.color = Color.yellow;
                break;
            case BaggageState.JumpingToPlatform:
                Gizmos.color = Color.cyan;
                break;
            case BaggageState.OnPlatform:
                Gizmos.color = Color.magenta;
                break;
            case BaggageState.MovingToTruck:
                Gizmos.color = Color.green;
                break;
            case BaggageState.InTruck:
                Gizmos.color = Color.white;
                break;
            default:
                Gizmos.color = Color.gray;
                break;
        }

        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
