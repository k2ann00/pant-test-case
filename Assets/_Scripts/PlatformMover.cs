using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Yukarı aşağı hareket eden platform - Event-driven
/// SOLID: Single Responsibility - Sadece platform hareketi
/// Pattern: Event-driven - Manager'dan komut alır, olayları bildirir
/// </summary>
public class PlatformMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private Transform bottomPosition;
    [SerializeField] private Transform topPosition;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [SerializeField] private bool manualControl = false; // True ise autoStart çalışmaz, event-driven olur

    [Header("Top Position Trigger")]
    [SerializeField] private float topPositionThreshold = 0.1f;

    private bool isRunning = false;
    private bool isAtTop = false;
    private bool isAtBottom = true;
    private Tween activeTween;

    /// <summary>
    /// Platform şu anda hareket ediyor mu?
    /// </summary>
    public bool IsMoving => activeTween != null && activeTween.IsActive();

    /// <summary>
    /// Platform üstte mi?
    /// </summary>
    public bool IsAtTop => isAtTop;

    /// <summary>
    /// Platform altta mı?
    /// </summary>
    public bool IsAtBottom => isAtBottom;

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
    }
    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
    }


    private void OnPlayerEnteredCircle(CircleType type)
    {
        StartCoroutine(AutoMovementCycle());
    }
    private void OnPlayerExitedCircle(CircleType type)
    {
        StopMoving();
    }

    private void Start()
    {
        // Başlangıç pozisyonuna yerleş
        if (bottomPosition != null)
        {
            transform.position = bottomPosition.position;
            isAtBottom = true;
            isAtTop = false;
        }

        //// Manuel kontrol değilse otomatik başlat
        //if (!manualControl)
        //{
        //    StartAutoMovement();
        //}
    }

    /// <summary>
    /// Otomatik döngüsel hareketi başlatır (eski sistem)
    /// </summary>
    //[ContextMenu("Start Auto Movement")]
    //public void StartAutoMovement()
    //{
    //    if (isRunning)
    //    {
    //        Debug.LogWarning($"[{name}] Platform already moving!");
    //        return;
    //    }

    //    if (bottomPosition == null || topPosition == null)
    //    {
    //        Debug.LogError($"[{name}] Bottom or Top position is not assigned!");
    //        return;
    //    }

    //    isRunning = true;
    //    Debug.Log($"✅ [{name}] Platform auto-movement started");
    //    StartCoroutine(AutoMovementCycle());
    //}

    /// <summary>
    /// Platform'u yukarı gönder (event-driven)
    /// </summary>
    public void MoveToTop()
    {
        if (IsMoving)
        {
            Debug.LogWarning($"[{name}] Platform is already moving!");
            return;
        }

        if (isAtTop)
        {
            Debug.LogWarning($"[{name}] Platform is already at top!");
            return;
        }

        Debug.Log($"⬆️ [{name}] Moving to TOP");
        StartCoroutine(MoveToPosition(topPosition.position, true));
    }

    /// <summary>
    /// Platform'u aşağı gönder (event-driven)
    /// </summary>
    public void MoveToBottom()
    {
        if (IsMoving)
        {
            Debug.LogWarning($"[{name}] Platform is already moving!");
            return;
        }

        if (isAtBottom)
        {
            Debug.LogWarning($"[{name}] Platform is already at bottom!");
            return;
        }

        Debug.Log($"⬇️ [{name}] Moving to BOTTOM");
        StartCoroutine(MoveToPosition(bottomPosition.position, false));
    }

    /// <summary>
    /// Platform hareketini durdurur
    /// </summary>
    [ContextMenu("Stop Moving")]
    public void StopMoving()
    {
        isRunning = false;

        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }

        StopAllCoroutines();
        Debug.Log($"🛑 [{name}] Platform stopped");
    }

    /// <summary>
    /// Otomatik döngüsel hareket (eski sistem - geriye dönük uyumluluk için)
    /// </summary>
    private IEnumerator AutoMovementCycle()
    {
        while (isRunning)
        {
            // Aşağıdan yukarıya
            yield return MoveToPosition(topPosition.position, true);

            // Yukarıdan aşağıya
            yield return MoveToPosition(bottomPosition.position, false);
        }
    }

    /// <summary>
    /// Belirtilen pozisyona hareket eder + EventBus event fırlatır
    /// </summary>
    private IEnumerator MoveToPosition(Vector3 targetPosition, bool movingToTop)
    {
        // State güncelle
        isAtTop = false;
        isAtBottom = false;

        activeTween = transform.DOMove(targetPosition, moveDuration)
            .SetEase(moveEase)
            .OnComplete(() =>
            {
                if (movingToTop)
                {
                    isAtTop = true;
                    isAtBottom = false;
                    Debug.Log($"✅ [{name}] Reached TOP - Raising event");
                    EventBus.RaisePlatformReachedTop(); // 🔔 Event fırlat
                }
                else
                {
                    isAtTop = false;
                    isAtBottom = true;
                    Debug.Log($"✅ [{name}] Reached BOTTOM - Raising event");
                    EventBus.RaisePlatformReachedBottom(); // 🔔 Event fırlat
                }
            });

        // Hareket tamamlanana kadar bekle
        yield return activeTween.WaitForCompletion();
    }

    /// <summary>
    /// Tek cycle süresi (yukarı + aşağı)
    /// </summary>
    public float GetCycleDuration()
    {
        return moveDuration * 2f; // Up + Down
    }

    /// <summary>
    /// Inspector validation
    /// </summary>
    private void OnValidate()
    {
        if (bottomPosition == null)
            Debug.LogWarning($"[{name}] Bottom position is not assigned!");

        if (topPosition == null)
            Debug.LogWarning($"[{name}] Top position is not assigned!");

        if (moveDuration <= 0)
        {
            Debug.LogWarning($"[{name}] Move duration must be greater than 0!");
            moveDuration = 1f;
        }

        // Manuel kontrol uyarısı
        if (manualControl)
        {
            Debug.Log($"ℹ️ [{name}] Manual control enabled - Use MoveToTop()/MoveToBottom() methods or events");
        }
    }

    /// <summary>
    /// Gizmo çizimi (Editor'da görselleştirme)
    /// </summary>
    private void OnDrawGizmos()
    {
        if (bottomPosition == null || topPosition == null)
            return;

        // Bottom pozisyon (kırmızı)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bottomPosition.position, 0.3f);
        Gizmos.DrawLine(bottomPosition.position + Vector3.left * 0.5f, bottomPosition.position + Vector3.right * 0.5f);

        // Top pozisyon (yeşil)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(topPosition.position, 0.3f);
        Gizmos.DrawLine(topPosition.position + Vector3.left * 0.5f, topPosition.position + Vector3.right * 0.5f);

        // Hareket yolu (sarı çizgi)
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(bottomPosition.position, topPosition.position);

        // Platform mevcut pozisyon (mavi)
        if (Application.isPlaying)
        {
            Gizmos.color = isAtTop ? Color.cyan : Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }

    private void OnDestroy()
    {
        // Cleanup
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }
}
