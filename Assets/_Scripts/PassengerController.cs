using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public enum PassengerState
{
    Waiting,
    HandingBaggage,
    WalkingToTarget,
    Done
}

public class PassengerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;


    [Header("Baggage Settings")]
    [SerializeField] private Transform baggageStackRoot;
    [SerializeField] private GameObject myBaggage;
    [SerializeField] private bool HasBaggage;

    [Header("State Settings")]
    [SerializeField] private PassengerState currentState;
    //WalkingToTarger
    [SerializeField] private Transform target;
    [SerializeField] private float customerSpeed = 5f;
    private bool hasArrivedAtQueueFront = false;
    public int QueueIndex;

    private Vector3 tempVec;
    private List<Vector3> currentPath = new List<Vector3>();
    private Tween activeTween;
    public TextMeshPro stateText;



    public PassengerState CurrentState => currentState;
    private void Start()
    {
        InitializeComponents();
        InitializeBaggage();
    }

    private void InitializeComponents()
    {
        animator = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody>();
        currentState = PassengerState.Waiting;
    }

    private void InitializeBaggage()
    {
        var baggage = transform.Find("Baggage");
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


    private void Update()
    {
        StateHandler();
        stateText.text = currentState.ToString();
    }

    private void StateHandler()
    {
        switch (currentState)
        {
            case PassengerState.Waiting:
                WaitingState();
                break;
            case PassengerState.HandingBaggage:
                HandingBaggageState();
                break;
            case PassengerState.WalkingToTarget:
                //WalkingToTargetState(target);
                break;
            case PassengerState.Done:
                break;
            default:
                break;
        }
    }

    private void WalkingToTargetState(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: Target atanmadı!");
            return;
        }

        // Hedefe uzaklığı hesapla
        float distance = Vector3.Distance(transform.position, target.position);

        // Eğer hedefe çok yaklaştıysa dur
        if (distance < 0.1f)
        {
            rb.velocity = Vector3.zero;
            animator.SetBool("IsMoving", false);
            currentState = PassengerState.Done;

            EventBus.RaisePassengerReachedTarget(this);
            Debug.Log($"{name} hedefe ulaştı!");
            return;
        }

        // Hedef yönünü hesapla
        Vector3 direction = (target.position - transform.position).normalized;

        // Yumuşak bir şekilde o yöne dön
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 10f * Time.deltaTime);

        // Hareket et
        rb.MovePosition(transform.position + direction * customerSpeed * Time.deltaTime);

        // Animasyon aktif
        animator.SetBool("IsMoving", true);
    }
    /*
    private void WaitingState()
    {
        animator.SetBool("HasBaggage", HasBaggage);
        animator.SetBool("IsMoving", false);

        if (QueueIndex == 0
            && PassengerManager.Instance.IsPlayerInRange
            && PassengerManager.Instance.CanProcessFrontPassenger())
        {
            PassengerManager.Instance.SetProcessingFrontPassenger(true); // 🔸 işlem başlıyor
            currentState = PassengerState.HandingBaggage;
        }
    }


    private void HandingBaggageState()
    {
        if (HasBaggage && myBaggage != null && PassengerManager.Instance.IsPlayerInRange)
        {
            HasBaggage = false; // tekrar çağrılmasını engeller
            animator.SetTrigger("HandBaggageTrigger");
            BaggageAnimSequence();
        }
    }
    */

    private void WaitingState()
    {
        animator.SetBool("HasBaggage", HasBaggage);
        animator.SetBool("IsMoving", false);

        // Bavul verme şartı artık ön sıraya fiziksel olarak gelmeyi de kontrol ediyor
        if (QueueIndex == 0
            && PassengerManager.Instance.IsPlayerInRange
            && PassengerManager.Instance.CanProcessFrontPassenger())
        {
            Log($"🟢 {name} (QueueIndex 0) and at front. Starting baggage sequence...");
            PassengerManager.Instance.SetProcessingFrontPassenger(true);
            currentState = PassengerState.HandingBaggage;
        }

        else Debug.Log("Sıkıntı burada bro");
    }


    private void HandingBaggageState()
    {
        if (HasBaggage && myBaggage != null && PassengerManager.Instance.IsPlayerInRange)
        {
            HasBaggage = false;
            animator.SetTrigger("HandBaggageTrigger");
            Log($"🎒 {name} triggered baggage hand animation.");
            BaggageAnimSequence();
        }
    }
    private void BaggageAnimSequence()
    {
        if (myBaggage == null)
        {
            Debug.LogError($"{name}: myBaggage NULL — InitializeBaggage başarısız olmuş ya da önceden sıfırlanmış.");
            return;
        }

        var holder = FindObjectOfType<PlayerBaggageHolder>();
        if (holder == null)
        {
            Debug.LogError($"{name}: PlayerBaggageHolder bulunamadı!");
            return;
        }

        Transform target = holder.baggageStackRoot;

        tempVec = target.position;

        myBaggage.transform.SetParent(null);
        myBaggage.transform
            .DOJump(target.position, 1f, 1, 0.6f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                myBaggage.transform.DORotate(new Vector3(0, 0, 90), 0.25f, RotateMode.Fast)
                    .SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        holder.AddBaggage(myBaggage);
                        myBaggage = null;

                        EventBus.RaisePassengerHandedBaggage(this);
                    });
            });
    }

    public void TryStartHandingBaggage()
    {
        if (currentState == PassengerState.Waiting
            && QueueIndex == 0
            && PassengerManager.Instance.IsPlayerInRange
            && PassengerManager.Instance.CanProcessFrontPassenger())
        {
            PassengerManager.Instance.SetProcessingFrontPassenger(true);
            currentState = PassengerState.HandingBaggage;
        }
    }

    //public void OnHandAnimationFinished() 
    //{
    //    animator.SetBool("HasBaggage", false);
    //    currentState = PassengerState.WalkingToTarget;
    //    Debug.Log("OnHandAnimFinished and Current State = " + currentState.ToString());

    //    var path = PassengerManager.Instance.GetPathForPassenger(this);
    //    MoveAlongPath(path);

    //}

    public void OnHandAnimationFinished()
    {
        animator.SetBool("HasBaggage", false);
        currentState = PassengerState.WalkingToTarget;
        Log($"🎬 {name} finished hand animation. State={currentState}");

        var path = PassengerManager.Instance.GetPathForPassenger(this);
        MoveAlongPath(path);

        // Bavul verildikten sonra ön sıra flag resetlenebilir
        hasArrivedAtQueueFront = false;
    }


    /*
    public void MoveToFrontAndStartSequence(Vector3 frontPos)
    {
        // Eğer zaten hareket ediyorsa durdur
        DOTween.Kill(transform);

        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget; // geçici state (hareket ediyor)

        transform
            .DOMove(frontPos, 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Waiting;

                Debug.Log($"{name} sıranın önüne geldi. Şimdi bavul verme başlıyor.");
                TryStartHandingBaggage();
            });
    }


    private void MoveAlongPath(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count == 0)
        {
            Debug.LogWarning($"{name}: Path boş, yürüyüş iptal.");
            return;
        }

        // Eski tween varsa durdur (önceki hareketleri temizler)
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
            activeTween = null;
        }

        currentPath = pathPoints;
        animator.SetBool("IsMoving", true);

        // Mesafeye göre süre hesapla (daha doğal yürüyüş)
        float duration = CalculatePathDuration(pathPoints);

        // 🔹 DOTween hareketi başlat
        activeTween = transform
            .DOPath(
                pathPoints.ToArray(),
                duration,
                PathType.CatmullRom
            )
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f) // path yönüne dönsün
            .SetId($"{name}_pathTween") // kolay kill edebilmek için ID ekle
            .OnWaypointChange(i =>
            {
                Debug.Log($"{name} {i}. waypoint'e ulaştı. Pos = {transform.position}");
            })
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Done;
                activeTween = null;

                EventBus.RaisePassengerReachedTarget(this);
                Debug.Log($"{name} path bitti, state = Done");
            });
    }
    */

    public void MoveToFrontAndStartSequence(Vector3 frontPos)
    {
        DOTween.Kill(transform);

        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget;
        Log($"➡️ {name} moving to front position {frontPos}");

        transform
            .DOMove(frontPos, 0.7f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Waiting;

                // Ön sıraya geldi
                hasArrivedAtQueueFront = true;

                Log($"✅ {name} arrived at front. Now ready to start baggage handoff.");

                // Fiziksel olarak geldiğinde bavul sekansını tetikle
                TryStartHandingBaggage();
            });
    }


    private void MoveAlongPath(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count == 0)
        {
            Log($"{name}: ❌ Path boş, yürüyüş iptal.");
            return;
        }

        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
            activeTween = null;
        }

        currentPath = pathPoints;
        animator.SetBool("IsMoving", true);

        float duration = CalculatePathDuration(pathPoints);
        Log($"🚶 {name} starting path movement. Duration={duration:F2}s, waypoints={pathPoints.Count}");

        activeTween = transform
            .DOPath(pathPoints.ToArray(), duration, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetLookAt(0.01f)
            .SetId($"{name}_pathTween")
            .OnWaypointChange(i =>
            {
                Log($"{name} reached waypoint {i}/{pathPoints.Count - 1}");
            })
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Done;
                activeTween = null;

                Log($"🏁 {name} path complete. Raising PassengerReachedTarget event.");
                EventBus.RaisePassengerReachedTarget(this);
            });
    }


    private float CalculatePathDuration(List<Vector3> path)
    {
        float distance = 0f;
        for (int i = 1; i < path.Count; i++)
            distance += Vector3.Distance(path[i - 1], path[i]);
        return distance / customerSpeed;
    }

    private void OnDrawGizmos()
    {
        if (tempVec != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(tempVec, Vector3.one);
        }


    }

    private void Log(string message)
    {
        if (GameManager.Instance != null && GameManager.Instance.ShowDetailedLogs)
            Debug.Log($"[LOG] {message}");
    }

}
