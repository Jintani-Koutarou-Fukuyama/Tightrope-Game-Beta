using System;
using UnityEngine;
using UnityEngine.Events;

/*
  ◆説明
　プレイヤーを GoalPoint へ自動移動させるスクリプトです。
　RopePath が設定されている場合はロープ上を進み、未設定の場合は GoalPoint へ直接移動します。
　イベント中にプレイヤーを止める / 再開する処理から呼ばれることを想定しています。

　◆使い方
　・自動移動を開始したい場合
　　StartMoving() を呼びます。
　　moveOnStart が true の場合は、Start 時に自動で移動開始します。

　・自動移動を止めたい場合
　　StopMoving() を呼びます。
　　イベント演出中だけ一時停止したい場合は PlayerStoping = true にすると、移動処理を止めて歩きアニメーションも止めます。
　　再開する場合は PlayerStoping = false に戻してください。

　・ゴール到達後に再利用したい場合
　　ResetAutoMove() を呼ぶと、ゴール到達状態を戻し、現在位置と GoalPoint から距離を計算し直します。
　　その後、必要に応じて StartMoving() を呼んでください。

　・手動移動スクリプトと切り替えたい場合
　　SetManualMovementEnabled(false) で manualMovementBehaviours に登録した移動スクリプトを無効化します。
　　SetManualMovementEnabled(true) で再び有効化します。

　・ポーズイベントなどでプレイヤーに動きを付ける場合
　　まず StopMoving() または PlayerStoping = true で自動移動を止めてから演出してください。
　　演出が終わったら StartMoving() または PlayerStoping = false で再開します。

　◆注意点
　・TightropePlayerMover などの手動移動スクリプトと同時に動かすと、プレイヤー位置の制御が競合します。
　　必要なスクリプトは manualMovementBehaviours に登録し、disableManualMovementOnStart も確認してください。
　・Inspector で goalPoint、ropePath、animator を設定してください。
　・歩きアニメーションは animator の walkBoolName に設定した Bool を切り替えます。
　・PlayerStoping は既存イベントから直接使われているため、名前や使い方を変更しないでください。
　・GoalPoint に到達すると onGoalReached が呼ばれます。
 */
public class TightropeAutoGoalMover : MonoBehaviour
{
    [Header("Target")]
    // プレイヤーが自動で向かうゴール地点です。
    // Scene内のGoalPointを入れてください。
    [SerializeField] private Transform goalPoint;
    // RopePathを入れると、ロープに沿ってGoalPointまで進みます。
    // 未設定の場合はGoalPointへまっすぐMoveTowardsで進みます。
    [SerializeField] private RopePath ropePath;

    [Header("Move")]
    // 1秒間に進む距離です。
    [SerializeField] private float moveSpeed = 1.2f;
    // ロープ上の点からプレイヤーを少し上へ置くための高さです。
    [SerializeField] private float heightOffset = 0f;
    // GoalPointにこの距離まで近づいたら到着扱いにします。
    [SerializeField] private float goalStopDistance = 0.1f;
    // trueならStart時から自動移動します。
    [SerializeField] private bool moveOnStart = true;
    // trueなら進行方向へプレイヤーを向けます。
    [SerializeField] private bool rotateToMoveDirection = true;
    // 回転のなめらかさです。
    [SerializeField] private float rotationSpeed = 10f;
    // trueなら、下のManual Movement BehavioursをStart時にOFFにします。
    // 既存のTightropePlayerMoverなど、手動前進スクリプトとの競合を防ぐためです。
    [SerializeField] private bool disableManualMovementOnStart = true;
    // 自動前進と同時に動かしたくない手動移動スクリプトを入れます。
    [SerializeField] private Behaviour[] manualMovementBehaviours;

    [Header("Animator")]
    // 既存Animatorを使って歩きモーションを切り替えたい場合に入れます。
    [Tooltip("プレイヤーのAnimatorです。未設定の場合は同じGameObjectまたは子から自動取得します。")]
    [SerializeField] private Animator animator;
    // Animator側の歩きbool名です。
    [Tooltip("歩きアニメーションへ切り替えるBool Parameter名です。")]
    [SerializeField] private string walkBoolName = "catwalk";

