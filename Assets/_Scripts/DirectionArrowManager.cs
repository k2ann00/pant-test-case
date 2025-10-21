using UnityEngine;
using DG.Tweening;

public class DirectionArrowManager : MonoBehaviour
{
    public static DirectionArrowManager Instance;

    [Header("Arrow Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private SpriteRenderer arrowRenderer;
    [SerializeField] private float orbitRadius = 0.7f;
    [SerializeField] private float orbitSpeed = 2f;
    [SerializeField] private float heightOffset = 0.5f;
    [SerializeField] private Quaternion initialRotation = Quaternion.Euler(90f, 0f, -26.4f);

    [Header("Target Positions")]
    [SerializeField] private Transform unlockableArea1Target;
    [SerializeField] private Transform welcomingCircleTarget;
    [SerializeField] private Transform baggageUnloadTarget;
    [SerializeField] private Transform baggageXrayTarget;
    [SerializeField] private Transform stairsTarget;
    [SerializeField] private Transform passengerXrayTarget;
    [SerializeField] private Transform unlockableArea2Target;

    


    private DirectionState currentState = DirectionState.None;
    private Transform currentTarget;
    private bool isArrowActive = false;

    private bool isAllPassengersHandedBaggage;
    private bool isAllBaggageStacked;
    private bool isAllBaggageInPlatform;
    private int passengerXrayCount;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            playerTransform = transform.parent;
        }

        if (arrowTransform != null)
        {
            arrowTransform.rotation = initialRotation;
        }

        if (welcomingCircleTarget != null)
        {
            ShowArrow(unlockableArea1Target, DirectionState.UnlockableArea1);
        }
    }

    private void OnEnable()
    {
        EventBus.PlayerEnteredCircle += OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle += OnPlayerExitedCircle;
    }

    private void OnDisable()
    {
        EventBus.PlayerEnteredCircle -= OnPlayerEnteredCircle;
        EventBus.PlayerExitedCircle -= OnPlayerExitedCircle;
    }

    private void OnPlayerEnteredCircle(CircleType circleType)
    {

        switch (circleType)
        {
            case CircleType.WelcomingCircle:
                PlayerController.Instance.CinematicLookAt(
                    welcomingCircleTarget.position,
                    welcomingCircleTarget.Find("LookAt").position);
                HideArrow();
                break;
            case CircleType.BaggageUnload:
                PlayerController.Instance.CinematicLookAt(
                    baggageUnloadTarget.position,
                    baggageUnloadTarget.Find("LookAt").position);
                HideArrow();
                break;
            case CircleType.BaggageXray:
                PlayerController.Instance.CinematicLookAt(
                    baggageXrayTarget.position,
                    baggageXrayTarget.Find("LookAt").position);
                HideArrow();
                break;
            case CircleType.PassengerXray:
                PlayerController.Instance.CinematicLookAt(
                    passengerXrayTarget.position,
                    passengerXrayTarget.Find("LookAt").position);
                HideArrow();
                break;

            // WelcomingCircle PassengerManager tarafından gizleniyor
            // Diğer circle'lar için arrow görünür kalır
        }
    }

    private void OnPlayerExitedCircle(CircleType circleType)
    {

        switch (circleType)
        {
            case CircleType.WelcomingCircle:
                
                // Tüm passengerlar bagaj verdiyse → BaggageUnload, yoksa WelcomingCircle
                if (isAllPassengersHandedBaggage)
                {
                    ShowArrow(baggageUnloadTarget, DirectionState.BaggageUnload);
                }
                else
                {
                    ShowArrow(welcomingCircleTarget, DirectionState.WelcomingCircle);
                }
                break;

            case CircleType.BaggageUnload:
                
                // Tüm bagajlar stack'lendiyse → BaggageXray, yoksa BaggageUnload
                if (isAllBaggageStacked)
                {
                    ShowArrow(baggageXrayTarget, DirectionState.BaggageXray);
                }
                else
                {
                    ShowArrow(baggageUnloadTarget, DirectionState.BaggageUnload);
                }
                break;

            case CircleType.BaggageXray:
                // Tüm bagajlar platform'a geçtiyse → Stairs, yoksa BaggageXray
                if (isAllBaggageInPlatform)
                {
                    ShowArrow(stairsTarget, DirectionState.Stairs);
                }
                else
                {
                    ShowArrow(baggageXrayTarget, DirectionState.BaggageXray);
                }
                break;

            case CircleType.PassengerXray:
                // Passenger XRay'den çıktıysa - 6 passenger işlendiyse → UnlockableArea2, yoksa PassengerXray
                if (passengerXrayCount >= 6)
                {
                    ShowArrow(unlockableArea2Target, DirectionState.UnlockableArea2);
                }
                else
                {
                    ShowArrow(passengerXrayTarget, DirectionState.PassengerXray);
                }
                break;
        }
    }

