using UnityEngine;

public interface ITransferTarget
{
    TransferSettings GetTransferSettings();
    void OnTransferProgress(float fill, int remaining);
    void OnTransferCompleted();
}

[System.Serializable]
public struct TransferSettings
{
    public string unlockId; // unique id for this unlockable area
    public int upgradeCost;
    public int transferSpeed;
    public int coinValue;
    public float coinTravelDuration;
    public float jumpPower;
    public int jumpCount;
    public float scaleUp;
    public GameObject moneyPrefab;
    public Transform moneyTarget;
    public Vector3 spawnOffset;
}
