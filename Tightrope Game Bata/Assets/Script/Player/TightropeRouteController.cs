using System.Collections;
using TMPro;
using UnityEngine;

/*
 * 共通ルートを進み、分岐地点で左右入力を待ってから、選んだルートを進めるスクリプトです。
 * このクラスは「次にどの区間へ進むか」だけを管理し、実際の座標移動は
 * TightropeAutoGoalMoverへ任せます。
 *
 * ルート配列は「開始点、終了点」の2個1組で設定します。
 * 例: LeftRope2Start, LeftRope2End, LeftRope3Start, LeftRope3End
 * 次の組の開始点へは瞬間移動するため、建物の屋上などに隙間があっても使えます。
 */
public class TightropeRouteController : MonoBehaviour
{
    private enum RouteState
    {
        NotStarted,
        MovingCommonRoute,
        WaitingForBranch,
        MovingSelectedRoute,
        Completed
    }

    [Header("移動担当")]
    [Tooltip("SuitManに付いているTightropeAutoGoalMoverを設定します。")]
    [SerializeField] private TightropeAutoGoalMover playerMover;

    [Tooltip("各ロープ区間を進む速度です。")]
    [SerializeField, Min(0.01f)] private float moveSpeed = 1.2f;

    [Tooltip("Play開始時に共通ルートの移動を自動で始めます。")]
    [SerializeField] private bool startOnPlay = true;

    [Header("ルートTransform（開始・終了の2個1組）")]
    [Tooltip("例: CommonRope1Start, CommonRope1End")]
    [SerializeField] private Transform[] commonRoutePoints;

    [Tooltip("例: LeftRope2Start, LeftRope2End, LeftRope3Start, LeftRope3End")]
    [SerializeField] private Transform[] leftRoutePoints;

    [Tooltip("例: RightRope2Start, RightRope2End, RightRope3Start, RightRope3End")]
    [SerializeField] private Transform[] rightRoutePoints;

    [Header("分岐入力")]
    [Tooltip("左ルートを選ぶキーです。分岐待機中だけ判定します。")]
    [SerializeField] private KeyCode leftRouteKey = KeyCode.LeftArrow;

    [Tooltip("右ルートを選ぶキーです。分岐待機中だけ判定します。")]
    [SerializeField] private KeyCode rightRouteKey = KeyCode.RightArrow;

    [Header("分岐案内（任意）")]
    [Tooltip("未設定でも動作します。設定した場合は分岐待機中だけ表示します。")]
    [SerializeField] private TMP_Text branchGuideText;

    [Tooltip("分岐地点で表示する文章です。")]
    [SerializeField] private string branchGuideMessage = "左キーで左ルート / 右キーで右ルート";

    [Header("BalanceManager連携（任意）")]
    [Tooltip("分岐待機中だけ通常ゲージを止めたい場合に設定します。未設定でもルート移動は動作します。")]
    [SerializeField] private BalanceManager balanceManager;

    [Tooltip("設定したBalanceManagerの既存Pause/Resume APIを分岐待機中だけ使用します。")]
    [SerializeField] private bool pauseBalanceWhileChoosing = true;

    private RouteState state = RouteState.NotStarted;
    private Transform[] activeRoutePoints;
    private int currentSegmentStartIndex;
    private bool balancePausedByThisController;

    public bool IsWaitingForBranch => state == RouteState.WaitingForBranch;
    public bool HasCompletedRoute => state == RouteState.Completed;

    private void Awake()
    {
        if (playerMover == null)
        {
            // 同じGameObjectに付けた場合だけ自動取得します。名前検索は使用しません。
            playerMover = GetComponent<TightropeAutoGoalMover>();
        }

        if (playerMover == null)
        {
            Debug.LogWarning("TightropeRouteController: Player Moverが設定されていません。", this);
        }
        else
        {
            // TightropeAutoGoalMover側の従来のmoveOnStartより先に、ルート制御へ切り替えます。
            playerMover.EnableRouteControl();
        }

        SetBranchGuideVisible(false);
    }

    private void OnEnable()
    {
        if (playerMover != null)
        {
            playerMover.RouteSegmentReached += OnRouteSegmentReached;
        }
    }

    private void Start()
    {
        if (startOnPlay)
        {
            StartRoute();
        }
    }

    private void Update()
    {
        // 左右キーは分岐待機中だけルート選択に使用します。
        if (state != RouteState.WaitingForBranch)
        {
            return;
        }

        bool leftPressed = Input.GetKeyDown(leftRouteKey);
        bool rightPressed = Input.GetKeyDown(rightRouteKey);

        if (leftPressed && rightPressed)
        {
            Debug.LogWarning("TightropeRouteController: 左右キーが同時に押されました。どちらか一方を押してください。", this);
            return;
        }

        if (leftPressed)
        {
            SelectRoute(leftRoutePoints, "左");
        }
        else if (rightPressed)
        {
            SelectRoute(rightRoutePoints, "右");
        }
    }

    private void OnDisable()
    {
        if (playerMover != null)
        {
            playerMover.RouteSegmentReached -= OnRouteSegmentReached;
            playerMover.StopMoving();
        }

        ResumeBalanceIfNeeded();
        SetBranchGuideVisible(false);
    }

