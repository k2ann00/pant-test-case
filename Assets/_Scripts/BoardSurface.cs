using UnityEngine;


[RequireComponent(typeof(MeshRenderer))]
public class BoardSurface : MonoBehaviour
{
    [Header("Texture Settings")]
    [SerializeField] private int textureWidth = 1024;
    [SerializeField] private int textureHeight = 1024;
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private float paintThreshold = 0.85f; // %85 boyama → %100 sayılır

    [Header("References")]
    [SerializeField] private Shader paintShader; // Build'de shader kaybını önlemek için
    private MeshRenderer meshRenderer;
    private RenderTexture renderTexture;
    private Texture2D paintTexture;
    private Material paintMaterial;

    // Boyama tracking
    private int totalPixels;
    private int paintedPixels;
    private Color[] originalPixels;

    // %60 boyama → %100 göster (threshold ile normalize)
    public float PaintedPercentage
    {
        get
        {
            if (totalPixels == 0) return 0f;

            float rawPercentage = (paintedPixels / (float)totalPixels);

            // Threshold ile normalize et: %60 boyama → %100 göster
            float normalizedPercentage = Mathf.Clamp01(rawPercentage / paintThreshold) * 100f;

            return normalizedPercentage;
        }
    }

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        InitializePaintingSurface();
    }

    private void InitializePaintingSurface()
    {
        // Texture2D oluştur (CPU-side, paint için)
        paintTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        paintTexture.filterMode = FilterMode.Bilinear;
        paintTexture.wrapMode = TextureWrapMode.Clamp;

        // Başlangıçta beyaz arka plan
        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        paintTexture.SetPixels(pixels);
        paintTexture.Apply();

        // Original pixels'i kaydet (percentage tracking için)
        originalPixels = (Color[])pixels.Clone();
        totalPixels = pixels.Length;
        paintedPixels = 0;

        // Material oluştur ve texture'ı ata
        // Inspector'dan shader atanmışsa onu kullan, yoksa Shader.Find ile bul
        Shader shaderToUse = paintShader != null ? paintShader : Shader.Find("Unlit/Texture");

        if (shaderToUse == null)
        {
            Debug.LogError("[BoardSurface] Shader not found! Please assign 'Unlit/Texture' shader in Inspector!");
            shaderToUse = Shader.Find("Standard"); // Fallback
        }

        paintMaterial = new Material(shaderToUse);
        paintMaterial.mainTexture = paintTexture;
        meshRenderer.material = paintMaterial;

        Debug.Log($"[BoardSurface] Initialized {textureWidth}x{textureHeight} painting surface with shader: {shaderToUse.name}");
    }

    public void Paint(Vector2 uv, Color color, int brushSize = 10)
    {
        if (paintTexture == null) return;

        // UV'yi pixel koordinatına çevir
        int x = Mathf.FloorToInt(uv.x * textureWidth);
        int y = Mathf.FloorToInt(uv.y * textureHeight);

        // Fırça boyutuna göre çevredeki pixelleri de boya (circle brush)
        int radius = brushSize / 2;
        int newlyPaintedPixels = 0;

        for (int offsetX = -radius; offsetX <= radius; offsetX++)
        {
            for (int offsetY = -radius; offsetY <= radius; offsetY++)
            {
                // Circle brush için mesafe kontrolü
                if (offsetX * offsetX + offsetY * offsetY > radius * radius)
                    continue;

                int pixelX = x + offsetX;
                int pixelY = y + offsetY;

                // Sınır kontrolü
                if (pixelX < 0 || pixelX >= textureWidth || pixelY < 0 || pixelY >= textureHeight)
                    continue;

                // Pixel index
                int pixelIndex = pixelY * textureWidth + pixelX;

                // Eğer bu pixel daha önce boyanmamışsa, sayacı artır
                Color currentColor = paintTexture.GetPixel(pixelX, pixelY);
                if (ColorEquals(currentColor, backgroundColor))
                {
                    newlyPaintedPixels++;
                }

                // Pixel'i boya
                paintTexture.SetPixel(pixelX, pixelY, color);
            }
        }

        // Texture'ı güncelle (Apply çağrısı GPU'ya yükler)
        paintTexture.Apply();

        // Boyanan pixel sayısını güncelle
        paintedPixels += newlyPaintedPixels;
    }


    private bool ColorEquals(Color a, Color b, float threshold = 0.01f)
    {
        return Mathf.Abs(a.r - b.r) < threshold &&
               Mathf.Abs(a.g - b.g) < threshold &&
               Mathf.Abs(a.b - b.b) < threshold;
    }


    public void ClearSurface()
    {
        if (paintTexture == null) return;

        Color[] pixels = new Color[textureWidth * textureHeight];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = backgroundColor;
        }
        paintTexture.SetPixels(pixels);
        paintTexture.Apply();

        paintedPixels = 0;
        Debug.Log("[BoardSurface] Surface cleared");
    }

    public Texture2D GetPaintTexture()
    {
        return paintTexture;
    }

    /// <summary>
    /// 1024x1024 resmi, 1920x1080 boyutundaki resimlerin ortasına yerleştir (background color ile doldur)
    /// Resim genişletilmez, olduğu gibi tutulur - sadece yatay olarak centered
    /// </summary>
    public Texture2D GetResizedPaintTexture()
    {
        if (paintTexture == null) return null;

        // Yeni boyut: 1920x1080
        int newWidth = 1920;
        int newHeight = 1080;

        // Yeni texture oluştur (background color ile dolu)
        Texture2D resizedTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
        Color[] newPixels = new Color[newWidth * newHeight];

        // Tüm pixelleri background color ile doldur
        for (int i = 0; i < newPixels.Length; i++)
        {
            newPixels[i] = Color.black;
        }
        resizedTexture.SetPixels(newPixels);

        // Orijinal resmi ortaya yerleştir
        // X ekseni: (1920 - 1024) / 2 = 448 (yatay center)
        // Y ekseni: (1080 - 1024) / 2 = 28 (dikey center)
        int offsetX = (newWidth - textureWidth) / 2;
        int offsetY = (newHeight - textureHeight) / 2;

        // Orijinal texture'ın pixellerini al
        Color[] originalPixels = paintTexture.GetPixels();

        // Orijinal pixelleri yeni texture'ın ortasına yerleştir (genişletmeden)
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                int originalIndex = y * textureWidth + x;
                int newX = offsetX + x;
                int newY = offsetY + y; // Y eksenini normal tut
                int newIndex = newY * newWidth + newX;

                if (newIndex >= 0 && newIndex < newPixels.Length)
                {
                    newPixels[newIndex] = originalPixels[originalIndex];
                }
            }
        }

        resizedTexture.SetPixels(newPixels);
        resizedTexture.Apply();

        Debug.Log($"[BoardSurface] Canvas created: {newWidth}x{newHeight}, Original painting ({textureWidth}x{textureHeight}) centered at offset ({offsetX}, {offsetY})");

        return resizedTexture;
    }

    private void OnDestroy()
    {
        // Memory leak önleme
        if (paintTexture != null)
        {
            Destroy(paintTexture);
        }
        if (paintMaterial != null)
        {
            Destroy(paintMaterial);
        }
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
    }
}
