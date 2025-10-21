using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;


[RequireComponent(typeof(BarFill))]
public class UnlockableArea : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BarFill barFill;
    [SerializeField] private TextMeshPro costText;
    [SerializeField] private Transform arrowPosition; // Position for arrow mark

    [Header("Locked Objects (will be disabled on unlock)")]
    [SerializeField] private GameObject[] lockedObjects;

    [Header("Unlocked Objects (will be enabled on unlock)")]
    [SerializeField] private GameObject[] unlockedObjects;

    [Header("Animation Settings")]
    [SerializeField] private float unlockAnimationDuration = 0.8f;
    [SerializeField] private float scaleUpMultiplier = 1.2f; // 0 -> 1.2 -> 1.0
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("Board Settings")]
    [SerializeField] private bool isBoardArea = false; // Bu bir board unlock area mı?
    [SerializeField] private Transform boardTransform; // Board objesinin transform'u (sadece isBoardArea=true ise)

    private bool isInitialized = false;

    private void Awake()
    {
        // Auto-find BarFill if not assigned
        if (barFill == null)
            barFill = GetComponent<BarFill>();

        if (barFill == null)
        {
            Debug.LogError($"[{name}] UnlockableArea requires a BarFill component!");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (barFill != null)
        {
            barFill.OnProgressChanged += OnProgressChanged;
            barFill.OnUnlockCompleted += OnUnlockCompleted;
        }
    }

    private void OnDisable()
    {
        if (barFill != null)
        {
            barFill.OnProgressChanged -= OnProgressChanged;
            barFill.OnUnlockCompleted -= OnUnlockCompleted;
        }
    }

    private void Start()
    {
        // Initialize locked/unlocked object states
        InitializeObjectStates();
        ReferanceCheck();

        // Show arrow if GameManager exists
        if (arrowPosition != null && GameManager.Instance != null)
        {
            GameManager.Instance.ShowArrow(arrowPosition.position);
        }

        isInitialized = true;
    }

    private void InitializeObjectStates()
    {
        bool isUnlocked = barFill != null && barFill.IsUnlocked;

        // Set locked objects state
        foreach (var obj in lockedObjects)
        {
            if (obj != null)
                obj.SetActive(!isUnlocked);
        }

        // Set unlocked objects state
        foreach (var obj in unlockedObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isUnlocked);

                // If already unlocked, ensure scale is normal
                if (isUnlocked)
                    obj.transform.localScale = Vector3.one;
            }
        }
    }

    private void OnProgressChanged(float fill, int remaining)
    {
        // Update cost text
        if (costText != null)
        {
            costText.text = remaining.ToString();
        }
    }


    private void OnUnlockCompleted()
    {
        Debug.Log($"[{name}] Unlock completed! Starting unlock sequence...");

        // Hide arrow
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HideArrow();
        }

        // Start unlock sequence
        StartCoroutine(UnlockSequence());
    }


    private IEnumerator UnlockSequence()
    {
        // 1. Disable locked objects (instant)
        foreach (var obj in lockedObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }

        // Small delay before showing unlocked objects
        yield return new WaitForSeconds(0.2f);

        // 2. Enable and animate unlocked objects
        foreach (var obj in unlockedObjects)
        {
            if (obj != null)
            {
                // Set scale to zero
                obj.transform.localScale = Vector3.zero;

                // Activate object
                obj.SetActive(true);

                // Animate scale: 0 -> scaleUp -> 1.0
                AnimateUnlockedObject(obj.transform);
            }
        }

        // 3. Wait for scale animation to complete
        yield return new WaitForSeconds(unlockAnimationDuration);

        // 4. Now trigger stair events AFTER scale animation is done (scale = 1.0)
        foreach (var obj in unlockedObjects)
        {
            if (obj != null)
            {
                StairsController stairsController = obj.GetComponent<StairsController>();
                if (stairsController != null)
                {
                    EventBus.RaiseStairsUnlocked(obj);
                }
            }
        }

        // 5. If this is a board area, trigger board event
        if (isBoardArea)
        {
            if (boardTransform != null)
            {
                Debug.Log($"[{name}] Triggering BoardUnlocked event for {boardTransform.name}");
                EventBus.RaiseBoardUnlocked(boardTransform);
            }
            else
            {
                // Otomatik olarak unlockedObjects içinde "Board" isimli objeyi bul
                foreach (var obj in unlockedObjects)
                {
                    if (obj != null && obj.name.Contains("Board"))
                    {
                        Debug.Log($"[{name}] Auto-detected board: {obj.name}");
                        EventBus.RaiseBoardUnlocked(obj.transform);
                        break;
                    }
                }
            }
        }
    }


    private void AnimateUnlockedObject(Transform target)
    {
        if (target == null) return;

        Vector3 overshootScale = Vector3.one * scaleUpMultiplier;
        Vector3 finalScale = Vector3.one;

        Sequence seq = DOTween.Sequence();

        // 0 -> overshoot (60% of duration)
        seq.Append(target.DOScale(overshootScale, unlockAnimationDuration * 0.6f)
            .SetEase(scaleEase));

        // overshoot -> final (40% of duration)
        seq.Append(target.DOScale(finalScale, unlockAnimationDuration * 0.4f)
            .SetEase(Ease.OutQuad));
    }


    private void ReferanceCheck()
    {
        if (barFill == null)
            barFill = GetComponent<BarFill>();

        if (barFill == null)
            Debug.LogWarning($"[{name}] BarFill component not found!");

        if (costText == null)
            Debug.LogWarning($"[{name}] Cost text not assigned!");

        if (lockedObjects == null || lockedObjects.Length == 0)
            Debug.LogWarning($"[{name}] No locked objects assigned!");

        if (unlockedObjects == null || unlockedObjects.Length == 0)
            Debug.LogWarning($"[{name}] No unlocked objects assigned!");

        if (unlockAnimationDuration <= 0)
        {
            Debug.LogWarning($"[{name}] Unlock animation duration must be greater than 0!");
            unlockAnimationDuration = 0.8f;
        }
    }
}
