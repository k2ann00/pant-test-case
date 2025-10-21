using System.Collections;
using UnityEngine;
using System.IO;

public class DrawingEmailSender : MonoBehaviour
{
    public void SendDrawingToEmail()
    {
        StartCoroutine(ProcessAndSendEmail());
    }

    IEnumerator ProcessAndSendEmail()
    {
        yield return new WaitForEndOfFrame();

        // BoardSurface'den PNG'yi al
        PaintingManager paintingManager = PaintingManager.Instance;
        if (paintingManager == null)
        {
            Debug.LogError("PaintingManager bulunamadı!");
            yield break;
        }

        BoardSurface boardSurface = paintingManager.GetBoardSurface();
        if (boardSurface == null)
        {
            Debug.LogError("BoardSurface bulunamadı!");
            yield break;
        }

        // Resized texture'ı al (1920x1080)
        Texture2D resizedTexture = boardSurface.GetResizedPaintTexture();
        if (resizedTexture == null)
        {
            Debug.LogError("Texture alınamadı!");
            yield break;
        }

        // PNG'ye encode et
        byte[] pngBytes = resizedTexture.EncodeToPNG();

        // Masaüstüne kaydet
        string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string fileName = $"Suprize_From_k2ann00_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
        string filePath = Path.Combine(desktopPath, fileName);

        File.WriteAllBytes(filePath, pngBytes);
        Debug.Log("✅ PNG kaydedildi: " + filePath);

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

        Debug.Log("📧 Gmail açıldı. Dosyayı sürükleyip ekleyebilirsin!");

        // Texture'ı temizle
        Destroy(resizedTexture);

        // Oyunu kapat
        yield return new WaitForSeconds(2f);
        Application.Quit();
    }
}