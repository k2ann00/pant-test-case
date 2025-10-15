using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;



public class PassengerControllerCOP : MonoBehaviour
{
    #region OldestCode
    //[Header("Movement Settings")]
    //[SerializeField] private float moveSpeed = 2f;
    //[SerializeField] private float rotationSpeed = 10f;
    //[SerializeField] private float reachThreshold = 0.2f;

    //[Header("References")]
    //private Animator animator;
    //private Transform currentTarget;
    //private GameObject myBaggage;

    //public bool HasBaggage => myBaggage != null;

    //public PassengerState State { get; private set; } = PassengerState.Waiting;
    //public int queueIndex;


    ////SILINECEK
    //[SerializeField] private TextMeshPro stateText;
    //private void Awake()
    //{
    //    animator = GetComponentInChildren<Animator>();
    //    var baggage = transform.Find("Baggage");
    //    if (baggage != null)
    //        myBaggage = baggage.gameObject;
    //    else
    //        Debug.LogWarning($"{name} için 'Baggage' child bulunamadı.");
    //}

    //private void Update()
    //{
    //    if ((State == PassengerState.WalkingToStairs || State == PassengerState.IdleAtSlot) && currentTarget != null)
    //        MoveTowards(currentTarget.position);

    //    stateText.text = State.ToString(); //SILINECEK
    //}

    //private void MoveTowards(Vector3 target)
    //{
    //    Vector3 dir = (target - transform.position);
    //    dir.y = 0;

    //    if (dir.sqrMagnitude < reachThreshold * reachThreshold)
    //    {
    //        OnReachTarget();
    //        return;
    //    }

    //    transform.position += dir.normalized * moveSpeed * Time.deltaTime;
    //    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
    //}

    //private void OnReachTarget()
    //{
    //    animator.SetBool("IsMoving", false);

    //    if (State == PassengerState.WalkingToStairs)
    //    {
    //        State = PassengerState.IdleAtSlot;
    //        Debug.Log($"[ReachTarget] {name} sıradaki slota ulaştı. State = {State}");

    //        if (!HasBaggage)
    //        {
    //            MoveToStairs(QueueManager.Instance.StairsTarget);
    //        }

    //        // Bavulu olan ön sıradaki Passenger bavul verecek
    //        else if (State == PassengerState.WalkingToStairs)
    //        {
    //            State = PassengerState.Done;
    //            Debug.Log($"[ReachTarget] {name} merdivene ulaştı.");
    //            gameObject.SetActive(false);
    //        }
    //    }
    //}


    //// Animation Event tarafından tetiklenecek (Carry-Recive bitince)
    //public void OnHandAnimationFinished()
    //{
    //    Debug.Log($"[AnimEvent] {name} OnHandAnimationFinished çağrıldı.");

    //    if (!HasBaggage)
    //    {
    //        Debug.LogWarning($"{name} OnHandAnimationFinished ama bavulu yok!");
    //        return;
    //    }

    //    animator.SetBool("HasBaggage", false);

    //    var holder = FindObjectOfType<PlayerBaggageHolder>();
    //    if (holder == null)
    //    {
    //        Debug.LogError($"{name}: PlayerBaggageHolder bulunamadı!");
    //        return;
    //    }

    //    Transform target = holder.baggageStackRoot;

    //    myBaggage.transform.SetParent(null);
    //    myBaggage.transform
    //        .DOJump(target.position, 1f, 1, 0.6f)
    //        .SetEase(Ease.OutQuad)
    //        .OnComplete(() =>
    //        {
    //            myBaggage.transform.DORotate(new Vector3(0, 0, 90), 0.25f, RotateMode.Fast)
    //                .SetEase(Ease.OutBack)
    //                .OnComplete(() =>
    //                {
    //                    holder.AddBaggage(myBaggage);
    //                    myBaggage = null;

    //                    EventBus.RaisePassengerHandedBaggage(this);

    //                    // Bavulu verdikten sonra merdivene yürür
    //                    MoveToStairs(QueueManager.Instance.StairsTarget);
    //                });
    //        });
    //}

    //public void MoveToStairs(Transform target)
    //{
    //    currentTarget = target;
    //    State = PassengerState.WalkingToStairs;
    //    animator.SetBool("IsMoving", true);
    //    Debug.Log($"[MoveToStairs] {name} merdivene yöneliyor. Target: {target.name}, State: {State}");
    //}

    //public void MoveToSlot(Transform target)
    //{
    //    currentTarget = target;
    //    State = PassengerState.WalkingToSlot; // Yeni state: sıraya yürüyor
    //    animator.SetBool("IsMoving", true);
    //    Debug.Log($"[MoveToSlot] {name} sıradaki pozisyona ilerliyor ({target.name}). State: {State}");
    //}


