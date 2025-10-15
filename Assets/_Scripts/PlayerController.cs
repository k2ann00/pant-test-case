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

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        // Joystick eksenlerini standart X/Z yönlerine map et
        input.x = -joystick.Horizontal;   // sað/sol
        input.z = -joystick.Vertical;     // ileri/geri

        // Animasyon hýzý
        animator.SetFloat("Speed", input.magnitude);
    }

    void FixedUpdate()
    {
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
}
