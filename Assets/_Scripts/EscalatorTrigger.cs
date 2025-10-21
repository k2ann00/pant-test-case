using UnityEngine;


public class EscalatorTrigger : MonoBehaviour
{
    [Header("Trigger Type")]
    [SerializeField] private bool isStartPoint = true; // true: StartPoint, false: EndPoint

    [Header("References")]
    [SerializeField] private Transform endPointTransform; // StartPoint ise: hedef pozisyon


    public Vector3 GetTargetPosition()
    {
        if (isStartPoint && endPointTransform != null)
        {
            return endPointTransform.position;
        }

        Debug.LogWarning($"[EscalatorTrigger] GetTargetPosition called but endPointTransform is null or this is not a StartPoint!");
        return transform.position;
    }

    
    public bool IsStartPoint => isStartPoint;

    private void OnDrawGizmos()
    {
        if (isStartPoint && endPointTransform != null)
        {
            // Başlangıç noktası: Yeşil
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            // Hedef noktası: Kırmızı
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(endPointTransform.position, 0.5f);

            // Bağlantı çizgisi: Sarı
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, endPointTransform.position);
        }
        else if (!isStartPoint)
        {
            // Bitiş noktası: Mavi
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