    private void Update()
    {
        if (isArrowActive && currentTarget != null && arrowTransform != null)
        {
            RotateArrowTowardsTarget();
        }
    }

    private void RotateArrowTowardsTarget()
    {
        if (playerTransform == null || currentTarget == null || arrowTransform == null) return;

        Vector3 playerPos = playerTransform.position;
        Vector3 targetPos = currentTarget.position;

        Vector3 directionToTarget = targetPos - playerPos;
        directionToTarget.y = 0;

        if (directionToTarget.sqrMagnitude > 0.001f)
        {
            float targetAngle = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float orbitAngle = targetAngle * Mathf.Deg2Rad;

            Vector3 orbitOffset = new Vector3(
                Mathf.Sin(orbitAngle) * orbitRadius,
                heightOffset,
                Mathf.Cos(orbitAngle) * orbitRadius
            );

            Vector3 targetPosition = playerPos + orbitOffset;
            arrowTransform.position = targetPosition;

            Vector3 arrowToTarget = targetPos - arrowTransform.position;
            arrowToTarget.y = 0;

            if (arrowToTarget.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(arrowToTarget.x, arrowToTarget.z) * Mathf.Rad2Deg;

                Quaternion targetRotation = Quaternion.Euler(
                    initialRotation.eulerAngles.x,
                    angle + initialRotation.eulerAngles.y,
                    initialRotation.eulerAngles.z
                );

                arrowTransform.rotation = Quaternion.Slerp(
                    arrowTransform.rotation,
                    targetRotation,
                    orbitSpeed * Time.deltaTime
                );
            }
        }
    }

    private void ShowArrow(Transform target, DirectionState state)
    {
        if (target == null)
        {
            Debug.LogWarning($"[DirectionArrowManager] Target is null for state {state}");
            return;
        }

        currentTarget = target;
        currentState = state;
        isArrowActive = true;

        if (arrowRenderer != null)
        {
            arrowRenderer.enabled = true;
            Debug.Log($"[DirectionArrowManager] Arrow renderer ENABLED - State: {state} | Target {target.name}");
        }
        else
        {
            Debug.LogError($"[DirectionArrowManager] Arrow renderer is NULL! Cannot show arrow.");
        }

        if (arrowTransform != null && !arrowTransform.gameObject.activeSelf)
        {
            arrowTransform.gameObject.SetActive(true);
            Debug.Log($"[DirectionArrowManager] Arrow Gamebject activated");
        }

        Debug.Log($"[DirectionArrowManager] Showing arrow - State: {state} | Target {target.name}");
    }

    public void HideArrow()
    {
        isArrowActive = false;
        currentTarget = null;

        if (arrowRenderer != null)
        {
            arrowRenderer.enabled = false;
        }

        Debug.Log($"[DirectionArrowManager] rrow hidden");
    }


    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || GameManager.Instance == null || !GameManager.Instance.ShowDetailedLogs) return;

        if (arrowTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(arrowTransform.position, currentTarget != null ? currentTarget.position : arrowTransform.position);
        }
    }

    #region Public API

    public void OnUnlockableArea1Unlocked()
    {
        Debug.Log("[DirectionArrowManager] UnlockableArea_1 unlocked");
        ShowArrow(welcomingCircleTarget, DirectionState.WelcomingCircle);
    }

    public void OnAllPassengersHandedBaggage()
    {
        Debug.Log("[DirectionArrowManager] All passengers handed baggage");
        isAllPassengersHandedBaggage = true;
        ShowArrow(baggageUnloadTarget, DirectionState.BaggageUnload);
    }

    public void OnAllBaggageStacked()
    {
        Debug.Log("[DirectionArrowManager] All baggage stacked");
        isAllBaggageStacked = true;
        ShowArrow(baggageXrayTarget, DirectionState.BaggageXray);
    }

    public void OnAllBaggageInPlatform()
    {
        Debug.Log("[DirectionArrowManager] All baggage in platform");
        isAllBaggageInPlatform = true;
        ShowArrow(stairsTarget, DirectionState.Stairs);
    }

    public void OnPlayerReachedTop()
    {
        Debug.Log("[DirectionArrowManager] Player reached top");
        ShowArrow(passengerXrayTarget, DirectionState.PassengerXray);
    }

    public void OnPassengerXrayCountChanged(int count)
    {
        Debug.Log($"[DirectionArrowManager] Passenger XRay count: {count}/");
        passengerXrayCount = count;

        if (count >= 6)
        {
            ShowArrow(unlockableArea2Target, DirectionState.UnlockableArea2);
        }
    }

    #endregion
}

public enum DirectionState
{
    None,
    UnlockableArea1,
    WelcomingCircle,
    BaggageUnload,
    BaggageXray,
    Stairs,
    PassengerXray,
    UnlockableArea2
}