    /// <summary>
    /// ルートを最初から開始します。startOnPlayがFalseの場合はUnityEventから呼ぶこともできます。
    /// </summary>
    public void StartRoute()
    {
        if (playerMover == null)
        {
            Debug.LogWarning("TightropeRouteController: Player Moverがないためルートを開始できません。", this);
            return;
        }

        if (!ValidateRoutePoints(commonRoutePoints, "Common Route Points") ||
            !ValidateRoutePoints(leftRoutePoints, "Left Route Points") ||
            !ValidateRoutePoints(rightRoutePoints, "Right Route Points"))
        {
            state = RouteState.NotStarted;
            playerMover.StopMoving();
            return;
        }

        if (moveSpeed <= 0f)
        {
            Debug.LogWarning("TightropeRouteController: Move Speedは0より大きい値にしてください。", this);
            state = RouteState.NotStarted;
            playerMover.StopMoving();
            return;
        }

        ResumeBalanceIfNeeded();
        SetBranchGuideVisible(false);
        playerMover.EnableRouteControl();

        state = RouteState.MovingCommonRoute;
        activeRoutePoints = commonRoutePoints;
        currentSegmentStartIndex = 0;

        Debug.Log("[TightropeRouteController] 共通ルートを開始します。", this);
        StartCurrentSegment();
    }

    private void SelectRoute(Transform[] selectedRoutePoints, string routeLabel)
    {
        // 状態を先に変えることで、同じ分岐での再選択を防ぎます。
        state = RouteState.MovingSelectedRoute;
        activeRoutePoints = selectedRoutePoints;
        currentSegmentStartIndex = 0;

        SetBranchGuideVisible(false);
        // 選択キーとBalanceManagerの左右入力が同じフレームで競合しないよう、
        // 通常ゲージの再開は入力処理が終わった後まで1フレーム遅らせます。
        StartCoroutine(ResumeBalanceAfterBranchInput());

        Debug.Log($"[TightropeRouteController] {routeLabel}ルートを選択しました。", this);
        StartCurrentSegment();
    }

    private IEnumerator ResumeBalanceAfterBranchInput()
    {
        yield return null;
        ResumeBalanceIfNeeded();
    }

    private void StartCurrentSegment()
    {
        Transform startPoint = activeRoutePoints[currentSegmentStartIndex];
        Transform endPoint = activeRoutePoints[currentSegmentStartIndex + 1];
        bool isFinalSegment =
            state == RouteState.MovingSelectedRoute &&
            currentSegmentStartIndex + 2 >= activeRoutePoints.Length;

        Debug.Log($"[TightropeRouteController] 区間を開始: {startPoint.name} -> {endPoint.name}", this);

        if (!playerMover.StartDirectRouteSegment(startPoint, endPoint, moveSpeed, isFinalSegment))
        {
            Debug.LogWarning("TightropeRouteController: 区間を開始できなかったためルート移動を停止します。", this);
            state = RouteState.NotStarted;
        }
    }

    private void OnRouteSegmentReached()
    {
        currentSegmentStartIndex += 2;

        if (currentSegmentStartIndex < activeRoutePoints.Length)
        {
            StartCurrentSegment();
            return;
        }

        if (state == RouteState.MovingCommonRoute)
        {
            EnterBranchSelection();
            return;
        }

        if (state == RouteState.MovingSelectedRoute)
        {
            state = RouteState.Completed;
            Debug.Log("[TightropeRouteController] 最終Goalに到達しました。既存のGoal到達処理を呼びます。", this);
        }
    }

    private void EnterBranchSelection()
    {
        state = RouteState.WaitingForBranch;
        SetBranchGuideVisible(true);
        PauseBalanceIfPossible();

        Debug.Log($"[TightropeRouteController] 分岐地点に到着しました。{branchGuideMessage}", this);
    }

    private bool ValidateRoutePoints(Transform[] routePoints, string fieldName)
    {
        if (routePoints == null || routePoints.Length < 2)
        {
            Debug.LogWarning($"TightropeRouteController: {fieldName}には開始点と終了点を設定してください。", this);
            return false;
        }

        if (routePoints.Length % 2 != 0)
        {
            Debug.LogWarning($"TightropeRouteController: {fieldName}は開始・終了の2個1組で設定してください。", this);
            return false;
        }

        for (int i = 0; i < routePoints.Length; i++)
        {
            if (routePoints[i] != null)
            {
                continue;
            }

            Debug.LogWarning($"TightropeRouteController: {fieldName}の要素{i}が未設定です。", this);
            return false;
        }

        return true;
    }

    private void PauseBalanceIfPossible()
    {
        balancePausedByThisController = false;

        if (!pauseBalanceWhileChoosing)
        {
            Debug.LogWarning("TightropeRouteController: 分岐中もBalanceManagerの左右入力が動作します。", this);
            return;
        }

        if (balanceManager == null)
        {
            Debug.LogWarning("TightropeRouteController: BalanceManager未設定のため、分岐中の通常ゲージ停止を行いません。", this);
            return;
        }

        // 他の処理が先に停止していた場合は、ルート側から勝手に再開しないようにします。
        if (balanceManager.IsNormalBalancePaused)
        {
            return;
        }

        balanceManager.PauseNormalBalanceGauge();
        balancePausedByThisController = true;
    }

    private void ResumeBalanceIfNeeded()
    {
        if (!balancePausedByThisController || balanceManager == null)
        {
            balancePausedByThisController = false;
            return;
        }

        balanceManager.ResumeNormalBalanceGauge();
        balancePausedByThisController = false;
    }

    private void SetBranchGuideVisible(bool visible)
    {
        if (branchGuideText == null)
        {
            return;
        }

        if (visible)
        {
            branchGuideText.text = branchGuideMessage;
        }

        branchGuideText.gameObject.SetActive(visible);
    }
}
