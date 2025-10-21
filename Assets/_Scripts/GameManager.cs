using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Arrow Mark")]
    [SerializeField] private GameObject arrowMarkPrefab;
    private ArrowMarkController arrowMarkInstance;


    [Header("Debug Settings")]
    public bool IsGameInSlowMo = false;
    public bool IsGameInFastMo = false;
    public bool ShowDetailedLogs = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        if (arrowMarkPrefab != null)
        {
            MoneyManager.Instance.AddMoney(50);
            var obj = Instantiate(arrowMarkPrefab, Vector3.zero, Quaternion.identity);
            arrowMarkInstance = obj.GetComponent<ArrowMarkController>();
            obj.SetActive(false);
        }

        if (IsGameInSlowMo) Time.timeScale = 0.5f;
        if (IsGameInFastMo) Time.timeScale = 3.0f;
    }

    
    public void ShowArrow(Vector3 worldPosition)
    {
        if (arrowMarkInstance != null)
        {
            arrowMarkInstance.ShowAt(worldPosition);
        }
    }

    public void HideArrow()
    {
        if (arrowMarkInstance != null)
        {
            arrowMarkInstance.Hide();
        }
    }


    private IEnumerator ScaleCoroutine(Transform target, float duration)
    {
        if (target == null) yield break;

        Vector3 startScale = Vector3.zero;
        Vector3 midScale = Vector3.one * 1.2f;
        Vector3 endScale = Vector3.one;

        float halfDuration = duration / 2f;
        float elapsed = 0f;

        // 0 -> 1.2
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(startScale, midScale, t);
            yield return null;
        }

        elapsed = 0f;

        // 1.2 -> 1
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            target.localScale = Vector3.Lerp(midScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale; // kesin bitiþ
    }
}
