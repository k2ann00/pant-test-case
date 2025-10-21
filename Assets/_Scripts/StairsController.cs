using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StairsController : MonoBehaviour
{

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.3f; // her geçiş süresi
    [SerializeField] private float delayBetweenCycles = 0f; // cycle arası bekleme
    [SerializeField] private Ease moveEase = Ease.Linear;
    [SerializeField] private bool autoStart = true;

    [Header("References")]
    [SerializeField] public List<Transform> steps = new List<Transform>();
    [SerializeField] private bool autoFindSteps = true; // Inspector'da child'ları otomatik bul
    private bool isRunning;
    private Vector3[] originalPositions; // Basamakların orijinal pozisyonları

    private void Awake()
    {
        // Eğer steps listesi boşsa ve autoFind açıksa, child'ları bul
        if (autoFindSteps && (steps == null || steps.Count == 0))
        {
            FindStepsInChildren();
        }
    }

    [ContextMenu("Find Steps in Children")]
    private void FindStepsInChildren()
    {
        steps = new List<Transform>();

        // Tüm child'ları al (sadece direkt child'lar, iç içe değil)
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Eğer ismi "Step" veya "Stairs" içeriyorsa ekle
            if (child.name.ToLower().Contains("step") || child.name.ToLower().Contains("stair"))
            {
                steps.Add(child);
            }
        }

        Debug.Log($"[Stairs] Found {steps.Count} steps in {gameObject.name}");

        if (steps.Count > 0)
        {
            Debug.Log($"[Stairs] First: {steps[0].name}, Last: {steps[steps.Count - 1].name}");
        }
    }

    private void OnEnable()
    {
        // Event'e abone ol
        EventBus.StairsUnlocked += OnStairsUnlocked;
    }

    private void OnDisable()
    {
        // Event aboneliğini iptal et
        EventBus.StairsUnlocked -= OnStairsUnlocked;

        // Animasyonu durdur
        if (isRunning)
        {
            StopMoving();
        }
    }

    /// <summary>
    /// Merdiven unlock olduğunda çağrılır (scale animasyonu bittikten sonra)
    /// </summary>
    private void OnStairsUnlocked(GameObject unlockedObject)
    {
        StartMoving();
    }

    [ContextMenu("Start Moving")]
    public void StartMoving()
    {
        if (isRunning || steps.Count < 2)
            return;


        // Orijinal pozisyonları kaydet (scale = 1.0 olduğunda)
        if (originalPositions == null || originalPositions.Length != steps.Count)
        {
            originalPositions = new Vector3[steps.Count];
            for (int i = 0; i < steps.Count; i++)
            {
                originalPositions[i] = steps[i].position;
            }
            Debug.Log($"[Stairs] Saved {originalPositions.Length} original positions");
        }

        isRunning = true;
        StartCoroutine(CycleRoutine());
    }

    private IEnumerator CycleRoutine()
    {
        while (isRunning)
        {
            MoveAllStepsOnce();

            // 🔹 Hareket bitmesini bekle
            yield return new WaitForSeconds(moveDuration + delayBetweenCycles);

            // 🔹 Yeni pozisyonları kaydet
            for (int i = 0; i < steps.Count; i++)
            {
                originalPositions[i] = steps[i].position;
            }
        }
    }


    private void MoveAllStepsOnce()
    {
        if (steps == null || steps.Count == 0 || originalPositions == null)
            return;

        // Her basamağı bir sonraki pozisyona hareket ettir
        for (int i = 0; i < steps.Count; i++)
        {
            int nextIndex = (i + 1) % steps.Count;
            Vector3 targetPosition = originalPositions[nextIndex];

            // Unique ID oluştur
            string uniqueID = $"{gameObject.GetInstanceID()}_Step_{i}";

            // Önce eski tween'i temizle
            steps[i].DOKill();

            // Yeni hareketi başlat
            steps[i].DOMove(targetPosition, moveDuration)
                .SetEase(moveEase)
                .SetId(uniqueID);
        }
    }




    [ContextMenu("Stop Moving")]
    public void StopMoving()
    {
        isRunning = false;
        StopAllCoroutines();

        // Sadece bu controller'ın step'lerini durdur
        for (int i = 0; i < steps.Count; i++)
        {
            steps[i].DOKill();
        }
    }
}
