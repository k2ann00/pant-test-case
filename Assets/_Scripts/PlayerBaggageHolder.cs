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
        baggage.transform.SetParent(baggageStackRoot);
        baggage.transform.localPosition = new Vector3(0, bags.Count * bagHeightStep, 0);
        baggage.transform.localRotation = Quaternion.Euler(0, 0, 90); // ✨
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
}
