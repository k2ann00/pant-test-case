using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    public event Action<int> OnMoneyChanged; // yeni bakiye g�nderilecek

    [SerializeField] public int money;
    public int Money => money;

    private class TransferSession
    {
        public ITransferTarget Target;
        public TransferSettings Settings;
        public int Transferred;
        public float SpawnAccumulator;
        public float TransferAccumulator; // fractional transfer accumulator
    }

    private readonly Dictionary<ITransferTarget, TransferSession> sessions = new Dictionary<ITransferTarget, TransferSession>();

    // completed unlock ids (in-memory only for this case-study)
    private readonly HashSet<string> completedUnlockIds = new HashSet<string>();

    // NOTE: Persistence via PlayerPrefs is intentionally disabled for the case study.
    // private const string PlayerPrefsPrefix = "unlocked_area_";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        
    }
    private void Update()
    {
        if (sessions.Count == 0) return;

        float dt = Time.deltaTime;

        // Iterate over a copy to allow removal during iteration
        var keys = new List<ITransferTarget>(sessions.Keys);
        foreach (var key in keys)
        {
            var s = sessions[key];
            // If already completed, skip
            if (s.Transferred >= s.Settings.upgradeCost)
            {
                CompleteSession(s);
                continue;
            }

            // Compute transfer amount with fractional accumulator
            s.TransferAccumulator += s.Settings.transferSpeed * dt;
            int transferInt = Mathf.FloorToInt(s.TransferAccumulator);
            if (transferInt > 0)
            {
                s.TransferAccumulator -= transferInt;

                int remaining = s.Settings.upgradeCost - s.Transferred;
                int canTake = Math.Min(transferInt, remaining);
                int actuallyTaken = Math.Min(canTake, money);

                if (actuallyTaken > 0)
                {
                    money -= actuallyTaken;
                    OnMoneyChanged?.Invoke(money);
                    s.Transferred += actuallyTaken;
                    s.SpawnAccumulator += actuallyTaken;

                    // Notify target about progress
                    float fill = Mathf.Clamp01((float)s.Transferred / s.Settings.upgradeCost);
                    s.Target.OnTransferProgress(fill, s.Settings.upgradeCost - s.Transferred);

                    // Spawn coins according to coinValue
                    while (s.SpawnAccumulator >= s.Settings.coinValue)
                    {
                        s.SpawnAccumulator -= s.Settings.coinValue;
                        SpawnCoin(s);
                    }
                }
            }

            // If after transfers we've reached completion
            if (s.Transferred >= s.Settings.upgradeCost)
            {
                CompleteSession(s);
            }

            // If no money left globally, we just wait until player has money
        }
    }

    private void CompleteSession(TransferSession s)
    {
        s.Target.OnTransferCompleted();
        sessions.Remove(s.Target);

        // mark unlocked if id provided (in-memory only for case study)
        var id = s.Settings.unlockId;
        if (!string.IsNullOrEmpty(id))
        {
            completedUnlockIds.Add(id);
            // Persistence disabled for demo; uncomment to persist as PlayerPrefs
            // PersistUnlockedId(id);
        }
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (money < amount) return false;
        money -= amount;
        OnMoneyChanged?.Invoke(money);
        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        money += amount;
        OnMoneyChanged?.Invoke(money);
    }

    // Start transferring for a target (called by e.g. BarFill on player in range)
    public void StartTransfer(ITransferTarget target)
    {
        if (target == null) return;

        var settings = target.GetTransferSettings();
        if (!string.IsNullOrEmpty(settings.unlockId))
        {
            // Only check in-memory completed ids for this demo; persistence is disabled
            if (completedUnlockIds.Contains(settings.unlockId))
                return;

            // If persistence were enabled, you could also check persisted state:
            // if (completedUnlockIds.Contains(settings.unlockId) || IsPersistedUnlocked(settings.unlockId))
            //     return;
        }

        if (sessions.ContainsKey(target)) return; // already transferring

        // Configure pool limits based on cost
        if (settings.moneyPrefab != null && MoneyPool.Instance != null)
        {
            MoneyPool.Instance.ConfigureMaxByCost(settings.moneyPrefab, settings.upgradeCost, 5);
        }

        var s = new TransferSession
        {
            Target = target,
            Settings = settings,
            Transferred = 0,
            SpawnAccumulator = 0f,
            TransferAccumulator = 0f
        };

        sessions[target] = s;

        // initial progress update
        target.OnTransferProgress(0f, settings.upgradeCost);
    }

    public void StopTransfer(ITransferTarget target)
    {
        if (target == null) return;
        if (!sessions.TryGetValue(target, out var s)) return;

        sessions.Remove(target);
    }

    public bool IsUnlocked(string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId)) return false;
        // In-memory only for demo. If persistence is desired, include IsPersistedUnlocked(unlockId)
        return completedUnlockIds.Contains(unlockId);
    }

    public void MarkUnlocked(string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId)) return;
        completedUnlockIds.Add(unlockId);
        // Persistence disabled for demo; uncomment to persist
        // PersistUnlockedId(unlockId);
    }

    private void SpawnCoin(TransferSession s)
    {
        var settings = s.Settings;
        if (settings.moneyPrefab == null || MoneyPool.Instance == null) return;

        // Determine world spawn/target positions
        // Use target's transform if moneyTarget provided, otherwise use target's spawn offset in world space
        Vector3 startPos = Vector3.zero;
        Vector3 targetPos = Vector3.zero;

        // Try to get target as MonoBehaviour to read transform
        if (s.Target is MonoBehaviour mb)
        {
            startPos = mb.transform.position + settings.spawnOffset;
            targetPos = settings.moneyTarget != null ? settings.moneyTarget.position : startPos;
        }
        else
        {
            startPos = settings.spawnOffset;
            targetPos = settings.moneyTarget != null ? settings.moneyTarget.position : startPos;
        }

        var coin = MoneyPool.Instance.Get(settings.moneyPrefab);
        if (coin == null)
        {
            // Pool limit reached - skip
            return;
        }

        coin.transform.position = startPos;
        coin.transform.localScale = Vector3.one * 0.6f;
        coin.transform.DOKill();

        float randomX = UnityEngine.Random.Range(-0.3f, 0.3f);
        float randomZ = UnityEngine.Random.Range(-0.3f, 0.3f);
        Vector3 randomizedTarget = targetPos + new Vector3(randomX, 0f, randomZ);

        Sequence seq = DOTween.Sequence();
        seq.Append(coin.transform.DOJump(randomizedTarget, settings.jumpPower, settings.jumpCount, settings.coinTravelDuration)
            .SetEase(Ease.OutQuad));
        seq.Join(coin.transform.DOScale(settings.scaleUp, settings.coinTravelDuration * 0.5f).SetEase(Ease.OutQuad));
        seq.Append(coin.transform.DOScale(1f, settings.coinTravelDuration * 0.5f).SetEase(Ease.InQuad));

        coin.transform.DORotate(new Vector3(0f, 360f, 0f), settings.coinTravelDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear);

        seq.OnComplete(() =>
        {
            // Null check - coin might be destroyed
            if (coin != null && coin.transform != null)
            {
                coin.transform.localScale = Vector3.one;
                coin.transform.rotation = Quaternion.identity;
                if (MoneyPool.Instance != null)
                {
                    MoneyPool.Instance.Return(settings.moneyPrefab, coin);
                }
            }
        });
    }
}
