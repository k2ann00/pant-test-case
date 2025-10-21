using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class MoneyMover : MonoBehaviour
{
    [HideInInspector] public GameObject prefab; // hangi prefab'�n havuzuna iade edilece�ini bilmek i�in
    private Coroutine moveCoroutine;

    // Ba�latmak i�in �a��r: Move(startPos, targetPos, duration)
    public void Move(Vector3 start, Vector3 target, float duration)
    {
        transform.position = start;

        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);


        moveCoroutine = StartCoroutine(MoveRoutine(start, target, duration));
    }

    private IEnumerator MoveRoutine(Vector3 start, Vector3 target, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // iste�e g�re kolayl�k: e�rile�tirme/lerp/smoothstep eklenebilir
            transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        transform.position = target;
        OnArrived();
    }

    private void OnArrived()
    {
        // Havuza geri g�nder
        if (MoneyPool.Instance != null)
            MoneyPool.Instance.Return(prefab, gameObject);
        else
            Destroy(gameObject);
    }
}