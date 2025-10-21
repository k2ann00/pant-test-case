using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    [SerializeField] private FloatingJoystick joystick;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Components")]
    private Rigidbody rb;
    private Animator animator;

    private Vector3 input;
    private bool isClimbing = false; //  Tırmanma durumu

    [Header("Look At System")]
    private Vector3 targetLookDirection; // Bakmak istediğimiz yön
    private bool shouldLookAtTarget = false; // Target'a bakmalı mı?
    private bool inputFrozen = false; // Cinematic sırasında input donduruldu mu?

    public bool IsPlayerReachedTopEscalator { get; private set; }

    private void OnEnable()
    {
        EventBus.BoardUnlocked += OnBoardUnlocked;
    }

    private void OnDisable()
    {
        EventBus.BoardUnlocked -= OnBoardUnlocked;
    }

    void Awake () => Instance = this;
    private void OnBoardUnlocked(Transform transform)
    {
        inputFrozen = true;
    }

    [Header("Model Reference")]
    [SerializeField] private Transform playerModel; // Child player model transform (assign in inspector)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();

        // If playerModel not assigned, try to find it automatically
        if (playerModel == null && transform.childCount > 0)
        {
            playerModel = transform.GetChild(0);
            Debug.Log($"[PlayerController] Auto-assigned playerModel: {playerModel.name}");
        }
    }

    void LateUpdate()
    {
        // Fix drift issue - reset child model's local transform every frame
        if (playerModel != null)
        {
            playerModel.localPosition = Vector3.zero;
            playerModel.localRotation = Quaternion.identity;
        }
    }

    public void CinematicLookAt(Vector3 circleCenter, Vector3 lookTarget, float duration = 1f)
    {
        StartCoroutine(CinematicLookAtRoutine(circleCenter, lookTarget, duration));
    }

    private IEnumerator CinematicLookAtRoutine(Vector3 circleCenter, Vector3 lookTarget, float duration)
    {
        Debug.Log($"[PlayerController] Starting cinematic look-at to {lookTarget}");

        // 1. Input'u dondur
        inputFrozen = true;
        input = Vector3.zero;

        // 2. Circle center'a hareket et
        Vector3 targetPos = new Vector3(circleCenter.x, transform.position.y, circleCenter.z);
        float moveDuration = duration * 0.4f; // %40'ı hareket için

        transform.DOMove(targetPos, moveDuration)
            .SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(moveDuration);

        // 3. Look target'a bak
        Vector3 lookDirection = lookTarget - transform.position;
        lookDirection.y = 0; // Yatay bakış

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            float rotateDuration = duration * 0.6f; // %60'ı rotasyon için

            transform.DORotateQuaternion(targetRotation, rotateDuration)
                .SetEase(Ease.InOutQuad);

            yield return new WaitForSeconds(rotateDuration);
        }

        Debug.Log($"[PlayerController] Cinematic look-at completed");

        // 4. Input'u geri aç
        inputFrozen = false;
    }

    public void LookAtPosition(Vector3 targetPosition)
    {
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0; // Yatay bakış (Y eksenini sıfırla)

        if (direction.sqrMagnitude > 0.001f)
        {
            targetLookDirection = direction.normalized;
            shouldLookAtTarget = true;
            Debug.Log($"[PlayerController] Looking at position: {targetPosition} | Direction: {targetLookDirection}");
        }
    }

    public void CancelLookAt()
    {
        shouldLookAtTarget = false;
    }

    void Update()
    {
        //  Tırmanma sırasında input alma
        if (isClimbing)
        {
            input = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            return;
        }

        //  Input frozen ise input alma
        if (inputFrozen)
        {
            input = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            return;
        }

        // Joystick eksenlerini standart X/Z yönlerine map et
        input.x = -joystick.Horizontal;   // sağ/sol
        input.z = -joystick.Vertical;     // ileri/geri

        // Animasyon hızı
        animator.SetFloat("Speed", input.magnitude);
    }

    void FixedUpdate()
    {
        //  Tırmanma sırasında hareket etme
        if (isClimbing) return;

        if (input.sqrMagnitude > 0.001f)
        {
            // Input var - normal rotasyon ve look-at sistemini devre dışı bırak
            shouldLookAtTarget = false;

            Vector3 moveDirection = new Vector3(input.x, 0f, input.z);
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
        else if (shouldLookAtTarget && targetLookDirection.sqrMagnitude > 0.001f)
        {
            // Input yok ve target var - target'a bak
            Quaternion targetRotation = Quaternion.LookRotation(targetLookDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Hareket
        Vector3 move = transform.forward * input.magnitude * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Yeni EscalatorTrigger sistemi
        EscalatorTrigger trigger = other.GetComponent<EscalatorTrigger>();
        if (trigger != null)
        {
            if (trigger.IsStartPoint)
            {
                // Başlangıç noktası - tırmanmaya başla
                Vector3 targetPos = trigger.GetTargetPosition();
                StartCoroutine(StartClimbing(targetPos));
            }
            else
            {
                // Bitiş noktası - hedefe ulaşıldı
                IsPlayerReachedTopEscalator = true;
            }
            return;
        }

        // Eski tag sistemi (backward compatibility)
        if (other.CompareTag("EscalatorEndPoint"))
        {
            IsPlayerReachedTopEscalator = true;
        }
        else if (other.CompareTag("EscalatorStartPoint"))
        {
            // Eski sistem - PassengerManager'dan pozisyon al
            Vector3 targetPos = PassengerManager.Instance.lastStepPos;
            StartCoroutine(StartClimbing(targetPos));
        }
    }

    private IEnumerator StartClimbing(Vector3 targetPosition)
    {
        //  Tırmanma başlıyor - input devre dışı
        isClimbing = true;
        rb.isKinematic = true;
        animator.SetFloat("Speed", 0f); // Yürüme animasyonu durdur

        Vector3 startPos = transform.position;
        Vector3 endPos = targetPosition;
        float climbSpeed = 2f;

        Debug.Log($"[Player] CLIMB START | From: {startPos} → To: {endPos} | Distance: {Vector3.Distance(startPos, endPos)}");

        float timer = 0f;
        while (!IsPlayerReachedTopEscalator)
        {
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * climbSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            timer += Time.fixedDeltaTime;
            if (timer >= 0.5f)
            {
                timer = 0f;
            }

            // Hedef çizgisi (Kırmızı: başlangıç → hedef, Yeşil: mevcut pozisyon → hedef)
            Debug.DrawLine(startPos, endPos, Color.red, 0.1f);
            Debug.DrawLine(rb.position, endPos, Color.green, 0.1f);
            Debug.DrawRay(rb.position, climbDir * 2f, Color.yellow, 0.1f);

            yield return new WaitForFixedUpdate();
        }


        // Trigger geldiğinde yavaşça son pozisyona yerleş
        while (Vector3.Distance(rb.position, endPos) > 0.05f)
        {
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * (2f) * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            Debug.DrawLine(rb.position, endPos, Color.blue, 0.1f);
            yield return new WaitForFixedUpdate();
        }

        rb.MovePosition(endPos);

        //  Rigidbody'yi tamamen durdur ve temizle
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false; //  Fizik tekrar aktif


        //  Input'u sıfırla ve tırmanmayı bitir
        input = Vector3.zero;
        isClimbing = false;
        IsPlayerReachedTopEscalator = false; //  Reset flag (bir sonraki tırmanma için)
    }
}
