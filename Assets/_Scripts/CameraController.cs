using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Follow Settings")]
    public Transform player;                  // Takip edilecek oyuncu
    public Vector3 offset = new Vector3(-2.45f, 9.04f, 8.06f); // Offset
    public float followSpeed = 5f;            // Ne kadar hýzlý takip edilecek
    public float dampingSpeed = 5f;            // Ne kadar hýzlý takip edilecek
    private Vector3 velocity = Vector3.zero;  // SmoothDamp için dahili

    void LateUpdate()
    {
        if (player == null) return;

        // Hedef pozisyon (offset ile)
        Vector3 targetPos = player.position + offset;

        // SmoothDamp ile pürüzsüz takip
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, dampingSpeed / followSpeed);

        // Kamera her zaman player'a baksýn
        transform.LookAt(player.position);
    }

    /// <summary>
    /// Belirli bir noktaya odaklanmak için kullan
    /// </summary>
    public void FocusOn(Transform focusPoint, float duration = 3f)
    {
        StopAllCoroutines();
        StartCoroutine(FocusRoutine(focusPoint, duration));
    }

    private IEnumerator FocusRoutine(Transform focusPoint, float duration)
    {
        Vector3 startOffset = offset;
        Vector3 targetOffset = offset; // istersen farklý offset verebilirsin
        float elapsed = 0f;

        // Hedefe geçiþ
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, focusPoint.position + targetOffset, elapsed);
            transform.LookAt(focusPoint.position);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // Tekrar player'a dön
        elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, player.position + startOffset, elapsed);
            transform.LookAt(player.position);
            yield return null;
        }
    }
}
