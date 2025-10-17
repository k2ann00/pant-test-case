using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class StairsController : MonoBehaviour
{
    public static StairsController Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float moveDuration = 0.3f; // her geçiş süresi
    [SerializeField] private float delayBetweenCycles = 0f; // cycle arası bekleme
    [SerializeField] private Ease moveEase = Ease.Linear;
    [SerializeField] private bool autoStart = true;

    [Header("References")]
    [SerializeField] public  List<Transform> steps = new List<Transform>();
    private bool isRunning;

    void Awake () => Instance = this;
    private void Start()
    {
        if (autoStart)
            StartMoving();
    }

    [ContextMenu("Start Moving")]
    public void StartMoving()
    {
        if (isRunning || steps.Count < 2)
            return;

        isRunning = true;
        StartCoroutine(CycleRoutine());
    }

    private System.Collections.IEnumerator CycleRoutine()
    {
        while (isRunning)
        {
            MoveAllStepsOnce();
            yield return new WaitForSeconds(moveDuration + delayBetweenCycles);
        }
    }

    private void MoveAllStepsOnce()
    {
        if (steps == null || steps.Count == 0) return;

        Vector3 firstStepPos = steps[0].position;
        Vector3 lastStepPos = steps[steps.Count - 1].position;

        // hedef pozisyonları önceden al
        Vector3[] targetPositions = new Vector3[steps.Count];
        for (int i = 0; i < steps.Count; i++)
        {
            int nextIndex = (i + 1) % steps.Count;
            targetPositions[i] = steps[nextIndex].position;
        }

        // DOTween hareketi
        for (int i = 0; i < steps.Count; i++)
        {
            Transform step = steps[i];

            

            step.DOMove(targetPositions[i], moveDuration)
                .SetEase(moveEase)
                .SetId($"StepTween_{i}");
        }
    }



    [ContextMenu("Stop Moving")]
    public void StopMoving()
    {
        isRunning = false;
        DOTween.KillAll();
    }
}
