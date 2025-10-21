using UnityEngine;
using UnityEngine.EventSystems;


public class PaintInputController : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask paintableLayer; // Board'un layer'ı
    [SerializeField] private float raycastDistance = 100f;

    [Header("Input Settings")]
    [SerializeField] private bool isPaintingEnabled = false; // Başlangıçta kapalı - Unlockable_area2 unlock olunca açılır
    [SerializeField] private bool showDebugRays = true; // Debug için ray görüntüle

    [Header("UV Mapping Settings - TEST İÇİN")]
    [SerializeField] private UVAxisMode uvAxisMode = UVAxisMode.Auto;
    [SerializeField] private bool invertU = false; // U eksenini ters çevir
    [SerializeField] private bool invertV = false; // V eksenini ters çevir
    [SerializeField] private bool swapUV = false; // U ve V'yi yer değiştir (Build fix için)
    [SerializeField] private bool autoSwapInBuild = true; // Build'de otomatik swap (Editor'de normal, Build'de swap)
    [SerializeField] private bool autoInvertVInBuild = true; // Build'de V eksenini otomatik ters çevir (alt-üst fix)

    // UV eksen seçenekleri
    public enum UVAxisMode
    {
        Auto,       // Otomatik tespit
        XY,         // X→U, Y→V (Z sabit)
        XZ,         // X→U, Z→V (Y sabit)
        YZ,         // Y→U, Z→V (X sabit)
        ZY,         // Z→U, Y→V (X sabit) ← Muhtemelen bu!
        YX,         // Y→U, X→V (Z sabit)
        ZX          // Z→U, X→V (Y sabit)
    }

    // Events
    public delegate void PaintEvent(RaycastHit hit, Vector2 uv);
    public event PaintEvent OnPaintAtPosition;

    private bool isPainting = false;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!isPaintingEnabled) return;

        HandleInput();
    }

    private void HandleInput()
    {
        // Mouse Input (Editor ve Standalone)
        if (Input.GetMouseButton(0))
        {
            // UI üzerinde mi kontrol et (joystick, butonlar vs)
            if (IsPointerOverUI())
            {
                isPainting = false;
                return;
            }

            Vector2 screenPosition = Input.mousePosition;
            PerformRaycast(screenPosition);
            isPainting = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isPainting = false;
        }

        // Touch Input (Mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            // UI üzerinde mi kontrol et
            if (IsPointerOverUI(touch.fingerId))
            {
                isPainting = false;
                return;
            }

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || touch.phase == TouchPhase.Began)
            {
                PerformRaycast(touch.position);
                isPainting = true;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isPainting = false;
            }
        }
    }

    private bool IsPointerOverUI(int touchId = -1)
    {
        if (EventSystem.current == null)
            return false;

        // Touch için
        if (touchId >= 0)
        {
            return EventSystem.current.IsPointerOverGameObject(touchId);
        }

        // Mouse için
        return EventSystem.current.IsPointerOverGameObject();
    }

    private void PerformRaycast(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[PaintInputController] Main camera is null!");
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Debug ray çiz
        if (showDebugRays)
        {
            Debug.DrawRay(ray.origin, ray.direction * raycastDistance, Color.red, 0.1f);
        }

        if (Physics.Raycast(ray, out hit, raycastDistance, paintableLayer))
        {
            // UV koordinatını al - textureCoord çalışmıyorsa manuel hesapla
            Vector2 uv = hit.textureCoord;

            // Eğer UV (0,0) ise veya NaN ise, manuel hesapla
            if (uv == Vector2.zero || float.IsNaN(uv.x) || float.IsNaN(uv.y))
            {
                uv = CalculateUVFromHitPoint(hit);
            }

            // Debug - hit bilgisi
            if (showDebugRays)
            {
                Debug.DrawLine(ray.origin, hit.point, Color.green, 0.1f);
            }

            // Event tetikle
            OnPaintAtPosition?.Invoke(hit, uv);
        }
        else
        {
            // Debug - miss
            if (showDebugRays && Time.frameCount % 30 == 0) // Her 30 frame'de bir log (spam önleme)
            {
                Debug.Log($"[PaintInputController] Raycast missed! Layer mask: {paintableLayer.value}");
            }
        }
    }


    private Vector2 CalculateUVFromHitPoint(RaycastHit hit)
    {
        // Local space'e çevir
        Vector3 localHitPoint = hit.collider.transform.InverseTransformPoint(hit.point);

        // Mesh Renderer'dan mesh bounds al (collider değil)
        MeshRenderer meshRenderer = hit.collider.GetComponent<MeshRenderer>();
        Bounds bounds = meshRenderer != null ? meshRenderer.bounds : hit.collider.bounds;

        // Local bounds hesapla
        Vector3 localMin = hit.collider.transform.InverseTransformPoint(bounds.min);
        Vector3 localMax = hit.collider.transform.InverseTransformPoint(bounds.max);

        float u, v;
        UVAxisMode mode = uvAxisMode;

        // Auto mode ise otomatik tespit
        if (mode == UVAxisMode.Auto)
        {
            Vector3 size = localMax - localMin;

            // Z ve Y en büyükse → ZY düzlemi
            if (Mathf.Abs(size.z) > Mathf.Abs(size.x) && Mathf.Abs(size.y) > Mathf.Abs(size.x))
            {
                mode = UVAxisMode.ZY;
            }
            // X ve Z en büyükse → XZ düzlemi
            else if (Mathf.Abs(size.x) > Mathf.Abs(size.y) && Mathf.Abs(size.z) > Mathf.Abs(size.y))
            {
                mode = UVAxisMode.XZ;
            }
            // Varsayılan: XY düzlemi
            else
            {
                mode = UVAxisMode.XY;
            }
        }

        // Seçilen moda göre UV hesapla
        switch (mode)
        {
            case UVAxisMode.XY:
                u = Mathf.InverseLerp(localMin.x, localMax.x, localHitPoint.x);
                v = Mathf.InverseLerp(localMin.y, localMax.y, localHitPoint.y);
                break;

            case UVAxisMode.XZ:
                u = Mathf.InverseLerp(localMin.x, localMax.x, localHitPoint.x);
                v = Mathf.InverseLerp(localMin.z, localMax.z, localHitPoint.z);
                break;

            case UVAxisMode.YZ:
                u = Mathf.InverseLerp(localMin.y, localMax.y, localHitPoint.y);
                v = Mathf.InverseLerp(localMin.z, localMax.z, localHitPoint.z);
                break;

            case UVAxisMode.ZY:
                u = Mathf.InverseLerp(localMin.z, localMax.z, localHitPoint.z);
                v = Mathf.InverseLerp(localMin.y, localMax.y, localHitPoint.y);
                break;

            case UVAxisMode.YX:
                u = Mathf.InverseLerp(localMin.y, localMax.y, localHitPoint.y);
                v = Mathf.InverseLerp(localMin.x, localMax.x, localHitPoint.x);
                break;

            case UVAxisMode.ZX:
                u = Mathf.InverseLerp(localMin.z, localMax.z, localHitPoint.z);
                v = Mathf.InverseLerp(localMin.x, localMax.x, localHitPoint.x);
                break;

            default:
                u = 0.5f;
                v = 0.5f;
                break;
        }

        // Clamp 0-1 arası (invert'ten önce)
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        // Invert işlemleri
        bool shouldInvertU = invertU;
        bool shouldInvertV = invertV;

        // Auto invert: Build'de V'yi ters çevir (alt-üst fix)
        if (autoInvertVInBuild)
        {
#if UNITY_EDITOR
            shouldInvertV = invertV; // Editor'de manuel ayara uy
#else
            shouldInvertV = true;    // Build'de V'yi ters çevir
#endif
        }

        if (shouldInvertU) u = 1f - u;
        if (shouldInvertV) v = 1f - v;

        // Swap UV (Editor vs Build fix için)
        bool shouldSwap = swapUV;

        // Auto swap: Build'de swap yap, Editor'de yapma
        if (autoSwapInBuild)
        {
#if UNITY_EDITOR
            shouldSwap = false; // Editor'de swap yapma
#else
            shouldSwap = true;  // Build'de swap yap
#endif
        }

        if (shouldSwap)
        {
            float temp = u;
            u = v;
            v = temp;
        }

        // Final clamp
        u = Mathf.Clamp01(u);
        v = Mathf.Clamp01(v);

        // Debug
        if (showDebugRays && Time.frameCount % 30 == 0)
        {
            Debug.Log($"[UV Debug] Mode: {mode} | U: {u:F2} | V: {v:F2} | InvertU: {shouldInvertU} | InvertV: {shouldInvertV} | SwapUV: {shouldSwap}");
        }

        return new Vector2(u, v);
    }


    public void SetPaintingEnabled(bool enabled)
    {
        isPaintingEnabled = enabled;
        if (!enabled)
        {
            isPainting = false;
        }
        Debug.Log($"[PaintInputController] Painting {(enabled ? "enabled" : "disabled")}");
    }

    public bool IsPainting()
    {
        return isPainting;
    }
}