    //public void StartHandingBaggage()
    //{
    //    if (!HasBaggage)
    //    {
    //        Debug.LogWarning($"{name} bavul yok, veremiyor.");
    //        return;
    //    }

    //    if (State != PassengerState.IdleAtSlot && State != PassengerState.Waiting)
    //    {
    //        Debug.LogWarning($"{name} şu an bavul veremez. State: {State}");
    //        return;
    //    }

    //    State = PassengerState.HandingBaggage;
    //    animator.SetTrigger("HandBaggageTrigger");
    //    Debug.Log($"[Handing] {name} bavul veriyor...");
    //}
    #endregion

/*
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform baggageHoldPoint;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Vector3 targetPosition;

    // State Management
    private Dictionary<PassengerState, IPassengerState> states;
    private IPassengerState currentStateInstance;
    private PassengerState currentState;
    [SerializeField] private TextMeshPro stateText;


    // Queue Management
    private int queueIndex = -1;

    // Baggage
    private GameObject currentBaggage;

    // Properties
    public Rigidbody Rigidbody => rb;
    public Animator Animator => animator;
    public float MoveSpeed => moveSpeed;
    public Vector3 TargetPosition => targetPosition;
    public int QueueIndex
    {
        get => queueIndex;
        set => queueIndex = value;
    }
    public PassengerState CurrentState => currentState;
    public bool HasBaggage => currentBaggage != null;

    private void Awake()
    {
        InitializeComponents();
        InitializeStates();
    }

    private void OnEnable()
    {
        ChangeState(PassengerState.Waiting);
    }

    private void Update()
    {
        currentStateInstance?.Update(this);
    }

    private void InitializeComponents()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void InitializeStates()
    {
        states = new Dictionary<PassengerState, IPassengerState>
        {
            { PassengerState.Waiting, new WaitingState() },
            { PassengerState.HandingBaggage, new HandingBaggageState() },
            { PassengerState.WalkingToTarget, new WalkingToTargetState() },
            { PassengerState.Done, new DoneState() }
        };

    }

    public void ChangeState(PassengerState newState)
    {
        if (currentState == newState)
            return;

        currentStateInstance?.Exit(this);

        currentState = newState;
        currentStateInstance = states[newState];

        currentStateInstance.Enter(this);
        EventBus.RaisePassengerStateChanged(this);
        stateText.text = newState.ToString();
    }

    public void SetBaggage(GameObject baggage)
    {
        currentBaggage = baggage;
        if (baggage != null)
        {
            baggage.transform.SetParent(baggageHoldPoint);
            baggage.transform.localPosition = Vector3.zero;
            baggage.transform.localRotation = Quaternion.identity;
        }
    }

    public void SetTargetPosition(Vector3 target)
    {
        targetPosition = target;
    }

    // Animation Event - Animator'den çağrılacak
    public void OnHandAnimationFinished()
    {
        if (currentBaggage != null)
        {
            HandBaggageToPlayer();
        }
    }

    private void HandBaggageToPlayer()
    {
        var baggage = currentBaggage;
        currentBaggage = null;

        // Parent'ı null yap
        baggage.transform.SetParent(null);

        // Player'ın baggage holder'ını bul
        var playerBaggageHolder = GameObject.FindGameObjectWithTag("PlayerBaggageHolder");
        if (playerBaggageHolder == null)
        {
            Debug.LogError("PlayerBaggageHolder not found!");
            return;
        }

        var baggageStackRoot = playerBaggageHolder.transform.Find("BaggageStackRoot");
        if (baggageStackRoot == null)
        {
            Debug.LogError("BaggageStackRoot not found!");
            return;
        }

        // DoTween animasyonu
        var sequence = DOTween.Sequence();

        // Önce yukarı çık
        sequence.Append(baggage.transform.DOMoveY(baggage.transform.position.y + 2f, 0.3f)
            .SetEase(Ease.OutQuad));

        // Sonra player'a doğru git
        sequence.Append(baggage.transform.DOMove(baggageStackRoot.position, 0.4f)
            .SetEase(Ease.InOutQuad));

        // Animasyon bitince parent'ı set et ve state değiştir
        sequence.OnComplete(() =>
        {
            baggage.transform.SetParent(baggageStackRoot);
            baggage.transform.localRotation = Quaternion.identity;

            EventBus.RaisePassengerHandedBaggage(this);
            ChangeState(PassengerState.WalkingToTarget);
        });
    }

    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponent<Animator>();
    }
*/
}

