using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Collections;


public class PaintingUI : MonoBehaviour
{
    // Windows API için P/Invoke
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 0x0014;
    private const int SPIF_UPDATEINIFILE = 0x01;
    private const int SPIF_SENDCHANGE = 0x02;
    [Header("References")]
    [SerializeField] private PaintingManager paintingManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject moneyUIPanel; // Money UI
    [SerializeField] private GameObject paintingUIPanel; // Tüm UI'nin parent'ı
    [SerializeField] private TextMeshProUGUI percentageText; // Boyama yüzdesi
    [SerializeField] private Slider brushSizeSlider; // Fırça boyutu slider'ı

    [Header("Color Buttons (4 colors)")]
    [SerializeField] private Button whiteButton;
    [SerializeField] private Button orangeButton;
    [SerializeField] private Button greyButton;
    [SerializeField] private Button blackButton;

    [Header("Action Buttons")]
    [SerializeField] private Button closeButton; // Boyama modundan çık
    [SerializeField] private TextMeshProUGUI closeButtonText; // Close button'un text'i
    [SerializeField] private TextMeshProUGUI successText; // SURPIZE!

    [Header("Settings")]
    [SerializeField] private int minBrushSize = 5;
    [SerializeField] private int maxBrushSize = 180; // Slider %100 → 180
    [SerializeField] private int defaultBrushSize = 30;

    // Click count tracking
    private int exitButtonClickCount = 0;
    private string lastSavedPngPath = ""; // Son kaydedilen PNG dosyasının yolu

    private void Start()
    {
        // PaintingManager referansı
        if (paintingManager == null)
            paintingManager = PaintingManager.Instance;

        // UI button event'lerini bağla
        SetupUIEvents();

        // Başlangıçta UI'yi gizle
        if (paintingUIPanel != null)
            paintingUIPanel.SetActive(false);

        // Slider ayarları
        if (brushSizeSlider != null)
        {
            brushSizeSlider.minValue = minBrushSize;
            brushSizeSlider.maxValue = maxBrushSize;
            brushSizeSlider.value = defaultBrushSize;
        }
    }

    private void OnEnable()
    {
        // Board unlock event'ini dinle
        EventBus.BoardUnlocked += OnBoardUnlocked;
    }

    private void OnDisable()
    {
        EventBus.BoardUnlocked -= OnBoardUnlocked;
    }

    private void Update()
    {
        // Boyama yüzdesini güncelle
        UpdatePercentageText();
    }


    private void OnBoardUnlocked(Transform board)
    {
        ShowPaintingUI();
    }


    private void SetupUIEvents()
    {
        // Renk butonları (4 renk)
        if (whiteButton != null)
            whiteButton.onClick.AddListener(() => OnColorButtonClicked(0));

        if (orangeButton != null)
            orangeButton.onClick.AddListener(() => OnColorButtonClicked(1));

        if (greyButton != null)
            greyButton.onClick.AddListener(() => OnColorButtonClicked(2));

        if (blackButton != null)
            blackButton.onClick.AddListener(() => OnColorButtonClicked(3));

        // Fırça boyutu slider
        if (brushSizeSlider != null)
            brushSizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);

