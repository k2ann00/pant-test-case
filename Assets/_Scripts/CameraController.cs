using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance;
    [Header("Follow Settings")]
    public Transform player;                  // Takip edilecek oyuncu
    public Vector3 offset = new Vector3(-2.45f, 9.04f, 8.06f); // Offset
    public float followSpeed = 5f;            // Ne kadar hızlı takip edilecek
    public float dampingSpeed = 5f;           // Ne kadar hızlı takip edilecek
    private Vector3 velocity = Vector3.zero;  // SmoothDamp için dahili

    [Header("Board Camera Settings")]
    [SerializeField] private Transform boardCameraTarget; // Inspector'dan ayarlanacak kamera pozisyonu (Empty GameObject)
    [SerializeField] private float boardTransitionDuration = 1.5f; // Geçiş süresi
    [SerializeField] private Ease cameraEaseType = Ease.InOutCubic; // Kamera geçiş animasyon tipi
    [SerializeField] private float boardFOV = 85f; // Board modunda FOV değeri

    private bool isFocusingOnBoard = false;
    private Transform currentBoardTarget;
    private Vector3 originalOffset;
    private float originalFOV;
    private Camera cam;

    void Awake () => Instance = this;
    private void Start()
    {
        // Camera component'i al
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraController] Camera component not found!");
        }

        // Original değerleri kaydet
        originalOffset = offset;
        if (cam != null)
            originalFOV = cam.fieldOfView;
    }

    private void OnEnable()
    {
        EventBus.BoardUnlocked += OnBoardUnlocked;
    }

    private void OnDisable()
    {
        EventBus.BoardUnlocked -= OnBoardUnlocked;
    }

    private void OnBoardUnlocked(Transform boardTransform)
    {
        Debug.Log($"[CameraController] Board unlocked! Focusing on board...");
        FocusOnBoard(boardTransform);
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Board'a odaklanıyorsa normal takibi atla
        if (isFocusingOnBoard)
            return;

        // Hedef pozisyon (offset ile)
        Vector3 targetPos = player.position + offset;

        // SmoothDamp ile pürüzsüz takip
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref velocity, dampingSpeed / followSpeed);

        // Kamera her zaman player'a baksın
        transform.LookAt(player.position);
    }

    
    public void FocusOnBoard(Transform boardTransform)
    {
        if (boardCameraTarget == null)
        {
            Debug.LogWarning("[CameraController] Board Camera Target is not assigned in Inspector!");
            return;
        }

        currentBoardTarget = boardTransform;
        isFocusingOnBoard = true;

        Debug.Log($"[CameraController] Starting cinematic transition to board camera target...");

        // Kamerayı Inspector'dan ayarlanan target pozisyon ve rotasyona smooth geçir
        Sequence seq = DOTween.Sequence();

        // Pozisyon geçişi - Inspector'dan ayarlanan pozisyona git
        seq.Append(transform.DOMove(boardCameraTarget.position, boardTransitionDuration)
            .SetEase(cameraEaseType));

        // Rotasyon geçişi - Inspector'dan ayarlanan rotasyonu al
        seq.Join(transform.DORotateQuaternion(boardCameraTarget.rotation, boardTransitionDuration)
            .SetEase(cameraEaseType));

        // FOV geçişi - Yavaşça 85'e doğru kaydır
        if (cam != null)
        {
            seq.Join(DOTween.To(() => cam.fieldOfView, x => cam.fieldOfView = x, boardFOV, boardTransitionDuration)
                .SetEase(cameraEaseType));
        }

        seq.OnComplete(() =>
        {
            Debug.Log("[CameraController] Cinematic camera transition completed!");
        });
    }

  
    public void ReturnToPlayerFollow()
    {
        if (!isFocusingOnBoard) return;

        Debug.Log("[CameraController] Returning to player follow mode...");

        isFocusingOnBoard = false;
        currentBoardTarget = null;

        // Offset'i original'e geri al
        offset = originalOffset;

        // Kamerayı tekrar player takibine geçir
        Vector3 targetPos = player.position + offset;

        Sequence seq = DOTween.Sequence();

        seq.Append(transform.DOMove(targetPos, boardTransitionDuration)
            .SetEase(cameraEaseType));

        seq.Join(transform.DOLookAt(player.position, boardTransitionDuration)
            .SetEase(cameraEaseType));

        // FOV'u original değerine geri döndür
        if (cam != null)
        {
            seq.Join(DOTween.To(() => cam.fieldOfView, x => cam.fieldOfView = x, originalFOV, boardTransitionDuration)
                .SetEase(cameraEaseType));
        }

        seq.OnComplete(() =>
        {
            Debug.Log("[CameraController] Camera returned to player follow!");
        });
    }


    public void FocusOn(Transform focusPoint, float duration = 3f)
    {
        StopAllCoroutines();
        StartCoroutine(FocusRoutine(focusPoint, duration));
    }

    private IEnumerator FocusRoutine(Transform focusPoint, float duration)
    {
        Vector3 startOffset = offset;
        Vector3 targetOffset = offset; // istersen farkl� offset verebilirsin
        float elapsed = 0f;

        // Hedefe ge�i�
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(transform.position, focusPoint.position + targetOffset, elapsed);
            transform.LookAt(focusPoint.position);
            yield return null;
        }

        yield return new WaitForSeconds(duration);

        // Tekrar player'a d�n
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