    [Header("Events")]
    [SerializeField] private UnityEvent onGoalReached;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private float currentDistanceAlongRope;
    private float goalDistanceAlongRope;
    private bool isMoving;
    private bool hasReachedGoal;
    private bool isRouteControlled;
    private bool isRouteSegmentActive;
    private bool invokeGoalOnRouteSegmentReached;

    public bool IsMoving => isMoving;
    public bool HasReachedGoal => hasReachedGoal;

    // TightropeRouteControllerが、1区間の移動完了を受け取るための通知です。
    public event Action RouteSegmentReached;

    public bool PlayerStoping = false;//プレイヤー固定フラグ
    private void Reset()
    {
        AutoAssignReferences();
    }

    private void Awake()
    {
        AutoAssignReferences();
    }

    private void Start()
    {
        if (disableManualMovementOnStart)
        {
            SetManualMovementEnabled(false);
        }

        InitializePathDistances();

        if (moveOnStart && !isRouteControlled)
        {
            StartMoving();
        }
    }

    private void Update()
    {
        // 停止中は必ず歩きBoolをOFFにします。
        // PlayerStopingはイベント側から変更されるため、ルート移動中でもここで毎フレーム確認します。
        if (!isMoving || hasReachedGoal || goalPoint == null || PlayerStoping)
        {
            SetWalkAnimation(false);
            return;
        }

        if (ropePath != null)
        {
            MoveAlongRope();
        }
        else
        {
            MoveDirectlyToGoal();
        }
    }

    public void StartMoving()
    {
        if (hasReachedGoal)
        {
            return;
        }

        isMoving = true;
        SetWalkAnimation(true);
    }

    public void StopMoving()
    {
        isMoving = false;
        SetWalkAnimation(false);
    }

    /// <summary>
    /// 外部のルート管理スクリプトから移動を制御する時に呼びます。
    /// Awakeで呼ぶと、既存のmoveOnStartによる自動開始を安全に止められます。
    /// </summary>
    public void EnableRouteControl()
    {
        isRouteControlled = true;
        isRouteSegmentActive = false;
        hasReachedGoal = false;
        StopMoving();
    }

    /// <summary>
    /// 指定した開始Transformから終了Transformまで、座標間移動を開始します。
    /// 最終区間だけinvokeGoalWhenReachedをtrueにすると、既存のonGoalReachedも呼ばれます。
    /// </summary>
    public bool StartDirectRouteSegment(
        Transform startPoint,
        Transform endPoint,
        float segmentMoveSpeed,
        bool invokeGoalWhenReached)
    {
        if (startPoint == null || endPoint == null)
        {
            Debug.LogWarning("TightropeAutoGoalMover: ルート区間の開始点または終了点が設定されていません。", this);
            return false;
        }

        if (segmentMoveSpeed <= 0f)
        {
            Debug.LogWarning("TightropeAutoGoalMover: ルート移動速度は0より大きい値にしてください。", this);
            return false;
        }

        isRouteControlled = true;
        isRouteSegmentActive = true;
        invokeGoalOnRouteSegmentReached = invokeGoalWhenReached;
        hasReachedGoal = false;

        // 今回のルート制はRopePathではなく、Transform間の直線移動を使用します。
        ropePath = null;
        goalPoint = endPoint;
        moveSpeed = segmentMoveSpeed;

        // 各区間の開始点へ移動します。ロープ間の屋上部分もこの処理で瞬間移動できます。
        transform.SetPositionAndRotation(startPoint.position, startPoint.rotation);
        StartMoving();
        return true;
    }