        // Close button
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }


    private void OnColorButtonClicked(int colorIndex)
    {
        if (paintingManager != null)
        {
            paintingManager.SetColor(colorIndex);
            Debug.Log($"[PaintingUI] Color {colorIndex} selected");
        }
    }


    private void OnBrushSizeChanged(float value)
    {
        if (paintingManager != null)
        {
            int brushSize = Mathf.RoundToInt(value);
            paintingManager.SetBrushSize(brushSize);
        }
    }


    private void OnCloseButtonClicked()
    {
        exitButtonClickCount++;

        switch (exitButtonClickCount)
        {
            case 1:
                UpdateCloseButtonText("Çizim Tamam Değil Mi?");
                Debug.Log("[PaintingUI] Exit button clicked - Count: 1");
                break;

            case 2:
                UpdateCloseButtonText("Eminizdir Umarım");
                Debug.Log("[PaintingUI] Exit button clicked - Count: 2");
                break;

            case 3:
                UpdateCloseButtonText("Valla Benden Günah Gitti");
                Debug.Log("[PaintingUI] Exit button clicked - Count: 3");
                break;

            case 4:
                UpdateCloseButtonText("SAVE PNG");
                Debug.Log("[PaintingUI] Exit button clicked - Count: 4 - Saving PNG...");
                StartSaveSystem(setAsWallpaper: false);
                break;

            case 5:
                UpdateCloseButtonText("SET WALLPAPER");
                Debug.Log("[PaintingUI] Exit button clicked - Count: 5 - Setting as wallpaper...");
                SetExistingFileAsWallpaper();
                StartCoroutine(QuitTheGame());
                break;

            default:
                // 5'ten fazla tıklanırsa normal close işlemi yap
                if (paintingManager != null)
                {
                    paintingManager.StopPaintingMode();
                }
                HidePaintingUI();
                break;
        }
    }

    private IEnumerator QuitTheGame()
    {
        successText.gameObject.SetActive(true);
#if !UNITY_STANDALONE_WIN && UNITY_EDITOR
        successText.text = "WINDOWS BUILD'DE DENEYIN!";
#endif
        SendDrawingEmail();
        yield return new WaitForSeconds(3f);
        Application.Quit();
    }

    private void SendDrawingEmail()
    {
        StartCoroutine(SendEmailProcess());
    }

    private IEnumerator SendEmailProcess()
    {
        yield return new WaitForEndOfFrame();

        BoardSurface boardSurface = paintingManager.GetBoardSurface();
        if (boardSurface == null)
        {
            Debug.LogError("[PaintingUI] BoardSurface bulunamadı!");
            yield break;
        }

        // Resized texture'ı al (1920x1080)
        Texture2D resizedTexture = boardSurface.GetResizedPaintTexture();
        if (resizedTexture == null)
        {
            Debug.LogError("[PaintingUI] Texture alınamadı!");
            yield break;
        }

        // PNG'ye encode et
        byte[] pngBytes = resizedTexture.EncodeToPNG();

        // Masaüstüne kaydet
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = $"Suprize_From_k2ann00_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string filePath = Path.Combine(desktopPath, fileName);

        bool saveSuccess = false;
        string errorMessage = "";

        // Try-catch bloğu (yield return olmadan)
        try
        {
            File.WriteAllBytes(filePath, pngBytes);
            Debug.Log($"[PaintingUI] ✅ PNG kaydedildi: {filePath}");
            saveSuccess = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PaintingUI] Hata: {e.Message}");
            errorMessage = e.Message;
            UpdateCloseButtonText("ERROR!");
        }

        // Texture'ı temizle
        if (resizedTexture != null)
        {
            Destroy(resizedTexture);
        }

        // Eğer kayıt başarısızsa, burada dur
        if (!saveSuccess)
        {
            yield break;
        }

        // Windows Explorer'da dosyayı göster
        System.Diagnostics.Process.Start("explorer.exe", "/select," + filePath);

        yield return new WaitForSeconds(0.5f);

        // Gmail taslak aç
        string email = "kaanavdan01@gmail.com";
        string subject = "Unity Case - Çizimim";

        string body = "[Eğer feedbackte bulunmamayı ve çiziminizi paylaşmayı istemezseniz BU SEKMEYI KAPABILIRSINIZ]%0A%0A" +
                      "[Case başarıyla tamamlandı ve çizimim aşağıda:]%0A" +
                      "(PNG dosyası masaüstünde: " + fileName + ")%0A%0A" +
                      "FEEDBACKS:%0A%0A";

        string gmailUrl = $"https://mail.google.com/mail/?view=cm&fs=1&to={email}&su={subject}&body={body}";

        Application.OpenURL(gmailUrl);

        UpdateCloseButtonText("EMAIL OPENED!");
        Debug.Log("[PaintingUI] 📧 Gmail açıldı!");

        // Success mesajını göster
        if (successText != null)
        {
            successText.gameObject.SetActive(true);
            successText.text = "GMAIL AÇILDI!\nDOSYAYI SÜRÜKLE-BIRAK İLE EKLEYEBİLİRSİN!";
        }
    }

    private void UpdateCloseButtonText(string newText)
    {
        if (closeButtonText != null)
        {
            closeButtonText.text = newText;
            Debug.Log($"[PaintingUI] Close button text changed to: {newText}");
        }
    }

    private void StartSaveSystem(bool setAsWallpaper = false)
    {
        Debug.Log($"[PaintingUI] Save system started!");

        if (paintingManager == null)
        {
            Debug.LogError("[PaintingUI] PaintingManager is null!");
            return;
        }

        BoardSurface boardSurface = paintingManager.GetBoardSurface();
        if (boardSurface == null)
        {
            Debug.LogError("[PaintingUI] BoardSurface is null!");
            return;
        }

        // Resized texture'ı al (1920x1080, background color ile)
        Texture2D resizedTexture = boardSurface.GetResizedPaintTexture();
        if (resizedTexture == null)
        {
            Debug.LogError("[PaintingUI] Resized texture is null!");
            return;
        }

        Debug.Log($"[PaintingUI] Resized texture ready for save: {resizedTexture.width}x{resizedTexture.height}");

        // 1. PNG'ye encode et
        byte[] pngBytes = resizedTexture.EncodeToPNG();
        if (pngBytes == null || pngBytes.Length == 0)
        {
            Debug.LogError("[PaintingUI] Failed to encode texture to PNG!");
            Destroy(resizedTexture);
            return;
        }

        Debug.Log($"[PaintingUI] PNG encoded successfully - Size: {pngBytes.Length} bytes");

        // 2. Desktop'a kaydet - dosya ismi: Suprize_From_k2ann00...
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = $"Surprise_From_k2ann00_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string fullPath = Path.Combine(desktopPath, fileName);

        try
        {
            File.WriteAllBytes(fullPath, pngBytes);
            Debug.Log($"[PaintingUI] PNG saved to: {fullPath} ({resizedTexture.width}x{resizedTexture.height})");

            // Kaydedilen dosyanın yolunu sakla
            lastSavedPngPath = fullPath;

            // Sadece PNG kayıt
            UpdateCloseButtonText("PNG SAVED!");

            // Başarı mesajı
            Debug.Log("[PaintingUI] ✓ Save completed successfully!");

        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PaintingUI] Failed to save PNG: {e.Message}");
            UpdateCloseButtonText("SAVE FAILED!");
        }
        finally
        {
            // Resized texture'ı temizle (memory leak önleme)
            Destroy(resizedTexture);
        }
    }

    /// <summary>
    /// Önceden kaydedilmiş PNG dosyasını wallpaper olarak ayarla (tekrar kaydetmeden)
    /// </summary>
    private void SetExistingFileAsWallpaper()
    {
        if (string.IsNullOrEmpty(lastSavedPngPath))
        {
            Debug.LogWarning("[PaintingUI] PNG dosyası kaydedilmedi! Önce 4 kez tıkla.");
            UpdateCloseButtonText("SAVE PNG FIRST!");
            return;
        }

        if (!File.Exists(lastSavedPngPath))
        {
            Debug.LogError($"[PaintingUI] PNG dosyası bulunamadı: {lastSavedPngPath}");
            UpdateCloseButtonText("PNG NOT FOUND!");
            return;
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        SetWindowsWallpaper(lastSavedPngPath);
        UpdateCloseButtonText("WALLPAPER SET!");
#else
        Debug.Log("[PaintingUI] Wallpaper ayarlama sadece Windows build'de çalışır");
        UpdateCloseButtonText("WIN BUILDDE DENE");
#endif
    }

    /// <summary>
    /// Windows wallpaper'ı ayarla (sadece Windows build'de çalışır)
    /// </summary>
    private void SetWindowsWallpaper(string imagePath)
    {
        try
        {
            // SystemParametersInfo ile wallpaper ayarla
            int result = SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, imagePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

            if (result != 0)
            {
                Debug.Log($"[PaintingUI] ✓ Wallpaper set successfully: {imagePath}");
            }
            else
            {
                Debug.LogWarning("[PaintingUI] SystemParametersInfo returned 0 - wallpaper might not be set");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[PaintingUI] Failed to set wallpaper: {e.Message}");
        }
    }


    private void UpdatePercentageText()
    {
        if (percentageText == null || paintingManager == null) return;

        float percentage = paintingManager.GetPaintedPercentage();
        percentageText.text = $"{percentage:F1}%";

        // %95 ve üzeri olunca close button'u aktif et
        if (closeButton != null && percentage >= 95f && !closeButton.gameObject.activeSelf)
        {
            closeButton.gameObject.SetActive(true);
            Debug.Log("[PaintingUI] Close button activated - Painting >= 95%");
        }
    }


    public void ShowPaintingUI()
    {
        if (paintingUIPanel != null)
        {
            paintingUIPanel.SetActive(true);
            Debug.Log("[PaintingUI] Painting UI shown");
        }

        if (moneyUIPanel != null)
        {
            moneyUIPanel.SetActive(false);
            Debug.Log("[PaintingUI] Money UI hidden");
        }
    }


    public void HidePaintingUI()
    {
        if (paintingUIPanel != null)
        {
            paintingUIPanel.SetActive(false);
            Debug.Log("[PaintingUI] Painting UI hidden");
        }
    }
}
