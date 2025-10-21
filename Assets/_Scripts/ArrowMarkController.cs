using UnityEngine;
using DG.Tweening;

public class ArrowMarkController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float floatHeight = 0.3f;   // Yukar�-a�a�� mesafe
    [SerializeField] private float floatDuration = 1.2f; // Animasyon s�resi
    [SerializeField] private bool faceCamera = true;     // Oyuncuya bakacak m�?

    private Vector3 basePos;
    private Tween moveTween;
    private bool isActive = false;

    private void OnEnable()
    {
        // Ba�lat�ld���nda animasyonu hemen �al��t�r
        StartFloating();
    }

    private void OnDisable()
    {
        StopFloating();
    }

    private void Update()
    {
        // Oyuncuya veya kameraya d�ns�n (opsiyonel)
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
        // Yukar� a�a�� s�rekli hareket eden tween (ping-pong)
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
