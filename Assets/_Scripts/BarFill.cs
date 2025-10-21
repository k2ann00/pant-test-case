using DG.Tweening;
using TMPro;
using UnityEngine;
using System;


public class BarFill : MonoBehaviour, ITransferTarget
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private GameObject moneyPrefab;
    [SerializeField] private Transform moneyTarget;

    [Header("Settings")]
    [SerializeField] private string unlockId = ""; // unique id for this unlockable area
    [SerializeField] private int upgradeCost = 500;
    [SerializeField] private int transferSpeed = 100;
    [SerializeField] private int coinValue = 10;
    [SerializeField] private float coinTravelDuration = 0.6f;
    [SerializeField] private float jumpPower = 0.8f;
    [SerializeField] private int jumpCount = 1;

    [Header("Coin Animation FX")]
    [SerializeField] private float scaleUp = 1.4f;      // zıplarken büyüme oranı

    // Events for communication with UnlockableArea
    public event Action<float, int> OnProgressChanged; // fill, remaining
    public event Action OnUnlockCompleted;

    private Material matInstance;
    private bool isPlayerInRange;
    private bool isUnlocked = false;
    private Collider myCollider;

    private void Start()
    {
        matInstance = targetRenderer.material;
        matInstance.SetFloat("_Fill", 0f);

        if (MoneyPool.Instance == null)
        {
            var go = new GameObject("MoneyPool");
            go.AddComponent<MoneyPool>();
        }

        myCollider = GetComponent<Collider>();

        // if already unlocked (from previous session), reflect that
        if (!string.IsNullOrEmpty(unlockId) && MoneyManager.Instance != null && MoneyManager.Instance.IsUnlocked(unlockId))
        {
            ApplyUnlockedState();
        }
    }

    private void Update()
    {
        if (isUnlocked) return;

        if (isPlayerInRange)
        {
            MoneyManager.Instance.StartTransfer(this);
        }
        else
        {
            MoneyManager.Instance.StopTransfer(this);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isPlayerInRange = false;
    }

    // ITransferTarget implementation
    public TransferSettings GetTransferSettings()
    {
        return new TransferSettings
        {
            unlockId = unlockId,
            upgradeCost = upgradeCost,
            transferSpeed = transferSpeed,
            coinValue = coinValue,
            coinTravelDuration = coinTravelDuration,
            jumpPower = jumpPower,
            jumpCount = jumpCount,
            scaleUp = scaleUp,
            moneyPrefab = moneyPrefab,
            moneyTarget = moneyTarget,
            spawnOffset = Vector3.up * 0.2f
        };
    }

    public void OnTransferProgress(float fill, int remaining)
    {
        matInstance.SetFloat("_Fill", fill);
        targetRenderer.material = matInstance;

        // Notify listeners (UnlockableArea)
        OnProgressChanged?.Invoke(fill, remaining);
    }

    public void OnTransferCompleted()
    {
        ApplyUnlockedState();
    }

    private void ApplyUnlockedState()
    {
        isUnlocked = true;

        if (myCollider != null)
            myCollider.enabled = false;

        // Mark as unlocked in MoneyManager
        if (!string.IsNullOrEmpty(unlockId) && MoneyManager.Instance != null)
            MoneyManager.Instance.MarkUnlocked(unlockId);

        // Notify listeners (UnlockableArea)
        OnUnlockCompleted?.Invoke();
    }
    public bool IsUnlocked => isUnlocked;
}
