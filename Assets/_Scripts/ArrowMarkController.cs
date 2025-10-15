using UnityEngine;
using DG.Tweening;

public class ArrowMarkController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 0.3f;   // Yukarý-aþaðý mesafe
    [SerializeField] private float floatDuration = 1.2f; // Animasyon süresi
    [SerializeField] private bool faceCamera = true;     // Oyuncuya bakacak mý?

    private Vector3 basePos;
    private Tween moveTween;
    private bool isActive = false;

    private void OnEnable()
    {
        // Baþlatýldýðýnda animasyonu hemen çalýþtýr
        StartFloating();
    }

    private void OnDisable()
    {
        StopFloating();
    }

    private void Update()
    {
        // Oyuncuya veya kameraya dönsün (opsiyonel)
        if (faceCamera && Camera.main)
        {
            transform.forward = Camera.main.transform.forward;
        }
    }

    public void ShowAt(Vector3 worldPos)
    {
        // Eski animasyonu durdur
        StopFloating();

        // Pozisyon ve aktiflik
        transform.position = worldPos;
        gameObject.SetActive(true);
        basePos = transform.position;

        StartFloating();
        isActive = true;
    }

    public void Hide()
    {
        StopFloating();
        gameObject.SetActive(false);
        isActive = false;
    }

    private void StartFloating()
    {
        if (!gameObject.activeInHierarchy) return;

        basePos = transform.position;
        // Yukarý aþaðý sürekli hareket eden tween (ping-pong)
        moveTween = transform.DOMoveY(basePos.y + floatHeight, floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopFloating()
    {
        if (moveTween != null && moveTween.IsActive())
        {
            moveTween.Kill();
        }
        transform.position = basePos;
    }
}
