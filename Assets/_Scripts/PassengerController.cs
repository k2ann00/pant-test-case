using DG.Tweening;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TextMeshPro stateText;

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
        currentState = PassengerState.Waiting;
    }

    private void Update()
    {
        if (stateText != null)
            stateText.text = currentState.ToString();
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
                EventBus.RaisePassengerHandedBaggage(this);
            });
    }
    
    public void StartWalkingPath(List<Vector3> path)
    {
        if (path == null || path.Count == 0) return;

        animator.SetBool("IsMoving", true);
        currentState = PassengerState.WalkingToTarget;
        EventBus.RaisePassengerStateChanged(this);

        float duration = CalculateDuration(path);
        activeTween = transform
            .DOPath(path.ToArray(), duration, PathType.CatmullRom)
            .SetEase(Ease.Linear)
            .SetLookAt(0.1f)
            .OnComplete(() =>
            {
                animator.SetBool("IsMoving", false);
                currentState = PassengerState.Done;
                EventBus.RaisePassengerStateChanged(this);
                EventBus.RaisePassengerReachedTarget(this);
            });
    }

    private float CalculateDuration(List<Vector3> path)
    {
        float total = 0f;
        for (int i = 1; i < path.Count; i++)
            total += Vector3.Distance(path[i - 1], path[i]);
        return total / moveSpeed;
    }
}
