using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bavulları stack şeklinde tutan container
/// SOLID: Single Responsibility - Sadece stack yönetimi
/// Pattern: Reusable component - Start position, Truck gibi birden fazla yerde kullanılabilir
/// </summary>
public class BaggageStack : MonoBehaviour
{
    [Header("Stack Settings")]
    [SerializeField] private Vector3 stackDirection = Vector3.up; // Hangi yönde stacklenecek (genelde Y)
    [SerializeField] private float baggageSpacing = 0.3f; // Bavullar arası mesafe
    [SerializeField] private bool autoParent = true; // Stack'e eklenince parent olsun mu?

    [Header("Visual Settings (Optional)")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private List<GameObject> baggageStack = new List<GameObject>();

    /// <summary>
    /// Bavul sayısı
    /// </summary>
    public int Count => baggageStack.Count;

    /// <summary>
    /// Stack boş mu?
    /// </summary>
    public bool IsEmpty => baggageStack.Count == 0;

    /// <summary>
    /// Stack'e bavul ekle (en üste)
    /// </summary>
    public void AddBaggage(GameObject baggage)
    {
        if (baggage == null)
        {
            Debug.LogWarning($"[{name}] Tried to add null baggage!");
            return;
        }

        baggageStack.Add(baggage);

        // Pozisyon hesapla (stack index'e göre)
        Vector3 targetPosition = GetStackPosition(baggageStack.Count - 1);
        baggage.transform.position = targetPosition;
        baggage.transform.rotation = Quaternion.identity;

        // Parent yap (isteğe bağlı)
        if (autoParent)
        {
            baggage.transform.SetParent(transform);
        }

        Debug.Log($"📦 [{name}] Added {baggage.name} to stack. Total: {baggageStack.Count}");
    }

    /// <summary>
    /// Stack'ten bavul çıkar (en üstteki)
    /// </summary>
    public GameObject RemoveFromTop()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[{name}] Stack is empty, cannot remove!");
            return null;
        }

        int topIndex = baggageStack.Count - 1;
        GameObject baggage = baggageStack[topIndex];
        baggageStack.RemoveAt(topIndex);

        // Parent'ı kaldır
        if (baggage != null)
        {
            baggage.transform.SetParent(null);
            Debug.Log($"📤 [{name}] Removed {baggage.name} from top. Remaining: {baggageStack.Count}");
        }

        return baggage;
    }

    /// <summary>
    /// Stack'ten bavul çıkar (en alttaki) - FIFO (First In First Out)
    /// </summary>
    public GameObject RemoveFromBottom()
    {
        if (IsEmpty)
        {
            Debug.LogWarning($"[{name}] Stack is empty, cannot remove!");
            return null;
        }

        GameObject baggage = baggageStack[0];
        baggageStack.RemoveAt(0);

        // Parent'ı kaldır
        if (baggage != null)
        {
            baggage.transform.SetParent(null);
            Debug.Log($"📤 [{name}] Removed {baggage.name} from bottom. Remaining: {baggageStack.Count}");
        }

        // Kalan bavulları aşağı kaydır
        UpdateStackPositions();

        return baggage;
    }

    /// <summary>
    /// Belirli bir indexten bavul al
    /// </summary>
    public GameObject RemoveAt(int index)
    {
        if (index < 0 || index >= baggageStack.Count)
        {
            Debug.LogWarning($"[{name}] Invalid index {index}!");
            return null;
        }

        GameObject baggage = baggageStack[index];
        baggageStack.RemoveAt(index);

        if (baggage != null)
        {
            baggage.transform.SetParent(null);
        }

        UpdateStackPositions();
        return baggage;
    }

    /// <summary>
    /// Tüm stack'i temizle
    /// </summary>
    public void Clear()
    {
        foreach (var baggage in baggageStack)
        {
            if (baggage != null)
            {
                baggage.transform.SetParent(null);
            }
        }

        baggageStack.Clear();
        Debug.Log($"🗑️ [{name}] Stack cleared");
    }

    /// <summary>
    /// Stack'teki tüm bavulları al (kopyasını döndürür)
    /// </summary>
    public List<GameObject> GetAll()
    {
        return new List<GameObject>(baggageStack);
    }

    /// <summary>
    /// Belirli indexte bavul var mı?
    /// </summary>
    public GameObject GetAt(int index)
    {
        if (index < 0 || index >= baggageStack.Count)
            return null;

        return baggageStack[index];
    }

    /// <summary>
    /// Stack pozisyonlarını güncelle (bavul çıkarıldığında)
    /// </summary>
    private void UpdateStackPositions()
    {
        for (int i = 0; i < baggageStack.Count; i++)
        {
            if (baggageStack[i] != null)
            {
                baggageStack[i].transform.position = GetStackPosition(i);
            }
        }
    }

    /// <summary>
    /// Belirli bir stack index için pozisyon hesapla
    /// </summary>
    private Vector3 GetStackPosition(int index)
    {
        return transform.position + (stackDirection.normalized * baggageSpacing * index);
    }

    /// <summary>
    /// Gizmo çizimi
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmoColor;

        // Base pozisyon
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Stack yönü çizgisi (5 birim)
        Vector3 endPoint = transform.position + (stackDirection.normalized * baggageSpacing * 5);
        Gizmos.DrawLine(transform.position, endPoint);

        // Stack pozisyonları (ilk 10 slot)
        for (int i = 0; i < 10; i++)
        {
            Vector3 slotPos = GetStackPosition(i);
            Gizmos.color = i < baggageStack.Count ? Color.green : Color.gray;
            Gizmos.DrawWireCube(slotPos, Vector3.one * 0.2f);
        }
    }

    /// <summary>
    /// Inspector validation
    /// </summary>
    private void OnValidate()
    {
        if (baggageSpacing <= 0)
        {
            Debug.LogWarning($"[{name}] baggageSpacing must be greater than 0!");
            baggageSpacing = 0.3f;
        }
    }
}
