using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MoneyStackManager : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Transform stackOrigin; // Stack başlangıç noktası (Inspector'dan ayarlanacak)
    [SerializeField] private GameObject moneyPrefab; // Money prefab
    [SerializeField] private float moneySpacing = 0.5f; // Money objeleri arası mesafe
    [SerializeField] private float rowHeight = 0.2f; // Her sıra yüksekliği (Y offset)
    [SerializeField] private float collectScale = 4f;

    [Header("Grid Pattern (4x2)")]
    [SerializeField] private int gridColumns = 4; // X ekseni (4 tane)
    [SerializeField] private int gridRows = 2; // Z ekseni (2 tane)

    [Header("Animation Settings")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.6f;

    [Header("Money Collection Settings")]
    [SerializeField] private int moneyValuePerPrefab = 10; // Her money prefab kaç para değerinde
    [SerializeField] private float collectRadius = 2f; // Player bu mesafede olunca toplama başlasın

    private List<GameObject> stackedMoneys = new List<GameObject>();
    private int currentLayer = 0; // Kaçıncı katta olduğumuzu takip eder
    private bool isCollecting = false; // Toplama işlemi devam ediyor mu


    public void SpawnAndStackMoney(Vector3 spawnPosition, bool isFromXray = false)
    {
        if (moneyPrefab == null)
        {
            Debug.LogError("[MoneyStackManager] Money prefab is not assigned!");
            return;
        }

        if (stackOrigin == null)
        {
            Debug.LogError("[MoneyStackManager] Stack origin is not assigned!");
            return;
        }

        // Pool'dan money al, null dönerse direkt instantiate et
        GameObject money = null;
        if (MoneyPool.Instance != null)
        {
            money = MoneyPool.Instance.Get(moneyPrefab);
        }

        // Pool null döndüyse veya pool yoksa, direkt instantiate et
        if (money == null)
        {
            Log($"⚠️ Pool returned null or unavailable - Creating new money instance");
            money = Instantiate(moneyPrefab);
        }

        if (money == null)
        {
            Debug.LogError("[MoneyStackManager] Failed to create money! Prefab might be null.");
            return;
        }

        // Başlangıç pozisyonunu ayarla
        money.transform.position = spawnPosition;
        money.transform.localScale = Vector3.one * 0.5f; // Küçük başlasın
        money.transform.rotation = Quaternion.Euler(0f, 65f, 0f); // Y ekseninde 65 derece

        // Stack pozisyonunu hesapla
        Vector3 targetStackPosition = CalculateNextStackPosition();

        // XRay ise scale 3x olacak
        Vector3 finalScale = isFromXray ? Vector3.one * collectScale : Vector3.one;

        // Jump animasyonu ile stack pozisyonuna git
        Sequence seq = DOTween.Sequence();

        seq.Append(money.transform.DOJump(targetStackPosition, jumpHeight, 1, jumpDuration)
            .SetEase(Ease.OutQuad));

        seq.Join(money.transform.DOScale(finalScale, jumpDuration * 0.5f)
            .SetEase(Ease.OutBack));

        seq.OnComplete(() =>
        {
            Log($"💰 Money stacked at position: {targetStackPosition} | Total: {stackedMoneys.Count}");
        });

        // Stack'e ekle
        stackedMoneys.Add(money);
    }


    private Vector3 CalculateNextStackPosition()
    {
        int totalInLayer = gridColumns * gridRows; // 4x2 = 8 para per layer
        int indexInLayer = stackedMoneys.Count % totalInLayer;

        // Her 8 parada bir üst kata çık
        if (stackedMoneys.Count > 0 && indexInLayer == 0)
        {
            currentLayer++;
            Log($"📈 Moving to layer {currentLayer}");
        }

        // 2x2 blok pattern: 0:(0,0) → 1:(X,0) → 2:(0,Z) → 3:(X,Z) tekrar eder
        // indexInLayer % 4 ile 2x2 blok içindeki pozisyonu buluruz
        // indexInLayer / 4 ile kaçıncı 2x2 blokta olduğumuzu buluruz

        int blockIndex = indexInLayer / 4; // 0 veya 1 (2 blok var)
        int posInBlock = indexInLayer % 4; // 0, 1, 2, 3 (2x2 blok içinde)

        // Her 2x2 blok için base offset
        int blockXOffset = (blockIndex == 1) ? 2 : 0; // 2. blok ise +2X offset
        int blockZOffset = 0; // Z ekseninde offset yok (tüm bloklar aynı Z hizasında)

        // Blok içindeki pozisyon (90 derece döndürülmüş)
        int xInBlock = (posInBlock == 2 || posInBlock == 3) ? 1 : 0; // Üst sıra için +1
        int zInBlock = (posInBlock == 1 || posInBlock == 3) ? 1 : 0; // Sağ sütun için +1

        // Toplam Z ve X (swap edildi - 90 derece dönme)
        int zMultiplier = blockXOffset + xInBlock; // Z ekseni (eskiden X'ti)
        int xMultiplier = blockZOffset + zInBlock; // X ekseni (eskiden Z'ydi)

        // Dünya pozisyonunu hesapla
        Vector3 position = stackOrigin.position;
        position.x += xMultiplier * moneySpacing; // X ekseni
        position.z += zMultiplier * moneySpacing; // Z ekseni
        position.y += currentLayer * rowHeight; // Kat offset (Y ekseni)

        return position;
    }

    public void ClearStack()
    {
        foreach (var money in stackedMoneys)
        {
            if (money != null)
            {
                if (MoneyPool.Instance != null)
                    MoneyPool.Instance.Return(moneyPrefab, money);
                else
                    Destroy(money);
            }
        }

        stackedMoneys.Clear();
        currentLayer = 0;
        Log("🧹 Stack cleared");
    }


    public int GetStackCount()
    {
        return stackedMoneys.Count;
    }

    private void Update()
    {
        // Player stack'e yakınsa para toplama başlat
        if (stackedMoneys.Count > 0 && !isCollecting)
        {
            Transform playerTransform = GetPlayerTransform();
            if (playerTransform != null && stackOrigin != null)
            {
                float distance = Vector3.Distance(playerTransform.position, stackOrigin.position);
                if (distance <= collectRadius)
                {
                    StartCoroutine(CollectMoneyToPlayer(playerTransform));
                }
            }
        }
    }

    private Transform GetPlayerTransform()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    private IEnumerator CollectMoneyToPlayer(Transform playerTransform)
    {
        isCollecting = true;
        Log($"💰 Started collecting {stackedMoneys.Count} moneys to player");

        // Tüm money'leri player'a doğru animasyonla gönder
        int totalMoneyValue = stackedMoneys.Count * moneyValuePerPrefab;

        // UI'daki money pozisyonunu bul (kameranın sağ üst köşesi)
        Camera mainCam = Camera.main;
        Vector3 uiTargetPos = mainCam != null
            ? mainCam.ViewportToWorldPoint(new Vector3(0.9f, 0.9f, 10f))
            : playerTransform.position + Vector3.up * 2f;

        List<GameObject> moneysCopy = new List<GameObject>(stackedMoneys);
        stackedMoneys.Clear();

        foreach (var money in moneysCopy)
        {
            if (money != null)
            {
                // Money'yi UI'ya doğru animasyonla gönder
                Sequence seq = DOTween.Sequence();
                seq.Append(money.transform.DOMove(uiTargetPos, 0.5f).SetEase(Ease.InQuad));
                seq.Join(money.transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad));
                seq.OnComplete(() =>
                {
                    if (MoneyPool.Instance != null)
                        MoneyPool.Instance.Return(moneyPrefab, money);
                    else
                        Destroy(money);
                });
            }
            yield return new WaitForSeconds(0.05f); // Her para arasında kısa delay
        }

        // Para değerini player'a ekle
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.AddMoney(totalMoneyValue);
            Log($"💵 Added {totalMoneyValue} money to player ({moneysCopy.Count} x {moneyValuePerPrefab})");
        }

        // Layer'ı sıfırla
        currentLayer = 0;
        isCollecting = false;
    }

    private void Log(string msg)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[MoneyStackManager] {msg}");
    }

    // Debug görselleştirme
    private void OnDrawGizmos()
    {
        if (stackOrigin == null) return;

        Gizmos.color = Color.yellow;

        // İlk layer grid'ini çiz (2x2 blok pattern - 90 derece döndürülmüş)
        // Blok 0: 0:(0,0) 1:(1,0) 2:(0,1) 3:(1,1)
        // Blok 1: 4:(0,2) 5:(1,2) 6:(0,3) 7:(1,3)
        int[] xMults = { 0, 1, 0, 1, 0, 1, 0, 1 };
        int[] zMults = { 0, 0, 1, 1, 2, 2, 3, 3 };

        for (int i = 0; i < 8; i++)
        {
            Vector3 pos = stackOrigin.position;
            pos.x += xMults[i] * moneySpacing;
            pos.z += zMults[i] * moneySpacing;
            Gizmos.DrawWireSphere(pos, 0.1f);

            // İndex numarasını göster (optional - Unity Editor'da görünür)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, i.ToString());
            #endif
        }
    }
}
