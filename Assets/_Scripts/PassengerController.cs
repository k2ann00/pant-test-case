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
    ToExit
}


public class PassengerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] public Rigidbody rb;
    [SerializeField] private TextMeshPro stateText;
    [SerializeField] private Transform firstStep; // Inspector'dan atanacak
    [SerializeField] private Transform lastStep;  // Inspector'dan atanacak
    [SerializeField] private bool IsPassengerReachedTopEsc = false;

    [Header("Baggage Settings")]
    [SerializeField] private GameObject myBaggage;
    [SerializeField] private bool HasBaggage = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    private Tween activeTween;
    private List<Vector3> currentPath = new List<Vector3>();

    public int QueueIndex { get; set; }
    public int PermanentOrder { get; private set; }
    public PassengerState currentState;
    private Coroutine climbCoroutine;

    public PassengerState CurrentState => currentState;

    private void OnEnable()
    {
        InitializeBaggage();

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
        }
        else
        {
            Debug.LogWarning($"{name}: 'Baggage' child bulunamadı. Child adı farklı olabilir!");
            HasBaggage = false;
            myBaggage = null;
        }
    }
    public void StartWalkingPathGeneric(List<Vector3> path, PassengerPathType pathType)
    {
        if (path == null || path.Count == 0)
        {
            Debug.LogWarning($"⚠️ [{name}] Path is empty for {pathType}");
            return;
        }

        // ✅ Önceki tween'leri temizle (kuyruk pozisyonu çakışmasını önle)
        DOTween.Kill(transform);

        Debug.Log($"🚶 [{name}] Starting path: {pathType} | Points: {path.Count} | Current State: {currentState}");

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
                rb.isKinematic = false;
                Debug.Log($"✅ [{name}] Path complete: {pathType}");
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
                EventBus.RaisePassengerReachedXRayEnd(this);
                break;

            case PassengerPathType.ToUpperQueue:
                currentState = PassengerState.Waiting;
                animator.SetBool("IsMoving", false);
                EventBus.RaisePassengerStateChanged(this);
                EventBus.RaisePassengerReachedUpperQueue(this);
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
        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        transform
            .DOMove(frontPos, 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Waiting;
                EventBus.RaisePassengerReachedFront(this);
            });
    }


    public void StartHandingBaggage()
    {
        if (!HasBaggage || myBaggage == null) return;

        currentState = PassengerState.HandingBaggage;
        EventBus.RaisePassengerStateChanged(this);
        animator.SetTrigger("HandBaggageTrigger");

        var holder = FindObjectOfType<PlayerBaggageHolder>();
        if (holder == null) return;
        var target = holder.baggageStackRoot;

        myBaggage.transform
            .DOJump(target.position, 1f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                HasBaggage = false;
                holder.AddBaggage(myBaggage);
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

    private IEnumerator ClimbRoutine()
    {
        currentState = PassengerState.Climbing;
        animator.SetBool("IsMoving", false); // Yürüyen merdivende yürüme animasyonu olmamalı
        rb.isKinematic = true; // Basamaklarla çarpışmaması için kinematic

        Vector3 startPos = transform.position;
        Vector3 endPos = lastStep.position;
        float climbSpeed = 2f;

        Debug.Log($"🧗 [{name}] CLIMB START | From: {startPos} → To: {endPos} | Distance: {Vector3.Distance(startPos, endPos)}");

        float timer = 0f;
        while (!IsPassengerReachedTopEsc)
        {
            // Merdiven boyunca hareket - rb.MovePosition kullan
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * climbSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            // Her 0.5 saniyede bir log
            timer += Time.fixedDeltaTime;
            if (timer >= 0.5f)
            {
                Debug.Log($"🧗 [{name}] CLIMBING | Current: {rb.position} | Target: {endPos} | Distance: {Vector3.Distance(rb.position, endPos)}");
                timer = 0f;
            }

            // Hedef çizgisi (Kırmızı: başlangıç → hedef, Yeşil: mevcut pozisyon → hedef)
            Debug.DrawLine(startPos, endPos, Color.red, 0.1f);
            Debug.DrawLine(rb.position, endPos, Color.green, 0.1f);
            Debug.DrawRay(rb.position, climbDir * 2f, Color.yellow, 0.1f);

            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"🧗 [{name}] REACHED TOP ESC TRIGGER | Moving to exact position...");

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

        Debug.Log($"✅ [{name}] CLIMB COMPLETE | Final Position: {rb.position}");
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
    //    yield return new WaitForFixedUpdate(); // ✅ Physics senkronu bekle

    //    rb.isKinematic = true;
    //    currentState = PassengerState.WalkingToTarget;
    //    animator.SetBool("IsMoving", false);
    //    EventBus.RaisePassengerStateChanged(this);

    //    // ✅ Event'i bir sonraki frame'de gönder
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

    public void UpdateStateText()
    {
        if (stateText != null)
            stateText.text = currentState.ToString();
    }

    


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EscalatorEndPoint"))
        {
            Debug.Log($"{name} reached escalator top");
            IsPassengerReachedTopEsc = true;
        }
    }
}
