using DG.Tweening;
using UnityEngine;


public class BaggageXrayMover : MonoBehaviour
{
    private Tween activeTween;

   
    public void StartXrayPath(Vector3[] pathPoints, float duration)
    {
        if (pathPoints == null || pathPoints.Length == 0)
        {
            Debug.LogError($"[{name}] X-Ray path is empty!");
            return;
        }

        Debug.Log($"[{name}] Starting X-Ray path | Points: {pathPoints.Length} | Duration: {duration}s");

        // DOPath ile smooth hareket
        activeTween = transform.DOPath(pathPoints, duration, PathType.Linear)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                Debug.Log($" [{name}] Completed X-Ray path");
                EventBus.RaiseBaggageCompletedXray(gameObject);
            });
    }


    public void Stop()
    {
        if (activeTween != null && activeTween.IsActive())
        {
            activeTween.Kill();
        }
    }

    private void OnDestroy()
    {
        Stop();
    }
}
