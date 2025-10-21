using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
public class PlatformMover : MonoBehaviour
{
    public static PlatformMover Instance { get; private set; }
    [Header("Movement Settings")]
    [SerializeField] private Transform bottomPosition;
    [SerializeField] private Transform topPosition;
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;

    [Header("Top Position Trigger")]
    private bool isRunning = false;
    private bool isAtTop = false;
    private bool isAtBottom = true;
    private Tween activeTween;


    public bool IsMoving => activeTween != null && activeTween.IsActive();

    public bool IsAtTop => isAtTop;


    public bool IsAtBottom => isAtBottom;
    public bool playerInBaggageXrayZone;


    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
        EventBus.BaggageReachedEnd += OnBaggageReachedEnd;
    }
    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
        EventBus.BaggageReachedEnd -= OnBaggageReachedEnd;
    }



    private void OnPlayerEnteredCircle(CircleType type)
    {
        // Sadece BaggageXray alanında olduğunda izin ver
        if (type == CircleType.BaggageXray)
        {
            playerInBaggageXrayZone = true;
            EventBus.RaisePlayerEnteredBaggageXray();
            Debug.Log("Oyuncu BaggageXray alanına girdi.");
        }
    }

    private void OnPlayerExitedCircle(CircleType type)
    {
        if (type == CircleType.BaggageXray)
        {
            playerInBaggageXrayZone = false;
            EventBus.RaisePlayerExitedBaggageXray();
            Debug.Log("Oyuncu BaggageXray alanından çıktı.");
        }
    }
    private void Awake() => Instance = this;



    private void Start()
    {
        // Başlangıç pozisyonuna yerleş
        if (bottomPosition != null)
        {
            transform.position = bottomPosition.position;
            isAtBottom = true;
            isAtTop = false;
        }
    }
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


    [ContextMenu("Stop Moving")]
    public void StopMoving()
    {
        isRunning = false;

        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }

        StopAllCoroutines();
        Debug.Log($"[{name}] Platform stopped");
    }


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
                    Debug.Log($" [{name}] Reached TOP - Raising event");
                    EventBus.RaisePlatformReachedTop(); // 🔔 Event fırlat
                }
                else
                {
                    isAtTop = false;
                    isAtBottom = true;
                    Debug.Log($" [{name}] Reached BOTTOM - Raising event");
                    EventBus.RaisePlatformReachedBottom(); // 🔔 Event fırlat
                }
            });

        // Hareket tamamlanana kadar bekle
        yield return activeTween.WaitForCompletion();
    }


    public float GetCycleDuration()
    {
        return moveDuration * 2f; // Up + Down
    }


    private void OnBaggageReachedEnd()
    {
        if (!playerInBaggageXrayZone)
        {
            Debug.Log("Bagaj sona geldi ama oyuncu Xray bölgesinde değil.");
            return;
        }

        Debug.Log("Bagaj sona geldi + Xray aktif → Platform hareket ediyor!");
        MoveToTop(); // veya StartCoroutine(AutoMovementCycle());
    }


    private void OnValidate()
    {
        if (moveDuration <= 0)
        {
            moveDuration = 1f;
        }
    }


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
