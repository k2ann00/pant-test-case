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

public class PassengerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private TextMeshPro stateText;
    [SerializeField] private Transform firstStep; // Inspector'dan atanacak
    [SerializeField] private Transform lastStep;  // Inspector'dan atanacak

    [Header("Baggage Settings")]
    [SerializeField] private GameObject myBaggage;
    [SerializeField] private bool HasBaggage = true;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    private Tween activeTween;
    private List<Vector3> currentPath = new List<Vector3>();

    public int QueueIndex { get; set; }
    private PassengerState currentState;

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
                //animator.SetBool("IsMoving", false);
                //currentState = PassengerState.Done;
                //EventBus.RaisePassengerStateChanged(this);
                //EventBus.RaisePassengerReachedTarget(this);
                animator.SetBool("IsMoving", false);
                // 🧩 Eğer path merdiven tabanında bitiyorsa tırmanma başlat
                if (EventBus.HasStairs)
                    EventBus.RaisePassengerReachedStairs(this);
                else
                    EventBus.RaisePassengerReachedTarget(this);

            });
    }

    //public void StartClimbingRoutine(Transform[] steps)
    //{
    //    if (steps == null || steps.Length == 0) return;

    //    currentState = PassengerState.Climbing;
    //    EventBus.RaisePassengerStateChanged(this);
    //    animator.SetBool("IsMoving", false);

    //    rb.isKinematic = false; // Fizik devreye girsin
    //    StartCoroutine(ClimbStepsRoutine(steps));
    //}

    //private IEnumerator ClimbStepsRoutine(Transform[] steps)
    //{
    //    float climbSpeed = 2f;
    //    rb.isKinematic = true;
    //    foreach (Transform step in steps)
    //    {
    //        Vector3 target = step.position + Vector3.up * 0.1f;
    //        while (Vector3.Distance(transform.position, target) > 0.1f)
    //        {
    //            Debug.DrawLine(transform.position, target, Color.red);
    //            Vector3 dir = (target - transform.position).normalized;
    //            rb.MovePosition(transform.position + dir * climbSpeed * Time.fixedDeltaTime);
    //            yield return new WaitForFixedUpdate();
    //        }
    //        yield return new WaitForSeconds(0.05f);
    //    }

    //    // Üste çıkınca
    //    rb.isKinematic = true;
    //    currentState = PassengerState.Done;
    //    EventBus.RaisePassengerStateChanged(this);
    //    EventBus.RaisePassengerReachedTarget(this);
    //}


    public void StartClimbingRoutine()
    {
        currentState = PassengerState.Climbing;
        EventBus.RaisePassengerStateChanged(this);
        animator.SetBool("IsMoving", false);

        rb.isKinematic = false;
        StartCoroutine(ClimbRoutine());
    }

    private IEnumerator ClimbRoutine()
    {
        rb.isKinematic = true;

        Vector3 startPos = firstStep.position;
        Vector3 endPos = lastStep.position;

        // yön vektörü: merdiven boyunca yukarı doğru
        Vector3 climbDir = (endPos - startPos).normalized;

        while (Vector3.Distance(transform.position, endPos) > 0.1f)
        {
            Debug.DrawLine(transform.position, endPos);
            rb.MovePosition(transform.position + climbDir * 2.0f * Time.fixedDeltaTime);
            yield return new WaitForFixedUpdate();
        }
        //transform.position = endPos;

        // Üste çıkınca
        rb.isKinematic = true;
        currentState = PassengerState.Done;
        EventBus.RaisePassengerStateChanged(this);
        EventBus.RaisePassengerReachedTarget(this);
    }

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
}
