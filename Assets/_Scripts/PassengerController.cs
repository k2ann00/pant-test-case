using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core.PathCore;
using DG.Tweening.Plugins.Options;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

public enum PassengerState
{
    Waiting,
    HandingBaggage,
    WalkingToTarget,
    Climbing,
    Done
}

public enum PassengerPathType
{
    ToStairs,
    ToXRay,
    ToUpperQueue,
    ToExit,
    ToInspectionPoint // Yeni: XRay inspection point'e gitmek için
}


public class PassengerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] public Animator animator;
    [SerializeField] public Rigidbody rb;
    [SerializeField] private Transform firstStep; // Inspector'dan atanacak
    [SerializeField] private Transform lastStep;  // Inspector'dan atanacak
    [SerializeField] private bool IsPassengerReachedTopEscalator = false;

    [Header("Baggage Settings")]
    [SerializeField] private GameObject myBaggage;
    [SerializeField] private bool HasBaggage = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    private Tween activeTween;
    private List<Vector3> currentPath = new List<Vector3>();

    [Header("Appearance Settings")]
    private static readonly Color[] SkinColors = new Color[]
    {
        new Color(1f, 0.87f, 0.74f),      // Light skin
        new Color(0.96f, 0.76f, 0.57f),   // Medium light skin
        new Color(0.72f, 0.53f, 0.38f),   // Medium skin
        new Color(0.47f, 0.32f, 0.24f)    // Dark skin
    };

    public int QueueIndex { get; set; }
    public int PermanentOrder { get; private set; }
    public PassengerState currentState;
    private Coroutine climbCoroutine;

    public PassengerState CurrentState => currentState;

    private void OnEnable()
    {
        InitializeBaggage();
        RandomizeAppearance();
    }
    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        currentState = PassengerState.Waiting;
        PermanentOrder = QueueIndex;
    }


    private void InitializeBaggage()
    {
        Transform baggage = transform.Find("Baggage");
        if (baggage != null)
        {
            myBaggage = baggage.gameObject;
            HasBaggage = true;
            RandomizeBaggageAppearance();
        }
        else
        {
            Debug.LogWarning($"{name}: 'Baggage' child bulunamadı. Child adı farklı olabilir!");
            HasBaggage = false;
            myBaggage = null;
        }
    }

    private void RandomizeBaggageAppearance()
    {
        if (myBaggage == null) return;

        // Find MeshRenderer in baggage object and its children
        MeshRenderer[] renderers = myBaggage.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer.materials.Length > 0)
            {
                Material[] materials = renderer.materials;

                // Randomize all materials with random colors
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        Color randomColor = new Color(
                            Random.Range(0f, 1f),
                            Random.Range(0f, 1f),
                            Random.Range(0f, 1f),
                            1f
                        );
                        materials[i].color = randomColor;
                    }
                }

                // Apply modified materials back to renderer
                renderer.materials = materials;
            }
        }
    }

    private void RandomizeAppearance()
    {
        // Find SkinnedMeshRenderer in child objects
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();

        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            if (renderer.materials.Length >= 3)
            {
                Material[] materials = renderer.materials;

                // Materials[0] - SkinColor: Random from predefined skin colors
                if (materials[0] != null)
                {
                    Color randomSkinColor = SkinColors[Random.Range(0, SkinColors.Length)];
                    materials[0].color = randomSkinColor;
                }

                // Materials[1] - AtlasTexture: Random color
                if (materials[1] != null)
                {
                    Color randomAtlasColor = new Color(
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        1f
                    );
                    materials[1].color = randomAtlasColor;
                }

                // Materials[2] - AtlasPant: Random color
                if (materials[2] != null)
                {
                    Color randomPantColor = new Color(
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        Random.Range(0f, 1f),
                        1f
                    );
                    materials[2].color = randomPantColor;
                }

                // Apply modified materials back to renderer
                renderer.materials = materials;
            }
        }
    }
    public void StartWalkingPathGeneric(List<Vector3> path, PassengerPathType pathType)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"[{name}] Path is empty for {pathType}");
            return;
        }

        //  Önceki tween'leri temizle (kuyruk pozisyonu çakışmasını önle)
        DOTween.Kill(transform);

        Log($"🚶 [{name}] Starting path: {pathType} | Points: {path.Count} | Current State: {currentState}");

        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        float duration = CalculateDuration(path);
        activeTween = transform
            .DOPath(path.ToArray(), duration, PathType.Linear)
            .SetEase(Ease.Linear)
            .SetLookAt(0)
            .OnUpdate(() =>
            {
                Vector3 e = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, e.y, 0f);
            })
            .OnComplete(() =>
            {
                //animator.SetBool("IsMoving", false);
                // rb.isKinematic = false; // KALDIRILDI - fizik çarpışmasını önlemek için
                Log($" [{name}] Path complete: {pathType}");
                HandlePathComplete(pathType);
            });
    }

    private void HandlePathComplete(PassengerPathType pathType)
    {
        switch (pathType)
        {
            case PassengerPathType.ToStairs:
                EventBus.RaisePassengerReachedStairs(this);
                break;

            case PassengerPathType.ToXRay:
                // XRay path'i bitti - upper queue'ya gitmek için PassengerManager'a bildir
                animator.SetBool("IsMoving", false);
                rb.isKinematic = true; // Fizik çarpışmasını önle
                currentState = PassengerState.Waiting;
                EventBus.RaisePassengerReachedXRayEnd(this); // Upper queue'ya gitmek için gerekli
                break;

            case PassengerPathType.ToUpperQueue:
                currentState = PassengerState.Waiting;
                animator.SetBool("IsMoving", false);
                rb.isKinematic = true;
                EventBus.RaisePassengerStateChanged(this);
                EventBus.RaisePassengerReachedUpperQueue(this);
                break;

            case PassengerPathType.ToInspectionPoint:
                // Inspection point'e ulaştı - event tetikleme (PassengerXrayManager kendi yönetiyor)
                animator.SetBool("IsMoving", false);
                rb.isKinematic = true;
                currentState = PassengerState.Waiting;
                // Event yok - PassengerXrayManager zaten InspectionRoutine içinde bekliyor
                break;

            case PassengerPathType.ToExit:
            default:
                EventBus.RaisePassengerReachedTarget(this);
                break;
        }
    }


    public void SetPermanentOrder(int order)
    {
        PermanentOrder = order;
    }

    public void MoveToFront(Vector3 frontPos)
    {
        DOTween.Kill(transform);

        // Hareket mesafesini kontrol et
        float distance = Vector3.Distance(transform.position, frontPos);

        // Eğer mesafe çok kısa ise animasyon ve hareket başlatma, direkt event tetikle
        if (distance < 0.1f)
        {
            Log($"[{name}] Already at front position (distance: {distance}). Skipping movement.");
            // Direkt event'i tetikle
            EventBus.RaisePassengerReachedFront(this);
            return;
        }

        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        // Animasyonu hemen başlat
        animator.SetBool("IsMoving", true);

        transform
            .DOMove(frontPos, 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                // State'i Waiting'e çevirme! StartHandingBaggage zaten state'i değiştirecek
                // currentState = PassengerState.Waiting;
                EventBus.RaisePassengerReachedFront(this);
            });
    }


    public void StartHandingBaggage()
    {
        if (!HasBaggage || myBaggage == null)
        {
            // Bagaj yoksa Waiting state'ine geç
            currentState = PassengerState.Waiting;
            EventBus.RaisePassengerStateChanged(this);
            return;
        }

        currentState = PassengerState.HandingBaggage;
        EventBus.RaisePassengerStateChanged(this);
        animator.SetTrigger("HandBaggageTrigger");

        var holder = FindObjectOfType<PlayerBaggageHolder>();
        if (holder == null)
        {
            // Holder yoksa Waiting state'ine geç
            currentState = PassengerState.Waiting;
            EventBus.RaisePassengerStateChanged(this);
            return;
        }

        // Direkt stack pozisyonuna zıpla (en üstteki bagajın üzerine)
        Vector3 targetStackPos = holder.GetNextStackPosition();

        // Closure için local kopyalar oluştur
        var baggageToHand = myBaggage;
        var baggageHolder = holder;

        baggageToHand.transform
            .DOJump(targetStackPos, 1f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Null kontrolü
                if (baggageToHand != null && baggageHolder != null)
                {
                    baggageHolder.AddBaggage(baggageToHand);
                }

                HasBaggage = false;
                myBaggage = null;
                animator.SetBool("HasBaggage", false);
                StartCoroutine(WaitUntillBaggageReach());
            });
    }
    
    private IEnumerator WaitUntillBaggageReach()
    {
        yield return new WaitForSeconds(1f);
        EventBus.RaisePassengerHandedBaggage(this);
    }

    public void StartWalkingPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0) return;

        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        float duration = CalculateDuration(path);
        activeTween = transform
            .DOPath(path.ToArray(), duration, PathType.CatmullRom) //PathType.CatmullRom
            .SetEase(Ease.Linear)
            .SetLookAt(lookAhead: 0)
            .OnUpdate(() =>
            {
                Vector3 e = transform.eulerAngles;
                transform.rotation = Quaternion.Euler(0f, e.y, 0f);

            })
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                // 🧩 Eğer path merdiven tabanında bitiyorsa tırmanma başlat
                if (EventBus.HasStairs)
                    EventBus.RaisePassengerReachedStairs(this);
                else
                    EventBus.RaisePassengerReachedTarget(this);

            });
    }

    public void StartClimbingRoutine()
    {
        currentState = PassengerState.Climbing;
        EventBus.RaisePassengerStateChanged(this);
        animator.SetBool("IsMoving", false); // Yürüyen merdiven karakteri taşıyor, yürüme animasyonu olmamalı

        rb.isKinematic = true; // Basamaklarla çarpışmaması için kinematic (ClimbRoutine içinde rb.MovePosition kullanılıyor)
        StartCoroutine(ClimbRoutine());
    }


    //private IEnumerator ClimbRoutine()
    //{
    //    Vector3 endPos = lastStep.position;
    //    float climbSpeed = 2f;

    //    Debug.Log($"{name} starting climb from {transform.position} to {endPos} | Distance: {Vector3.Distance(transform.position, endPos)}");

    //    while (Vector3.Distance(transform.position, endPos) > 0.05f)
    //    {
    //        Vector3 climbDir = (endPos - transform.position).normalized; // 🔹 her frame yönü yeniden hesapla
    //        rb.MovePosition(transform.position + climbDir * climbSpeed * Time.fixedDeltaTime);
    //        yield return new WaitForFixedUpdate();
    //    }

    //    transform.position = endPos;
    //    rb.isKinematic = true;
    //    animator.SetBool("IsMoving", false);
    //    currentState = PassengerState.WalkingToTarget;
    //    EventBus.RaisePassengerStateChanged(this);
    //    EventBus.RaisePassengerReachedTopStairs(this);
    //}
    public Vector3 GetLastStepPosition()
    {
        return lastStep.position;
    }
    private IEnumerator ClimbRoutine()
    {
        currentState = PassengerState.Climbing;
        animator.SetBool("IsMoving", false); // Yürüyen merdivende yürüme animasyonu olmamalı
        rb.isKinematic = true; // Basamaklarla çarpışmaması için kinematic

        Vector3 startPos = transform.position;
        Vector3 endPos = lastStep.position;
        float climbSpeed = 2f;

        Log($"🧗 [{name}] CLIMB START | From: {startPos} → To: {endPos} | Distance: {Vector3.Distance(startPos, endPos)}");

        float timer = 0f;
        while (!IsPassengerReachedTopEscalator)
        {
            // Merdiven boyunca hareket - rb.MovePosition kullan
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * climbSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            // Her 0.5 saniyede bir log
            timer += Time.fixedDeltaTime;
            if (timer >= 0.5f)
            {
                Log($"🧗 [{name}] CLIMBING | Current: {rb.position} | Target: {endPos} | Distance: {Vector3.Distance(rb.position, endPos)}");
                timer = 0f;
            }

            // Hedef çizgisi (Kırmızı: başlangıç → hedef, Yeşil: mevcut pozisyon → hedef)
            Debug.DrawLine(startPos, endPos, Color.red, 0.1f);
            Debug.DrawLine(rb.position, endPos, Color.green, 0.1f);
            Debug.DrawRay(rb.position, climbDir * 2f, Color.yellow, 0.1f);

            yield return new WaitForFixedUpdate();
        }

        Log($"🧗 [{name}] REACHED TOP ESC TRIGGER | Moving to exact position...");

        // Trigger geldiğinde yavaşça son pozisyona yerleş
        while (Vector3.Distance(rb.position, endPos) > 0.05f)
        {
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * (climbSpeed / 2f) * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            Debug.DrawLine(rb.position, endPos, Color.blue, 0.1f);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPos);
        rb.isKinematic = true;
        animator.SetBool("IsMoving", false);
        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        EventBus.RaisePassengerReachedTopStairs(this);

        climbCoroutine = null;
    }

    //    private IEnumerator ClimbRoutine()
    //{
    //    Vector3 endPos = lastStep.position;
    //    float climbSpeed = 2f;

    //    Debug.Log($"{name} starting climb from {transform.position} to {endPos} | Distance: {Vector3.Distance(transform.position, endPos)}");

    //    while (Vector3.Distance(transform.position, endPos) > 0.05f)
    //    {
    //        Vector3 climbDir = (endPos - transform.position).normalized;
    //        rb.MovePosition(transform.position + climbDir * climbSpeed * Time.fixedDeltaTime);
    //        yield return new WaitForFixedUpdate();
    //    }

    //    transform.position = endPos;
    //    yield return new WaitForFixedUpdate(); //  Physics senkronu bekle

    //    rb.isKinematic = true;
    //    currentState = PassengerState.WalkingToTarget;
    //    animator.SetBool("IsMoving", false);
    //    EventBus.RaisePassengerStateChanged(this);

    //    //  Event'i bir sonraki frame'de gönder
    //    yield return null;
    //    EventBus.RaisePassengerReachedTopStairs(this);
    //}


    public void WalkingToXRay(List<Vector3> path)
    {

    }

    private float CalculateDuration(List<Vector3> path)
    {
        float total = 0f;
        for (int i = 1; i < path.Count; i++)
            total += Vector3.Distance(path[i - 1], path[i]);
        return total / moveSpeed;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EscalatorEndPoint"))
        {
            Log($"{name} reached escalator top");
            IsPassengerReachedTopEscalator = true;
        }
    }

    public void StopAllMovement()
    {
        // Tween'leri durdur
        DOTween.Kill(transform);
        if (activeTween != null)
        {
            activeTween.Kill();
            activeTween = null;
        }

        // Coroutine'leri durdur
        if (climbCoroutine != null)
        {
            StopCoroutine(climbCoroutine);
            climbCoroutine = null;
        }
        StopAllCoroutines();

        // Animator'ü durdur
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.ResetTrigger("HandBaggageTrigger");
        }

        // Rigidbody'yi durdur
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // State'i waiting'e al
        currentState = PassengerState.Waiting;
        IsPassengerReachedTopEscalator = false;

        Log($"[{name}] All movement stopped");
    }

    private void Log(string msg)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[PassengerManager] {msg}");
    }
}
