using System.Collections.Generic;
using UnityEngine;

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


    public int Count => baggageStack.Count;


    public bool IsEmpty => baggageStack.Count == 0;


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

        Debug.Log($"[{name}] Added {baggage.name} to stack. Total: {baggageStack.Count}");
    }


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
            Debug.Log($"[{name}] Removed {baggage.name} from top. Remaining: {baggageStack.Count}");
        }

        return baggage;
    }


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
            Debug.Log($"[{name}] Removed {baggage.name} from bottom. Remaining: {baggageStack.Count}");
        }

        // Kalan bavulları aşağı kaydır
        UpdateStackPositions();

        return baggage;
    }


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
        Debug.Log($"[{name}] Stack cleared");
    }


    public List<GameObject> GetAll()
    {
        return new List<GameObject>(baggageStack);
    }


    public GameObject GetAt(int index)
    {
        if (index < 0 || index >= baggageStack.Count)
            return null;

        return baggageStack[index];
    }

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


    private Vector3 GetStackPosition(int index)
    {
        return transform.position + (stackDirection.normalized * baggageSpacing * index);
    }


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
