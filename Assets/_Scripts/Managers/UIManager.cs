using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private float throttleInterval = 0.1f; // seconds

    private Coroutine subscribeCoroutine;

    private int pendingAmount;
    private bool hasPending;
    private float lastUpdateTime = -999f;

    private void OnEnable()
    {
        // Try immediate subscribe; if MoneyManager not ready, start coroutine to wait
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateMoney;
            UpdateMoney(MoneyManager.Instance.Money);
        }
        else
        {
            subscribeCoroutine = StartCoroutine(TrySubscribe());
        }
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoney;

        if (subscribeCoroutine != null)
        {
            StopCoroutine(subscribeCoroutine);
            subscribeCoroutine = null;
        }
    }

    private IEnumerator TrySubscribe()
    {
        // Wait a few frames for MoneyManager to initialize
        int attempts = 0;
        while (MoneyManager.Instance == null && attempts < 60)
        {
            attempts++;
            yield return null;
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += UpdateMoney;
            UpdateMoney(MoneyManager.Instance.Money);
        }

        subscribeCoroutine = null;
    }

    private void Update()
    {
        // Throttle pending updates
        if (hasPending && (Time.unscaledTime - lastUpdateTime >= throttleInterval))
        {
            ApplyPending();
        }
    }

    private void UpdateMoney(int newAmount)
    {
        // Receive update from MoneyManager; defer actual UI set to throttle
        pendingAmount = newAmount;
        hasPending = true;

        if (Time.unscaledTime - lastUpdateTime >= throttleInterval)
        {
            ApplyPending();
        }
    }

    private void ApplyPending()
    {
        if (!hasPending) return;
        if (moneyText != null)
            moneyText.text = pendingAmount.ToString("N0");

        lastUpdateTime = Time.unscaledTime;
        hasPending = false;
    }
}
