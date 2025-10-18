using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private FloatingJoystick joystick;

    [Header("Movement Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Components")]
    private Rigidbody rb;
    private Animator animator;

    private Vector3 input;
    private bool isClimbing = false; // ✅ Tırmanma durumu

    public bool IsPlayerReachedTopEscalator { get; private set; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // ✅ Tırmanma sırasında input alma
        if (isClimbing)
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
        // ✅ Tırmanma sırasında hareket etme
        if (isClimbing) return;

        if (input.sqrMagnitude > 0.001f)
        {
            // Hareket yönüne göre rotasyon
            Vector3 moveDirection = new Vector3(input.x, 0f, input.z);
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        // Hareket
        Vector3 move = transform.forward * input.magnitude * speed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("EscalatorEndPoint"))
        {
            IsPlayerReachedTopEscalator = true;
        }
        else
        if (other.CompareTag("EscalatorStartPoint"))
        {
            StartCoroutine(StartClimbing());
        }
    }

    private IEnumerator StartClimbing()
    {
        // ✅ Tırmanma başlıyor - input devre dışı
        isClimbing = true;
        rb.isKinematic = true;
        animator.SetFloat("Speed", 0f); // Yürüme animasyonu durdur

        Vector3 startPos = transform.position;
        Vector3 endPos = PassengerManager.Instance.lastStepPos;
        float climbSpeed = 2f;

        Debug.Log($"🧗 [Player] CLIMB START | From: {startPos} → To: {endPos} | Distance: {Vector3.Distance(startPos, endPos)}");

        float timer = 0f;
        while (!IsPlayerReachedTopEscalator)
        {
            Vector3 climbDir = (endPos - rb.position).normalized;
            Vector3 newPos = rb.position + climbDir * climbSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);

            timer += Time.fixedDeltaTime;
            if (timer >= 0.5f)
            {
                Debug.Log($"🧗 [Player] CLIMBING | Current: {rb.position} | Target: {endPos} | Distance: {Vector3.Distance(rb.position, endPos)}");
                timer = 0f;
            }

            // Hedef çizgisi (Kırmızı: başlangıç → hedef, Yeşil: mevcut pozisyon → hedef)
            Debug.DrawLine(startPos, endPos, Color.red, 0.1f);
            Debug.DrawLine(rb.position, endPos, Color.green, 0.1f);
            Debug.DrawRay(rb.position, climbDir * 2f, Color.yellow, 0.1f);

            yield return new WaitForFixedUpdate();
        }

        Debug.Log($"🧗 [Player] REACHED TOP ESC TRIGGER | Moving to exact position...");

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

        // ✅ Rigidbody'yi tamamen durdur ve temizle
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false; // ✅ Fizik tekrar aktif

        Debug.Log($"✅ [Player] CLIMB COMPLETE | Final Position: {rb.position}");

        // ✅ Input'u sıfırla ve tırmanmayı bitir
        input = Vector3.zero;
        isClimbing = false;
        IsPlayerReachedTopEscalator = false; // ✅ Reset flag (bir sonraki tırmanma için)
    }
}