    public void SetManualMovementEnabled(bool enabled)
    {
        if (manualMovementBehaviours == null)
        {
            return;
        }

        for (int i = 0; i < manualMovementBehaviours.Length; i++)
        {
            Behaviour behaviour = manualMovementBehaviours[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            behaviour.enabled = enabled;
        }
    }

    public void ResetAutoMove()
    {
        hasReachedGoal = false;
        InitializePathDistances();
    }

    private void InitializePathDistances()
    {
        if (ropePath == null || goalPoint == null)
        {
            return;
        }

        currentDistanceAlongRope = ropePath.GetDistanceAlongRope(transform.position);
        goalDistanceAlongRope = ropePath.GetDistanceAlongRope(goalPoint.position);
    }

    private void MoveAlongRope()
    {
        float previousDistance = currentDistanceAlongRope;
        currentDistanceAlongRope = Mathf.MoveTowards(
            currentDistanceAlongRope,
            goalDistanceAlongRope,
            moveSpeed * Time.deltaTime);

        Vector3 nextPosition = ropePath.GetPointAtDistance(currentDistanceAlongRope) + Vector3.up * heightOffset;
        Vector3 moveDirection = nextPosition - transform.position;
        transform.position = nextPosition;

        if (rotateToMoveDirection)
        {
            RotateTowards(moveDirection);
        }

        SetWalkAnimation(!Mathf.Approximately(previousDistance, currentDistanceAlongRope));

        if (Mathf.Abs(currentDistanceAlongRope - goalDistanceAlongRope) <= goalStopDistance)
        {
            ReachGoal();
        }
    }

    private void MoveDirectlyToGoal()
    {
        Vector3 targetPosition = goalPoint.position;
        Vector3 nextPosition = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        Vector3 moveDirection = nextPosition - transform.position;
        transform.position = nextPosition;

        if (rotateToMoveDirection)
        {
            RotateTowards(moveDirection);
        }

        // 低速移動では1フレームの移動量が小さいため、Mathf.Epsilonで実際に動いたかだけを判定します。
        SetWalkAnimation(moveDirection.sqrMagnitude > Mathf.Epsilon);

        if (Vector3.Distance(transform.position, targetPosition) <= goalStopDistance)
        {
            ReachGoal();
        }
    }

    private void RotateTowards(Vector3 moveDirection)
    {
        if (moveDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ReachGoal()
    {
        if (hasReachedGoal)
        {
            return;
        }

        bool reachedRouteSegment = isRouteSegmentActive;
        bool shouldInvokeGoal = invokeGoalOnRouteSegmentReached;

        if (reachedRouteSegment && goalPoint != null)
        {
            // 分岐地点やGoalで誤差が残らないよう、区間終了時だけ終了Transformへ正確に合わせます。
            transform.position = goalPoint.position;
        }

        hasReachedGoal = true;
        isMoving = false;
        isRouteSegmentActive = false;
        invokeGoalOnRouteSegmentReached = false;
        SetWalkAnimation(false);

        if (reachedRouteSegment)
        {
            DebugLog("Route segment reached.");
            RouteSegmentReached?.Invoke();

            if (shouldInvokeGoal)
            {
                onGoalReached?.Invoke();
            }

            return;
        }

        DebugLog("Goal reached.");
        onGoalReached?.Invoke();
    }

    private void SetWalkAnimation(bool walking)
    {
        if (animator == null || string.IsNullOrEmpty(walkBoolName))
        {
            return;
        }

        animator.SetBool(walkBoolName, walking);
    }

    private void AutoAssignReferences()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (goalPoint == null)
        {
            GameObject goalObject = GameObject.Find("GoalPoint");
            if (goalObject != null)
            {
                goalPoint = goalObject.transform;
            }
        }

        if (ropePath == null)
        {
            GameObject ropeObject = GameObject.Find("Rope");
            if (ropeObject != null)
            {
                ropePath = ropeObject.GetComponent<RopePath>();
            }
        }
    }

    private void DebugLog(string message)
    {
        if (!debugLog)
        {
            return;
        }

        Debug.Log($"[TightropeAutoGoalMover] {message}", this);
    }
}
