using System.Collections.Generic;
using UnityEngine;

public class PlayerBaggageHolder : MonoBehaviour
{
    [Header("References")]
    public Transform baggageStackRoot; // el veya araba pozisyonu
    public float bagHeightStep = 0.2f;

    private List<GameObject> bags = new();

    public void AddBaggage(GameObject baggage)
    {
        if (baggage == null)
        {
            Debug.LogWarning("[PlayerBaggageHolder] Attempted to add null baggage!");
            return;
        }

        if (baggageStackRoot == null)
        {
            Debug.LogError("[PlayerBaggageHolder] baggageStackRoot is null!");
            return;
        }

        baggage.transform.SetParent(baggageStackRoot);
        baggage.transform.localPosition = new Vector3(0, bags.Count * bagHeightStep, 0);
        baggage.transform.localRotation = Quaternion.Euler(0, 0, 90); // Orijinal rotasyon
        bags.Add(baggage);
    }
    

    public GameObject RemoveBaggage()
    {
        if (bags.Count == 0) return null;
        var bag = bags[^1];
        bags.RemoveAt(bags.Count - 1);
        bag.transform.SetParent(null);
        return bag;
    }


    public Vector3 GetNextStackPosition()
    {
        if (baggageStackRoot == null)
        {
            Debug.LogError("[PlayerBaggageHolder] baggageStackRoot is null in GetNextStackPosition!");
            return Vector3.zero;
        }

        // Local pozisyon hesapla
        Vector3 localPos = new Vector3(0, bags.Count * bagHeightStep, 0);
        // World space'e çevir
        return baggageStackRoot.TransformPoint(localPos);
    }


    public int GetBaggageCount()
    {
        return bags.Count;
    }
}
