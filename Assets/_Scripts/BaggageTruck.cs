using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaggageTruck : MonoBehaviour
{
    public static BaggageTruck Instance { get; private set; }

    [Header("Movement Settings")]
    [SerializeField] private Transform startPosition; // Truck'ın başlangıç pozisyonu (geri dönüş için)
    [SerializeField] private Transform targetPosition; // Truck'ın gideceği hedef nokta
    [SerializeField] private float moveDuration = 2f;
    [SerializeField] private float waitAtDestination = 1f; // Hedefe ulaştıktan sonra bekleme süresi
    [SerializeField] private float returnDuration = 2f; // Geri dönüş süresi

    [Header("Baggage Container")]
    [SerializeField] private Transform baggageContainer; // Bagajların parent olacağı transform (truck kasası)

    private List<GameObject> loadedBaggages = new List<GameObject>();
    private bool hasMovedToTarget = false;
    private Tween activeTween;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"Multiple BaggageTruck instances detected! Destroying {name}");
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        // Tüm bagajlar yüklendiğinde hareket et
        EventBus.AllBaggagesLoadedToTruck += OnAllBaggagesLoaded;
        EventBus.BaggageReachedTruck += OnBaggageReachedTruck;
    }

    private void OnDisable()
    {
        EventBus.AllBaggagesLoadedToTruck -= OnAllBaggagesLoaded;
        EventBus.BaggageReachedTruck -= OnBaggageReachedTruck;
    }

    private void OnBaggageReachedTruck(GameObject baggage)
    {
        if (baggage == null) return;

        loadedBaggages.Add(baggage);

        // Bagajı truck kasasına parent yap (opsiyonel - eğer truck hareket edecekse gerekli)
        if (baggageContainer != null)
        {
            baggage.transform.SetParent(baggageContainer);
        }

        Debug.Log($"  [BaggageTruck] {baggage.name} loaded. Total: {loadedBaggages.Count}");
    }


    private void OnAllBaggagesLoaded()
    {
        if (hasMovedToTarget)
        {
            Debug.LogWarning("[BaggageTruck] Truck has already moved!");
            return;
        }

        if (targetPosition == null)
        {
            Debug.LogError("[BaggageTruck] Target position is not assigned!");
            return;
        }

        Debug.Log($"[BaggageTruck] All baggages loaded ({loadedBaggages.Count}). Moving to target...");
        StartCoroutine(MoveToTarget());
    }


    private IEnumerator MoveToTarget()
    {
        yield return new WaitForSeconds(1.5f);
        hasMovedToTarget = true;

        // DOTween ile hareket
        activeTween = transform.DOMove(targetPosition.position, moveDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Debug.Log(" [BaggageTruck] Truck reached destination!");
                EventBus.RaiseTruckReachedDestination();

                // Hedefe ulaştıktan sonra bagajları yok et ve geri dön
                StartCoroutine(UnloadAndReturn());
            });
    }


    private IEnumerator UnloadAndReturn()
    {
        // Hedefe ulaştıktan sonra biraz bekle
        yield return new WaitForSeconds(waitAtDestination);

        Debug.Log($"  [BaggageTruck] Unloading {loadedBaggages.Count} baggages...");

        // Bagajları pooling ile yok et
        foreach (GameObject baggage in loadedBaggages)
        {
            if (baggage != null)
            {
                // Pooling sistemini kullan
                ObjectPool.Instance.ReturnToPool(baggage);
            }
        }

        // Listeyi temizle
        loadedBaggages.Clear();
        Debug.Log(" [BaggageTruck] All baggages unloaded!");

        // 180 derece dön (Y ekseninde)
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[BaggageTruck] Rotating 180 degrees...");

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 180, 0);
        activeTween = transform.DORotateQuaternion(targetRotation, 1f)
            .SetEase(Ease.InOutQuad);

        yield return activeTween.WaitForCompletion();
        Debug.Log(" [BaggageTruck] Rotation completed!");

        // Truck'ı başlangıç pozisyonuna geri getir
        yield return new WaitForSeconds(0.3f);

        if (startPosition != null)
        {
            Debug.Log("[BaggageTruck] Returning to start position...");

            activeTween = transform.DOMove(startPosition.position, returnDuration)
                .SetEase(Ease.InOutQuad)
                .OnComplete(() =>
                {
                    Debug.Log(" [BaggageTruck] Truck returned to start position!");

                    // Start pozisyonuna gelince tekrar 180 derece dön (original rotasyona dön)
                    StartCoroutine(RotateAtStartPosition());
                });
        }
        else
        {
            Debug.LogWarning("[BaggageTruck] Start position is not assigned!");
            hasMovedToTarget = false;
        }
    }

    private IEnumerator RotateAtStartPosition()
    {
        yield return new WaitForSeconds(0.3f);
        Debug.Log("[BaggageTruck] Rotating 180 degrees back to original rotation...");

        Quaternion targetRotation = transform.rotation * Quaternion.Euler(0, 180, 0);
        activeTween = transform.DORotateQuaternion(targetRotation, 1f)
            .SetEase(Ease.InOutQuad);

        yield return activeTween.WaitForCompletion();
        Debug.Log(" [BaggageTruck] Rotation completed! Ready for next cycle.");

        hasMovedToTarget = false; // Bir sonraki cycle için reset
    }


    public void Stop()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }

        Debug.Log("[BaggageTruck] Truck stopped");
    }


    public void Reset()
    {
        Stop();
        loadedBaggages.Clear();
        hasMovedToTarget = false;

        Debug.Log("[BaggageTruck] Truck reset");
    }

    private void OnDestroy()
    {
        // Cleanup
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }


    private void Start()
    {
        if (startPosition == null)
            Debug.LogWarning("[BaggageTruck] Start position is not assigned!");

        if (targetPosition == null)
            Debug.LogWarning("[BaggageTruck] Target position is not assigned!");

        if (baggageContainer == null)
            Debug.LogWarning("[BaggageTruck] Baggage container is not assigned!");

        if (moveDuration <= 0)
        {
            Debug.LogWarning("[BaggageTruck] Move duration must be greater than 0!");
            moveDuration = 2f;
        }

        if (returnDuration <= 0)
        {
            Debug.LogWarning("[BaggageTruck] Return duration must be greater than 0!");
            returnDuration = 2f;
        }
    }


    private void OnDrawGizmos()
    {
        // Başlangıç pozisyonu (mavi küre)
        if (startPosition != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(startPosition.position, 0.5f);
            UnityEngine.GUI.color = Color.blue;
        }

        // Hedef nokta (yeşil küre)
        if (targetPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetPosition.position, 0.5f);

            // Hareket yolu (sarı çizgi)
            if (startPosition != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPosition.position, targetPosition.position);
            }
        }

        // Truck mevcut pozisyon (kırmızı küp)
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
    }
}
