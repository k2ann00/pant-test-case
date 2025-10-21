using UnityEngine;


public class PaintingManager : MonoBehaviour
{
    public static PaintingManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BoardSurface boardSurface;
    [SerializeField] private PaintInputController inputController;
    [SerializeField] private Transform boardTransform; // Board objesi

    [Header("Paint Settings")]
    [SerializeField] private Color currentColor = Color.white;
    [SerializeField] private int brushSize = 30;

    [Header("Available Colors (4 colors)")]
    [SerializeField] private Color whiteColor = new Color(1f, 1f, 1f);           // White #FFFFFF
    [SerializeField] private Color orangeColor = new Color(0.95f, 0.4f, 0.14f); // Orange #F26624
    [SerializeField] private Color greyColor = new Color(0.2f, 0.2f, 0.2f);     // Grey #343434
    [SerializeField] private Color blackColor = new Color(0f, 0f, 0f);          // Black #000000

    private bool isPaintingModeActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Board unlock event'ini dinle
        EventBus.BoardUnlocked += OnBoardUnlocked;

        // Input event'ini dinle
        if (inputController != null)
        {
            inputController.OnPaintAtPosition += OnPaintAtPosition;
        }
    }


    private void OnDisable()
    {
        EventBus.BoardUnlocked -= OnBoardUnlocked;

        if (inputController != null)
        {
            inputController.OnPaintAtPosition -= OnPaintAtPosition;
        }
    }


    private void OnBoardUnlocked(Transform board)
    {
        Debug.Log("[PaintingManager] Board unlocked! Activating painting mode...");

        // Board transform'u kaydet
        boardTransform = board;

        // Boyama modunu aktif et
        StartPaintingMode();
    }


    public void StartPaintingMode()
    {
        isPaintingModeActive = true;

        // Input controller'ı aktif et
        if (inputController != null)
        {
            inputController.SetPaintingEnabled(true);
        }

        // Player kontrollerini devre dışı bırak
        DisablePlayerControls();

        Debug.Log("[PaintingManager] Painting mode started!");
    }

    public void StopPaintingMode()
    {
        isPaintingModeActive = false;

        // Input controller'ı deaktif et
        if (inputController != null)
        {
            inputController.SetPaintingEnabled(false);
        }

        // Player kontrollerini aktif et
        EnablePlayerControls();

        // Kamerayı geri döndür
        if (CameraController.Instance != null)
        {
            CameraController.Instance.ReturnToPlayerFollow();
        }

        Debug.Log("[PaintingManager] Painting mode stopped!");
    }


    private void OnPaintAtPosition(RaycastHit hit, Vector2 uv)
    {
        if (!isPaintingModeActive) return;
        if (boardSurface == null) return;

        // Board'a boyama yap
        boardSurface.Paint(uv, currentColor, brushSize);
    }


    public void SetColor(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0: // White
                currentColor = whiteColor;
                Debug.Log("[PaintingManager] Color changed to White");
                break;
            case 1: // Orange
                currentColor = orangeColor;
                Debug.Log("[PaintingManager] Color changed to Orange");
                break;
            case 2: // Grey
                currentColor = greyColor;
                Debug.Log("[PaintingManager] Color changed to Grey");
                break;
            case 3: // Black
                currentColor = blackColor;
                Debug.Log("[PaintingManager] Color changed to Black");
                break;
        }
    }


    public void SetBrushSize(int size)
    {
        brushSize = Mathf.Clamp(size, 5, 180);
        Debug.Log($"[PaintingManager] Brush size changed to {brushSize}");
    }


    public float GetPaintedPercentage()
    {
        if (boardSurface == null) return 0f;
        return boardSurface.PaintedPercentage;
    }


    private void DisablePlayerControls()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
            Debug.Log("[PaintingManager] Player controls disabled");
        }

        // Joystick'i de devre dışı bırak
        FloatingJoystick joystick = FindObjectOfType<FloatingJoystick>();
        if (joystick != null)
        {
            joystick.gameObject.SetActive(false);
            Debug.Log("[PaintingManager] Joystick disabled");
        }
    }


    private void EnablePlayerControls()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = true;
            Debug.Log("[PaintingManager] Player controls enabled");
        }

        // Joystick'i tekrar aktif et
        FloatingJoystick joystick = FindObjectOfType<FloatingJoystick>();
        if (joystick != null)
        {
            joystick.gameObject.SetActive(true);
            Debug.Log("[PaintingManager] Joystick enabled");
        }
    }


    public Color GetCurrentColor()
    {
        return currentColor;
    }


    public int GetBrushSize()
    {
        return brushSize;
    }


    public BoardSurface GetBoardSurface()
    {
        return boardSurface;
    }
}
