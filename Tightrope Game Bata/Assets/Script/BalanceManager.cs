using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Serialization;

/*
  ◆説明
　バランスゲージ全体を管理するスクリプトです。
　通常ゲージ、イベント用レイアウト、スナイパー防御用ゲージ、マトリックス回避用ゲージの表示・操作・判定を担当します。
　イベント側のスクリプトは、playMode などの内部状態を直接変更せず、public メソッドを呼んで操作してください。

　◆使い方
　・通常バランスゲージを一時停止したい場合
　　PauseNormalBalanceGauge() を呼びます。
　　通常バランスゲージの更新、成功/失敗判定、ダメージ処理、タイマー更新を一時停止します。
　　スナイパーイベントだけでなく、ポーズイベント、会話イベント、演出イベント、地震イベントなどで通常ゲージを止めたい時にも使えます。

　・一時停止した通常バランスゲージを再開したい場合
　　ResumeNormalBalanceGauge() を呼びます。
　　イベント開始時に PauseNormalBalanceGauge() を呼んだ場合は、イベント終了時に必ず ResumeNormalBalanceGauge() を呼んでください。
　　ResumeNormalBalanceGauge() を呼び忘れると、通常ゲージが止まったままになります。

　・イベント用レイアウトに切り替えたい場合
　　SetEventVerticalLayoutActive(true) を呼ぶと、イベント用の縦表示レイアウトに切り替わります。
　　SetEventVerticalLayoutActive(false) を呼ぶと、通常の横表示レイアウトに戻ります。
　　個別に呼ぶ場合は SwitchToEventVerticalLayout() / SwitchToNormalHorizontalLayout() を使います。

　・スナイパー防御イベントで使う場合
　　イベント開始時に EnableSniperDefenseMode() を呼びます。
　　狙われる位置を変える場合は SetSniperTargetPosition(0.0f～1.0f) を呼びます。
　　撃たれたタイミングの成功/失敗判定は ResolveSniperDefenseShot() を呼びます。
　　イベント終了時は DisableSniperDefenseMode() を呼びます。

　・マトリックス回避イベントで使う場合
　　イベント開始時に EnableMatrixAvoidMode() を呼びます。
　　イベント終了時は DisableMatrixAvoidMode() を呼びます。

　・ゲージ状態を初期化したい場合
　　ResetBalance() を呼びます。
　　現在ゲージ内に入っているか確認したい場合は IsInsideTarget() を使います。

　◆注意点
　・playMode、gaugeDirection などの内部状態は、イベント側から直接変更しないでください。
　・ゲージの見た目は動かしたいが、判定だけ止めたい場合などは別仕様になるため、PauseNormalBalanceGauge() をそのまま使うべきか確認してください。
　・Inspector で gaugeRoot / balanceBar / targetZone / balancePoint を設定してください。
　・スナイパー防御では sniperDefenseStickTarget、マトリックス回避では Matrix Avoid Mode の各値を確認してください。
　・成功、失敗、ダメージ連携は onBalanceSuccess / onBalanceMiss / onDamage に設定します。
　・Animator を使う場合は animator と unbalanceBoolName を Animator Controller 側の Bool 名と合わせてください。
 */
public class BalanceManager : MonoBehaviour
{
    // 将来的なイベント切り替え用。
    // Horizontal: 横ゲージ、Vertical: 縦ゲージとしてUIを動かします。
    public enum BalanceGaugeDirection
    {
        Horizontal,
        Vertical
    }

    public enum BalancePlayMode
    {
        Normal,
        SniperDefense,
        MatrixAvoid
    }

    public enum StickFollowSpace
    {
        Local,
        World
    }

    public enum StickFollowAxis
    {
        X,
        Y,
        Z
    }

    public enum StickFollowUpdateTiming
    {
        Update,
        LateUpdate
    }

    [System.Serializable]
    public class DamageEvent : UnityEvent<int>
    {
    }

    private struct RectTransformLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
        public Quaternion localRotation;

        public RectTransformLayout(RectTransform rectTransform)
        {
            anchorMin = rectTransform.anchorMin;
            anchorMax = rectTransform.anchorMax;
            pivot = rectTransform.pivot;
            anchoredPosition = rectTransform.anchoredPosition;
            sizeDelta = rectTransform.sizeDelta;
            localScale = rectTransform.localScale;
            localRotation = rectTransform.localRotation;
        }

        public void ApplyTo(RectTransform rectTransform)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
            rectTransform.localScale = localScale;
            rectTransform.localRotation = localRotation;
        }
    }

    [Header("UI")]
    [SerializeField] private RectTransform gaugeRoot;
    [SerializeField] private RectTransform balanceBar;
    [SerializeField] private RectTransform targetZone;
    [SerializeField] private RectTransform balancePoint;

    [Header("Event Vertical Layout")]
    // イベント中にBalanceGauge全体を画面左側へ置くための設定です。
    // 横ゲージのサイズは変えず、親RectTransformを90度回転して縦表示にします。
    [SerializeField] private Vector2 eventVerticalAnchorMin = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 eventVerticalAnchorMax = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 eventVerticalPivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 eventVerticalAnchoredPosition = new Vector2(40f, 0f);
    [SerializeField] private float eventVerticalRotationZ = 90f;
    [SerializeField] private Vector3 eventVerticalScale = new Vector3(0.6f, 0.6f, 1f);
    [SerializeField] private bool resetGaugePositionOnLayoutSwitch = true;

    [Header("Gauge Direction")]
    [SerializeField] private BalanceGaugeDirection gaugeDirection = BalanceGaugeDirection.Horizontal;

    [Header("Mode")]
    // 通常綱渡り、スナイパー防御、マトリックス回避のどれとして動かすかです。
    // State遷移はSniperEventManager側で行い、BalanceManagerは表示・操作・判定だけを担当します。
    [SerializeField] private BalancePlayMode playMode = BalancePlayMode.Normal;

    [Header("Balance Point")]
    [SerializeField] private float pointMoveSpeed = 10f;
    [SerializeField] private KeyCode negativeKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode positiveKey = KeyCode.RightArrow;
    [SerializeField] private bool returnPointToCenter;
    [SerializeField] private float pointReturnSpeed = 80f;

    [Header("Target Zone")]
    [SerializeField] private float targetMoveSpeed = 0.5f;
    [SerializeField] private float targetMoveRangeRate = 0.85f;

    [Header("Random Challenge")]
    // trueにすると、黄色い目標範囲は動き続けず、ランダムな位置へ出る方式になります。
    [SerializeField] private bool useRandomTargetChallenge = true;
    // PlayerMarkerをTargetYellowAreaへ合わせる制限時間です。
    [SerializeField] private float challengeTimeLimit = 3f;
    // 成功したら次のランダム位置へ移動します。
    [SerializeField] private bool randomizeTargetAfterSuccess = true;
    // 失敗したら次のランダム位置へ移動します。
    [SerializeField] private bool randomizeTargetAfterFailure = true;
    // ランダム位置が現在のPlayerMarkerと重なりにくいようにする最小距離です。
    [SerializeField] private float randomTargetMinDistanceFromPoint = 30f;
    // UI Textを使っている場合の残り時間表示です。
    [SerializeField] private Text balanceTimerText;
    // TextMeshProを使っている場合の残り時間表示です。
    [SerializeField] private TMP_Text balanceTimerTmpText;
    // 残り時間表示の書式です。{0:0.0} に秒数が入ります。
    [SerializeField] private string timerFormat = "{0:0.0}";

    [Header("Sniper Defense Mode")]
    // スナイパー防御中、白球と連動して上下させる棒Transformです。
    [SerializeField] private Transform sniperDefenseStickTarget;
    // 白球の位置を棒のローカルY移動へ変換する幅です。
    [FormerlySerializedAs("sniperDefenseAdditionalFollowTargets")]
    [SerializeField] private Transform[] sniperDefenseHandFollowTargets;
    [SerializeField] private bool useSeparateHandFollowSettings = false;
    [SerializeField] private StickFollowSpace handFollowSpace = StickFollowSpace.Local;
    [SerializeField] private StickFollowAxis handFollowAxis = StickFollowAxis.Z;
    [SerializeField] private float handFollowMinValue = -0.007f;
    [SerializeField] private float handFollowMaxValue = 0.007f;
    [SerializeField] private StickFollowSpace stickFollowSpace = StickFollowSpace.Local;
    [FormerlySerializedAs("stickFollowAxisMode")]
    [SerializeField] private StickFollowAxis stickFollowAxis = StickFollowAxis.Z;
    [FormerlySerializedAs("stickFollowMinLocalY")]
    [SerializeField] private float stickFollowMinValue = -0.007f;
    [FormerlySerializedAs("stickFollowMaxLocalY")]
    [SerializeField] private float stickFollowMaxValue = 0.007f;
    [SerializeField] private float stickFollowSmoothSpeed = 15f;
    [SerializeField] private StickFollowUpdateTiming stickFollowUpdateTiming = StickFollowUpdateTiming.LateUpdate;
    [SerializeField] private bool resetStickTargetOnSniperDefenseEnd = true;
    // スナイパー防御中に白球を下へ動かすキーです。
    [SerializeField] private KeyCode sniperDefenseDownKey = KeyCode.DownArrow;
    // スナイパー防御中に白球を上へ動かすキーです。
    [SerializeField] private KeyCode sniperDefenseUpKey = KeyCode.UpArrow;

    [Header("Sniper Defense Sound")]
    [SerializeField] private AudioSource sniperDeflectAudioSource;
    [SerializeField] private AudioClip sniperDeflectSound;
    [SerializeField] private float sniperDeflectSoundVolume = 1.0f;

    [Header("Matrix Avoid Mode")]
    // マトリックス回避中、白球が自動で上昇する速さです。
    [SerializeField] private float matrixPointRiseSpeed = 80f;
    // マトリックス回避中、下入力で白球を押し戻す強さです。
    [SerializeField] private float matrixDownPushSpeed = 140f;
    // マトリックス回避中に白球を下へ押し戻すキーです。
    [SerializeField] private KeyCode matrixDownKey = KeyCode.DownArrow;
    [SerializeField] private float matrixAvoidAutoRiseSpeed = 80f;
    [SerializeField] private float matrixAvoidPushDownSpeed = 140f;
    [SerializeField, Range(0f, 1f)] private float matrixAvoidTargetPosition = 0.2f;
    [SerializeField] private float matrixAvoidAllowedOutsideTime = 3.0f;
    [SerializeField] private float matrixAvoidDuration = 3.0f;
    [SerializeField] private bool matrixAvoidUseDurationSuccess = false;

    [Header("Failure")]
    [SerializeField] private float failTimeLimit = 1.5f;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private bool resetPointAfterDamage = true;

    [Header("Player Wobble")]
    [SerializeField] private Transform wobbleTarget;
    [SerializeField] private float wobbleDuration = 0.6f;
    [SerializeField] private float wobbleAngle = 8f;
    [SerializeField] private float wobbleSpeed = 18f;

    [Header("Events")]
    [SerializeField] private UnityEvent onBalanceSuccess;
    [SerializeField] private UnityEvent onBalanceMiss;
    [SerializeField] private DamageEvent onDamage;

    [Header("Animator")]
    // Unbalanceアニメーションを再生するAnimatorです。
    // ここがnullだとSetBoolを呼べず、アニメーションは再生されません。
    [SerializeField] private Animator animator;
    // Animator側のboolパラメーター名です。
    // Animator Parametersにある名前と完全に同じ文字にしてください。
    [SerializeField] private string unbalanceBoolName = "Unbalance";
    // Unbalanceをtrueにしておく時間です。
    // 短すぎるとアニメーションに入る前にfalseへ戻ることがあります。
    [SerializeField] private float unbalanceDuration = 0.6f;

    [Header("Debug")]
    // trueにするとConsoleへ状態確認用ログを出します。
    // 原因調査が終わったらfalseにするとConsoleが静かになります。
    [SerializeField] private bool enableDebugLog = true;
    // outsideTimerのログを何秒ごとに出すかです。
    // 毎フレーム出すとConsoleが見づらくなるので、少し間隔を空けます。
    [SerializeField] private float timerLogInterval = 0.25f;

    private float pointAxisPosition;
    private float targetAxisPosition;
    private float targetMoveDirection = 1f;
    private float outsideTimer;
    private float challengeTimer;
    private float wobbleTimer;
    private Quaternion wobbleBaseRotation;
    private Vector3 sniperDefenseStickBasePosition;
    private Vector3[] sniperDefenseHandBasePositions;
    private bool hasSniperDefenseStickBasePosition;
    private bool hasSniperDefenseHandBasePositions;
    private bool isInsideTarget;
    private bool wasInsideTarget;
    private float nextSniperDefenseFollowLogTime;
    private bool isNormalBalancePaused;
    private Coroutine unbalanceCoroutine;
    private float nextTimerLogTime;
    private bool hasSavedHorizontalLayout;
    private bool hasSavedNormalCountUiState;
    private GameObject savedBalanceTimerTextObject;
    private GameObject savedBalanceTimerTmpTextObject;
    private bool balanceTimerTextActiveBeforeSniper;
    private bool balanceTimerTmpTextActiveBeforeSniper;
    private RectTransformLayout savedGaugeRootLayout;
    private RectTransformLayout savedBalanceBarLayout;
    private RectTransformLayout savedTargetZoneLayout;
    private RectTransformLayout savedBalancePointLayout;

    public float OutsideTimer => outsideTimer;
    public float ChallengeRemainingTime => Mathf.Max(0f, challengeTimeLimit - challengeTimer);
    public BalanceGaugeDirection GaugeDirection => gaugeDirection;
    public BalancePlayMode PlayMode => playMode;
    public bool IsNormalBalancePaused => isNormalBalancePaused;

    private void Start()
    {
        AutoAssignBalanceUi();

        if (gaugeRoot == null && balanceBar != null)
        {
            gaugeRoot = balanceBar.parent as RectTransform;
        }

        AutoAssignTimerText();
        SaveHorizontalLayoutIfNeeded();

        // UIの初期位置を現在のInspector配置から読み取ります。
        pointAxisPosition = GetAxisAnchoredPosition(balancePoint);
        targetAxisPosition = GetAxisAnchoredPosition(targetZone);

        if (wobbleTarget != null)
        {
            wobbleBaseRotation = wobbleTarget.localRotation;
        }

        if (useRandomTargetChallenge)
        {
            RandomizeTargetZone();
            challengeTimer = 0f;
        }

        ApplyAllUiPositions();
        UpdateBalanceState();
        wasInsideTarget = isInsideTarget;
        UpdateTimerText();

        DebugLog($"Start: IsInsideTarget={isInsideTarget}, animator={(animator != null ? animator.name : "null")}, failTimeLimit={failTimeLimit}");
        CheckAnimatorSetup();
    }

    private void Update()
    {
        switch (playMode)
        {
            case BalancePlayMode.SniperDefense:
                UpdateSniperDefenseMode();
                break;
            case BalancePlayMode.MatrixAvoid:
                UpdateMatrixAvoidMode();
                break;
            default:
                UpdateNormalMode();
                break;
        }

        UpdateWobble();
    }

    private void LateUpdate()
    {
        if (playMode != BalancePlayMode.SniperDefense)
        {
            return;
        }

        if (stickFollowUpdateTiming != StickFollowUpdateTiming.LateUpdate)
        {
            return;
        }

        UpdateSniperDefenseStickPosition("LateUpdate");
    }

    private void UpdateNormalMode()
    {
        if (isNormalBalancePaused)
        {
            return;
        }

        if (!useRandomTargetChallenge)
        {
            MoveTargetZone();
        }

        MoveBalancePoint();
        ApplyAllUiPositions();
        UpdateBalanceState();

        if (useRandomTargetChallenge)
        {
            UpdateRandomChallenge();
        }
        else
        {
            UpdateFailureTimer();
        }
    }

    // 外部イベントから横/縦を切り替えたい時に呼びます。
    public void SetGaugeDirection(BalanceGaugeDirection direction)
    {
        if (gaugeDirection == direction)
        {
            return;
        }

        gaugeDirection = direction;

        // 切り替え時に位置がゲージ外へ飛ばないよう、現在値を新しい長さで丸めます。
        pointAxisPosition = Mathf.Clamp(pointAxisPosition, GetMinPointPosition(), GetMaxPointPosition());
        targetAxisPosition = Mathf.Clamp(targetAxisPosition, GetMinTargetPosition(), GetMaxTargetPosition());
        ApplyAllUiPositions();
        UpdateBalanceState();
    }

    // ボタンやTimelineなどから横ゲージへ戻したい時用。
    public void SetHorizontalGauge()
    {
        SetGaugeDirection(BalanceGaugeDirection.Horizontal);
    }

    // イベント中だけ縦ゲージにしたい時用。
    public void SetVerticalGauge()
    {
        SetGaugeDirection(BalanceGaugeDirection.Vertical);
    }

    public void SwitchToEventVerticalLayout()
    {
        SaveHorizontalLayoutIfNeeded();

        SetGaugeDirection(BalanceGaugeDirection.Horizontal);
        ApplyEventVerticalLayout();

        if (resetGaugePositionOnLayoutSwitch)
        {
            ResetBalance();
        }

        DebugLog("Balance gauge switched to event vertical layout.");
    }

    public void SwitchToNormalHorizontalLayout()
    {
        RestoreHorizontalLayout();
        SetGaugeDirection(BalanceGaugeDirection.Horizontal);

        if (resetGaugePositionOnLayoutSwitch)
        {
            ResetBalance();
        }

        DebugLog("Balance gauge restored to normal horizontal layout.");
    }

    public void SetEventVerticalLayoutActive(bool eventActive)
    {
        if (eventActive)
        {
            SwitchToEventVerticalLayout();
        }
        else
        {
            SwitchToNormalHorizontalLayout();
        }
    }

    // ダメージ後やリトライ時にゲージを中央へ戻したい時用。
    public void ResetBalance()
    {
        pointAxisPosition = 0f;
        targetAxisPosition = 0f;
        outsideTimer = 0f;
        targetMoveDirection = 1f;
        challengeTimer = 0f;

        if (useRandomTargetChallenge)
        {
            RandomizeTargetZone();
        }

        ApplyAllUiPositions();
        UpdateBalanceState();
        UpdateTimerText();
    }

    public void SetSniperTargetPosition(float normalizedPosition)
    {
        float t = Mathf.Clamp01(normalizedPosition);
        targetAxisPosition = Mathf.Lerp(GetMinTargetPosition(), GetMaxTargetPosition(), t);
        ApplyAllUiPositions();
        UpdateBalanceState();
        DebugLog($"Sniper target set. normalized={t:F2}, target={targetAxisPosition:F2}");
    }

    public void EnableSniperDefenseMode()
    {
        playMode = BalancePlayMode.SniperDefense;
        outsideTimer = 0f;
        challengeTimer = 0f;
        nextSniperDefenseFollowLogTime = 0f;
        SetGaugeDirection(BalanceGaugeDirection.Horizontal);
        SaveHorizontalLayoutIfNeeded();
        ApplyEventVerticalLayout();
        hasSniperDefenseStickBasePosition = false;
        hasSniperDefenseHandBasePositions = false;
        SaveSniperDefenseStickBasePosition();
        LogSniperDefenseFollowSetup();
        ApplyAllUiPositions();
        UpdateBalanceState();
        DebugLog("SniperDefenseMode enabled.");
    }

    public void DisableSniperDefenseMode()
    {
        RestoreSniperDefenseStickPosition();

        if (playMode == BalancePlayMode.SniperDefense)
        {
            playMode = BalancePlayMode.Normal;
        }

        outsideTimer = 0f;
        DebugLog("SniperDefenseMode disabled.");
    }

    public void EnableMatrixAvoidMode()
    {
        playMode = BalancePlayMode.MatrixAvoid;
        outsideTimer = 0f;
        challengeTimer = 0f;
        SetGaugeDirection(BalanceGaugeDirection.Horizontal);
        SaveHorizontalLayoutIfNeeded();
        ApplyEventVerticalLayout();
        SetMatrixAvoidTargetPosition();
        pointAxisPosition = targetAxisPosition;
        ApplyAllUiPositions();
        UpdateBalanceState();
        wasInsideTarget = isInsideTarget;
        UpdateTimerText();
        DebugLog("MatrixAvoidMode enabled.");
    }

    public void DisableMatrixAvoidMode()
    {
        if (playMode == BalancePlayMode.MatrixAvoid)
        {
            playMode = BalancePlayMode.Normal;
        }

        outsideTimer = 0f;
        challengeTimer = 0f;
        UpdateTimerText();
        DebugLog("MatrixAvoidMode disabled.");
    }

    public void PauseNormalBalanceGauge()
    {
        if (isNormalBalancePaused)
        {
            return;
        }

        isNormalBalancePaused = true;
        outsideTimer = 0f;
        challengeTimer = 0f;
        UpdateTimerText();
        DebugLog("Normal balance gauge paused.");
    }

    public void ResumeNormalBalanceGauge()
    {
        if (!isNormalBalancePaused)
        {
            return;
        }

        isNormalBalancePaused = false;
        outsideTimer = 0f;
        challengeTimer = 0f;
        UpdateBalanceState();
        wasInsideTarget = isInsideTarget;
        UpdateTimerText();
        DebugLog("Normal balance gauge resumed.");
    }

    public void HideNormalCountUiForSniperEvent()
    {
        AutoAssignTimerText();

        GameObject textObject = balanceTimerText != null ? balanceTimerText.gameObject : null;
        GameObject tmpObject = balanceTimerTmpText != null ? balanceTimerTmpText.gameObject : null;

        if (textObject == null && tmpObject == null)
        {
            Debug.LogWarning("BalanceManager: normal count UI is not assigned. Hide skipped.", this);
            return;
        }

        if (!hasSavedNormalCountUiState)
        {
            savedBalanceTimerTextObject = textObject;
            savedBalanceTimerTmpTextObject = tmpObject;
            balanceTimerTextActiveBeforeSniper = textObject != null && textObject.activeSelf;
            balanceTimerTmpTextActiveBeforeSniper = tmpObject != null && tmpObject.activeSelf;
            hasSavedNormalCountUiState = true;
        }

        SetNormalCountGameObjectActive(textObject, false);

        if (tmpObject != textObject)
        {
            SetNormalCountGameObjectActive(tmpObject, false);
        }
    }

    public void RestoreNormalCountUiAfterSniperEvent()
    {
        if (!hasSavedNormalCountUiState)
        {
            return;
        }

        SetNormalCountGameObjectActive(savedBalanceTimerTextObject, balanceTimerTextActiveBeforeSniper);

        if (savedBalanceTimerTmpTextObject != savedBalanceTimerTextObject)
        {
            SetNormalCountGameObjectActive(savedBalanceTimerTmpTextObject, balanceTimerTmpTextActiveBeforeSniper);
        }

        hasSavedNormalCountUiState = false;
        savedBalanceTimerTextObject = null;
        savedBalanceTimerTmpTextObject = null;
        UpdateTimerText();
    }

    private void SetNormalCountGameObjectActive(GameObject targetObject, bool active)
    {
        if (targetObject == null)
        {
            return;
        }

        targetObject.SetActive(active);
    }

    public bool IsInsideTarget()
    {
        return isInsideTarget;
    }

    public bool ResolveSniperDefenseShot()
    {
        UpdateBalanceState();
        bool success = isInsideTarget;

        if (success)
        {
            PlaySniperDeflectSound();
            onBalanceSuccess?.Invoke();
        }
        else
        {
            ApplyBalanceMissReaction(true);
        }

        DebugLog($"Sniper defense shot resolved. success={success}");
        return success;
    }

    private void PlaySniperDeflectSound()
    {
        bool hasMissingAudioSetting = false;

        if (sniperDeflectAudioSource == null)
        {
            Debug.LogWarning("BalanceManager: sniperDeflectAudioSource not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (sniperDeflectSound == null)
        {
            Debug.LogWarning("BalanceManager: sniperDeflectSound not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (hasMissingAudioSetting)
        {
            return;
        }

        sniperDeflectAudioSource.PlayOneShot(sniperDeflectSound, sniperDeflectSoundVolume);
        Debug.Log("BalanceManager: Sniper deflect sound played.", this);
    }

    private void MoveBalancePoint()
    {
        float input = 0f;

        // negativeKeyは左/下方向、positiveKeyは右/上方向として扱います。
        if (Input.GetKey(negativeKey))
        {
            input -= 1f;
        }

        if (Input.GetKey(positiveKey))
        {
            input += 1f;
        }

        pointAxisPosition += input * pointMoveSpeed * Time.deltaTime;

        // 入力していない時に中央へ少し戻したい場合の設定です。
        if (returnPointToCenter && Mathf.Approximately(input, 0f))
        {
            pointAxisPosition = Mathf.MoveTowards(pointAxisPosition, 0f, pointReturnSpeed * Time.deltaTime);
        }

        pointAxisPosition = Mathf.Clamp(pointAxisPosition, GetMinPointPosition(), GetMaxPointPosition());
    }

    private void UpdateSniperDefenseMode()
    {
        float input = 0f;

        if (Input.GetKey(sniperDefenseDownKey))
        {
            input -= 1f;
        }

        if (Input.GetKey(sniperDefenseUpKey))
        {
            input += 1f;
        }

        pointAxisPosition += input * pointMoveSpeed * Time.deltaTime;
        pointAxisPosition = Mathf.Clamp(pointAxisPosition, GetMinPointPosition(), GetMaxPointPosition());

        ApplyAllUiPositions();
        UpdateBalanceState();

        if (stickFollowUpdateTiming == StickFollowUpdateTiming.Update)
        {
            UpdateSniperDefenseStickPosition("Update");
        }
    }

    private void UpdateMatrixAvoidMode()
    {
        SetMatrixAvoidTargetPosition();
        pointAxisPosition += matrixAvoidAutoRiseSpeed * Time.deltaTime;

        if (Input.GetKey(matrixDownKey))
        {
            pointAxisPosition -= matrixAvoidPushDownSpeed * Time.deltaTime;
        }

        pointAxisPosition = Mathf.Clamp(pointAxisPosition, GetMinPointPosition(), GetMaxPointPosition());

        ApplyAllUiPositions();
        UpdateBalanceState();
        UpdateMatrixAvoidFailureTimer();
        UpdateMatrixAvoidSuccessTimer();
        UpdateTimerText();
    }

    private void SetMatrixAvoidTargetPosition()
    {
        float target01 = Mathf.Clamp01(matrixAvoidTargetPosition);
        targetAxisPosition = Mathf.Lerp(GetMinTargetPosition(), GetMaxTargetPosition(), target01);
    }

    private void UpdateMatrixAvoidFailureTimer()
    {
        if (isInsideTarget)
        {
            outsideTimer = 0f;
            return;
        }

        outsideTimer += Time.deltaTime;
        LogOutsideTimer();

        if (outsideTimer < Mathf.Max(0.01f, matrixAvoidAllowedOutsideTime))
        {
            return;
        }

        DebugLog($"MatrixAvoidMode failed. outsideTimer={outsideTimer:F2}, allowed={matrixAvoidAllowedOutsideTime:F2}");
        outsideTimer = 0f;
        ApplyBalanceMissReaction(true);
    }

    private void UpdateMatrixAvoidSuccessTimer()
    {
        if (!matrixAvoidUseDurationSuccess)
        {
            return;
        }

        challengeTimer += Time.deltaTime;

        if (challengeTimer < Mathf.Max(0.1f, matrixAvoidDuration))
        {
            return;
        }

        FinishMatrixAvoidPhase();
    }

    public void CompleteMatrixAvoidByBulletPass()
    {
        if (playMode != BalancePlayMode.MatrixAvoid)
        {
            return;
        }

        DebugLog("MatrixAvoid succeeded by bullet pass.");
        FinishMatrixAvoidPhase();
    }

    private void FinishMatrixAvoidPhase()
    {
        DebugLog("MatrixAvoid succeeded.");
        DisableMatrixAvoidMode();
    }

    private void SaveSniperDefenseStickBasePosition()
    {
        if (sniperDefenseStickTarget != null && !hasSniperDefenseStickBasePosition)
        {
            sniperDefenseStickBasePosition = GetFollowPosition(sniperDefenseStickTarget, stickFollowSpace);
            hasSniperDefenseStickBasePosition = true;
            DebugLog($"SniperDefense stick base saved. target={sniperDefenseStickTarget.name}, space={stickFollowSpace}, local={FormatVector(sniperDefenseStickTarget.localPosition)}, world={FormatVector(sniperDefenseStickTarget.position)}, saved={FormatVector(sniperDefenseStickBasePosition)}");
        }

        if (!hasSniperDefenseHandBasePositions)
        {
            SaveSniperDefenseHandBasePositions();
        }
    }

    private void UpdateSniperDefenseStickPosition(string updateSource)
    {
        SaveSniperDefenseStickBasePosition();

        float normalizedPoint = GetNormalizedPointPosition01();
        float stickOffset = Mathf.Lerp(stickFollowMinValue, stickFollowMaxValue, normalizedPoint);
        float handOffset = useSeparateHandFollowSettings
            ? Mathf.Lerp(handFollowMinValue, handFollowMaxValue, normalizedPoint)
            : stickOffset;
        float smoothSpeed = Mathf.Max(0f, stickFollowSmoothSpeed);
        StickFollowSpace activeHandSpace = GetActiveHandFollowSpace();
        StickFollowAxis activeHandAxis = GetActiveHandFollowAxis();
        bool logThisFrame = ShouldLogSniperDefenseFollow();

        if (logThisFrame)
        {
            DebugLog($"UpdateSniperDefenseStickPosition called. source={updateSource}, timing={stickFollowUpdateTiming}, normalized={normalizedPoint:F3}, stickOffset={stickOffset:F4}, handOffset={handOffset:F4}, useSeparateHandFollowSettings={useSeparateHandFollowSettings}, stickSpace={stickFollowSpace}, stickAxis={stickFollowAxis}, handSpace={activeHandSpace}, handAxis={activeHandAxis}, handTargets={GetValidHandFollowTargetCount()}");
        }

        UpdateFollowTarget(sniperDefenseStickTarget, sniperDefenseStickBasePosition, hasSniperDefenseStickBasePosition, stickOffset, smoothSpeed, stickFollowSpace, stickFollowAxis, logThisFrame);
        UpdateSniperDefenseHandFollowTargets(handOffset, smoothSpeed, activeHandSpace, activeHandAxis, logThisFrame);
    }

    private void RestoreSniperDefenseStickPosition()
    {
        if (!resetStickTargetOnSniperDefenseEnd)
        {
            return;
        }

        if (sniperDefenseStickTarget != null && hasSniperDefenseStickBasePosition)
        {
            SetFollowPosition(sniperDefenseStickTarget, sniperDefenseStickBasePosition, stickFollowSpace);
        }

        RestoreSniperDefenseHandFollowTargets();
    }

    private void SaveSniperDefenseHandBasePositions()
    {
        int targetCount = sniperDefenseHandFollowTargets != null ? sniperDefenseHandFollowTargets.Length : 0;
        sniperDefenseHandBasePositions = new Vector3[targetCount];
        StickFollowSpace activeHandSpace = GetActiveHandFollowSpace();

        DebugLog($"SniperDefense hand base save start. targetCount={targetCount}, useSeparateHandFollowSettings={useSeparateHandFollowSettings}, activeSpace={activeHandSpace}");

        for (int i = 0; i < targetCount; i++)
        {
            Transform followTarget = sniperDefenseHandFollowTargets[i];
            if (followTarget == null)
            {
                DebugLog($"SniperDefense hand base skipped. index={i}, target=None");
                continue;
            }

            sniperDefenseHandBasePositions[i] = GetFollowPosition(followTarget, activeHandSpace);
            DebugLog($"SniperDefense hand base saved. index={i}, target={followTarget.name}, local={FormatVector(followTarget.localPosition)}, world={FormatVector(followTarget.position)}, saved={FormatVector(sniperDefenseHandBasePositions[i])}");
        }

        hasSniperDefenseHandBasePositions = true;
    }

    private void UpdateSniperDefenseHandFollowTargets(float targetOffset, float smoothSpeed, StickFollowSpace followSpace, StickFollowAxis followAxis, bool logThisFrame)
    {
        if (sniperDefenseHandFollowTargets == null || sniperDefenseHandBasePositions == null)
        {
            if (logThisFrame)
            {
                DebugLog("Hand follow update skipped. target array or base positions are null.");
            }
            return;
        }

        int targetCount = Mathf.Min(sniperDefenseHandFollowTargets.Length, sniperDefenseHandBasePositions.Length);
        if (logThisFrame)
        {
            DebugLog($"Hand follow update entered. targetCount={targetCount}, offset={targetOffset:F4}, space={followSpace}, axis={followAxis}");
        }

        for (int i = 0; i < targetCount; i++)
        {
            UpdateFollowTarget(sniperDefenseHandFollowTargets[i], sniperDefenseHandBasePositions[i], true, targetOffset, smoothSpeed, followSpace, followAxis, logThisFrame);
        }
    }

    private void RestoreSniperDefenseHandFollowTargets()
    {
        if (sniperDefenseHandFollowTargets == null || sniperDefenseHandBasePositions == null)
        {
            return;
        }

        StickFollowSpace activeHandSpace = GetActiveHandFollowSpace();
        int targetCount = Mathf.Min(sniperDefenseHandFollowTargets.Length, sniperDefenseHandBasePositions.Length);
        for (int i = 0; i < targetCount; i++)
        {
            Transform followTarget = sniperDefenseHandFollowTargets[i];
            if (followTarget == null)
            {
                continue;
            }

            SetFollowPosition(followTarget, sniperDefenseHandBasePositions[i], activeHandSpace);
        }
    }

    private StickFollowSpace GetActiveHandFollowSpace()
    {
        return useSeparateHandFollowSettings ? handFollowSpace : stickFollowSpace;
    }

    private StickFollowAxis GetActiveHandFollowAxis()
    {
        return useSeparateHandFollowSettings ? handFollowAxis : stickFollowAxis;
    }

    private bool ShouldLogSniperDefenseFollow()
    {
        if (!enableDebugLog)
        {
            return false;
        }

        if (Time.time < nextSniperDefenseFollowLogTime)
        {
            return false;
        }

        nextSniperDefenseFollowLogTime = Time.time + 1f;
        return true;
    }

    private void UpdateFollowTarget(
        Transform followTarget,
        Vector3 basePosition,
        bool hasBasePosition,
        float targetOffset,
        float smoothSpeed,
        StickFollowSpace followSpace,
        StickFollowAxis followAxis,
        bool logThisFrame)
    {
        if (followTarget == null || !hasBasePosition)
        {
            if (logThisFrame)
            {
                DebugLog($"Follow target skipped. target={(followTarget != null ? followTarget.name : "None")}, hasBasePosition={hasBasePosition}");
            }
            return;
        }

        Vector3 beforeLocalPosition = followTarget.localPosition;
        Vector3 beforeWorldPosition = followTarget.position;
        float baseValue = GetAxisValue(basePosition, followAxis);
        float targetValue = baseValue + targetOffset;
        Vector3 currentPosition = GetFollowPosition(followTarget, followSpace);
        float currentValue = GetAxisValue(currentPosition, followAxis);

        if (smoothSpeed <= 0f)
        {
            currentValue = targetValue;
        }
        else
        {
            currentValue = Mathf.MoveTowards(currentValue, targetValue, smoothSpeed * Time.deltaTime);
        }

        SetAxisValue(ref currentPosition, currentValue, followAxis);
        SetFollowPosition(followTarget, currentPosition, followSpace);

        if (logThisFrame)
        {
            DebugLog($"Follow target updated. target={followTarget.name}, space={followSpace}, axis={followAxis}, offset={targetOffset:F4}, base={FormatVector(basePosition)}, beforeLocal={FormatVector(beforeLocalPosition)}, afterLocal={FormatVector(followTarget.localPosition)}, beforeWorld={FormatVector(beforeWorldPosition)}, afterWorld={FormatVector(followTarget.position)}");
        }
    }

    private Vector3 GetFollowPosition(Transform followTarget, StickFollowSpace followSpace)
    {
        return followSpace == StickFollowSpace.World
            ? followTarget.position
            : followTarget.localPosition;
    }

    private void SetFollowPosition(Transform followTarget, Vector3 position, StickFollowSpace followSpace)
    {
        if (followSpace == StickFollowSpace.World)
        {
            followTarget.position = position;
        }
        else
        {
            followTarget.localPosition = position;
        }
    }

    private float GetAxisValue(Vector3 position, StickFollowAxis followAxis)
    {
        switch (followAxis)
        {
            case StickFollowAxis.X:
                return position.x;
            case StickFollowAxis.Z:
                return position.z;
            default:
                return position.y;
        }
    }

    private void SetAxisValue(ref Vector3 position, float value, StickFollowAxis followAxis)
    {
        switch (followAxis)
        {
            case StickFollowAxis.X:
                position.x = value;
                break;
            case StickFollowAxis.Z:
                position.z = value;
                break;
            default:
                position.y = value;
                break;
        }
    }

    private int GetValidHandFollowTargetCount()
    {
        if (sniperDefenseHandFollowTargets == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < sniperDefenseHandFollowTargets.Length; i++)
        {
            if (sniperDefenseHandFollowTargets[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private string FormatVector(Vector3 value)
    {
        return $"({value.x:F4}, {value.y:F4}, {value.z:F4})";
    }

    private void LogSniperDefenseFollowSetup()
    {
        int handTargetCount = GetValidHandFollowTargetCount();
        string handMode = useSeparateHandFollowSettings ? "Separate" : "SameAsStick";

        DebugLog($"SniperDefense follow setup. StickTarget={(sniperDefenseStickTarget != null ? sniperDefenseStickTarget.name : "None")}, HandTargets={handTargetCount}, HandMode={handMode}");

        if (sniperDefenseStickTarget == null)
        {
            Debug.LogWarning("Sniper Defense Stick Target is not assigned. Assign road_sign_metal_pole if the stick should move.", this);
        }

        if (handTargetCount == 0)
        {
            Debug.LogWarning("Sniper Defense Hand Follow Targets is empty. Assign LeftHandTarget / RightHandTarget if the arms should follow.", this);
        }
    }

    private float GetNormalizedPointPosition01()
    {
        float min = GetMinPointPosition();
        float max = GetMaxPointPosition();

        if (Mathf.Approximately(min, max))
        {
            return 0.5f;
        }

        return Mathf.InverseLerp(min, max, pointAxisPosition);
    }

    private void MoveTargetZone()
    {
        targetAxisPosition += targetMoveDirection * targetMoveSpeed * Time.deltaTime;

        float min = GetMinTargetPosition();
        float max = GetMaxTargetPosition();

        // 端まで来たら反転します。PingPongよりInspector調整時の挙動が読みやすい形にしています。
        if (targetAxisPosition > max)
        {
            targetAxisPosition = max;
            targetMoveDirection = -1f;
        }
        else if (targetAxisPosition < min)
        {
            targetAxisPosition = min;
            targetMoveDirection = 1f;
        }
    }

    private void UpdateBalanceState()
    {
        float targetHalfSize = GetAxisSize(targetZone) * 0.5f;
        float min = targetAxisPosition - targetHalfSize;
        float max = targetAxisPosition + targetHalfSize;

        isInsideTarget = pointAxisPosition >= min && pointAxisPosition <= max;

        // 状態が変わった瞬間だけイベントを呼びます。
        if (isInsideTarget != wasInsideTarget)
        {
            DebugLog($"Balance state changed: IsInsideTarget={isInsideTarget}, point={pointAxisPosition:F2}, targetMin={min:F2}, targetMax={max:F2}, targetCenter={targetAxisPosition:F2}");

            if (playMode == BalancePlayMode.Normal && !useRandomTargetChallenge && isInsideTarget)
            {
                onBalanceSuccess?.Invoke();
            }
            else if (playMode == BalancePlayMode.Normal && !useRandomTargetChallenge)
            {
                onBalanceMiss?.Invoke();
            }

            wasInsideTarget = isInsideTarget;
        }
    }

    private void UpdateFailureTimer()
    {
        if (isInsideTarget)
        {
            if (outsideTimer > 0f)
            {
                DebugLog($"outsideTimer reset: point returned inside target. outsideTimer was {outsideTimer:F2}");
            }

            outsideTimer = 0f;
            return;
        }

        outsideTimer += Time.deltaTime;
        LogOutsideTimer();

        // 一定時間外れたら、ふらつきとダメージ通知を発生させます。
        if (outsideTimer >= failTimeLimit)
        {
            DebugLog($"Damage triggered: outsideTimer={outsideTimer:F2}, failTimeLimit={failTimeLimit:F2}, damage={damageAmount}");

            outsideTimer = 0f;
            StartWobble();
            onDamage?.Invoke(damageAmount);
            PlayUnbalance();

            if (resetPointAfterDamage)
            {
                DebugLog("BalancePoint reset to center after damage.");
                pointAxisPosition = 0f;
            }
        }
    }

    private void UpdateRandomChallenge()
    {
        if (isInsideTarget)
        {
            HandleChallengeSuccess();
            return;
        }

        challengeTimer += Time.deltaTime;
        UpdateTimerText();

        if (challengeTimer >= challengeTimeLimit)
        {
            HandleChallengeFailure();
        }
    }

    private void HandleChallengeSuccess()
    {
        DebugLog($"Random challenge success. point={pointAxisPosition:F2}, target={targetAxisPosition:F2}");
        onBalanceSuccess?.Invoke();
        challengeTimer = 0f;

        if (randomizeTargetAfterSuccess)
        {
            RandomizeTargetZone();
        }

        ApplyAllUiPositions();
        UpdateBalanceState();
        wasInsideTarget = isInsideTarget;
        UpdateTimerText();
    }

    private void HandleChallengeFailure()
    {
        DebugLog($"Random challenge failed. challengeTimer={challengeTimer:F2}, limit={challengeTimeLimit:F2}");

        challengeTimer = 0f;
        outsideTimer = 0f;
        StartWobble();
        onBalanceMiss?.Invoke();
        onDamage?.Invoke(damageAmount);
        PlayUnbalance();

        if (resetPointAfterDamage)
        {
            pointAxisPosition = 0f;
        }

        if (randomizeTargetAfterFailure)
        {
            RandomizeTargetZone();
        }

        ApplyAllUiPositions();
        UpdateBalanceState();
        wasInsideTarget = isInsideTarget;
        UpdateTimerText();
    }

    private void ApplyBalanceMissReaction(bool invokeBalanceMiss)
    {
        StartWobble();

        if (invokeBalanceMiss)
        {
            onBalanceMiss?.Invoke();
        }

        onDamage?.Invoke(damageAmount);
        PlayUnbalance();
    }

    private void RandomizeTargetZone()
    {
        float min = GetMinTargetPosition();
        float max = GetMaxTargetPosition();

        if (max < min)
        {
            targetAxisPosition = 0f;
            return;
        }

        float newPosition = Random.Range(min, max);

        for (int i = 0; i < 10; i++)
        {
            if (Mathf.Abs(newPosition - pointAxisPosition) >= randomTargetMinDistanceFromPoint)
            {
                break;
            }

            newPosition = Random.Range(min, max);
        }

        targetAxisPosition = newPosition;
        DebugLog($"Target randomized. target={targetAxisPosition:F2}");
    }

    private void UpdateTimerText()
    {
        if (!useRandomTargetChallenge)
        {
            SetTimerText(string.Empty);
            return;
        }

        string text = string.Format(timerFormat, ChallengeRemainingTime);
        SetTimerText(text);
    }

    private void SetTimerText(string text)
    {
        if (balanceTimerText != null)
        {
            balanceTimerText.text = text;
        }

        if (balanceTimerTmpText != null)
        {
            balanceTimerTmpText.text = text;
        }
    }

    private void AutoAssignTimerText()
    {
        if (balanceTimerText != null || balanceTimerTmpText != null)
        {
            return;
        }

        GameObject timerObject = GameObject.Find("BalanceTimerText");
        if (timerObject == null)
        {
            return;
        }

        balanceTimerText = timerObject.GetComponent<Text>();
        balanceTimerTmpText = timerObject.GetComponent<TMP_Text>();
    }

    private void AutoAssignBalanceUi()
    {
        if (targetZone == null)
        {
            GameObject targetObject = GameObject.Find("TargetYellowArea");
            if (targetObject != null)
            {
                targetZone = targetObject.GetComponent<RectTransform>();
            }
        }

        if (balancePoint == null)
        {
            GameObject markerObject = GameObject.Find("PlayerMarker");
            if (markerObject != null)
            {
                balancePoint = markerObject.GetComponent<RectTransform>();
            }
        }

        if (gaugeRoot == null)
        {
            GameObject gaugeObject = GameObject.Find("BalanceGauge");
            if (gaugeObject != null)
            {
                gaugeRoot = gaugeObject.GetComponent<RectTransform>();
            }
        }
    }

    private void PlayUnbalance()
    {
        // Animator参照がない場合、SetBoolできないのでログを出して止めます。
        // ここが出たらInspectorのanimator欄にSuitのAnimatorを入れてください。
        if (animator == null)
        {
            DebugLog("Animator is null. Unbalance bool was not set.");
            return;
        }

        // パラメーター名が空だとAnimatorへ正しく命令できないので止めます。
        if (string.IsNullOrEmpty(unbalanceBoolName))
        {
            DebugLog("Unbalance bool name is empty. Animator.SetBool was skipped.");
            return;
        }

        // すでに戻すCoroutineが動いている場合は一度止めます。
        // 複数のCoroutineが同時にfalseを入れると、意図しないタイミングで戻ることがあるためです。
        if (unbalanceCoroutine != null)
        {
            StopCoroutine(unbalanceCoroutine);
            DebugLog("Previous StopUnbalance coroutine stopped before starting a new one.");
        }

        DebugLog($"Animator.SetBool({unbalanceBoolName}, true)");
        animator.SetBool(unbalanceBoolName, true);
        unbalanceCoroutine = StartCoroutine(StopUnbalance());
        DebugLog("StopUnbalance coroutine started.");
    }

    private void CheckAnimatorSetup()
    {
        // Animatorが未設定なら、ここで分かるようにします。
        // Unbalanceが再生されない時は、まずこのログを確認してください。
        if (animator == null)
        {
            DebugLog("Animator setup check: animator is null. Assign the player's Animator in Inspector.");
            return;
        }

        // Animator Controllerに指定したbool名が存在するか確認します。
        // 名前が1文字でも違うとSetBoolしても意図した遷移が起きません。
        if (!HasAnimatorBoolParameter(unbalanceBoolName))
        {
            DebugLog($"Animator setup check: bool parameter '{unbalanceBoolName}' was not found.");
            return;
        }

        DebugLog($"Animator setup check: bool parameter '{unbalanceBoolName}' was found.");
    }

    private bool HasAnimatorBoolParameter(string parameterName)
    {
        // Animatorが無い、または名前が空なら確認できません。
        if (animator == null || string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        // animator.parametersにはAnimator Controllerに登録されているParameter一覧が入っています。
        // ここを調べることで、Inspector側の名前ミスを見つけやすくします。
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void StartWobble()
    {
        if (wobbleTarget == null)
        {
            return;
        }

        wobbleBaseRotation = wobbleTarget.localRotation;
        wobbleTimer = wobbleDuration;
    }

    private void UpdateWobble()
    {
        if (wobbleTarget == null || wobbleTimer <= 0f)
        {
            return;
        }

        wobbleTimer -= Time.deltaTime;

        float rate = wobbleDuration > 0f ? wobbleTimer / wobbleDuration : 0f;
        float angle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleAngle * rate;

        // Z回転だけを足して、左右にふらついて見えるようにします。
        wobbleTarget.localRotation = wobbleBaseRotation * Quaternion.Euler(0f, 0f, angle);

        if (wobbleTimer <= 0f)
        {
            wobbleTarget.localRotation = wobbleBaseRotation;
        }
    }

    private void ApplyAllUiPositions()
    {
        SetAxisAnchoredPosition(balancePoint, pointAxisPosition);
        SetAxisAnchoredPosition(targetZone, targetAxisPosition);
    }

    private void SaveHorizontalLayoutIfNeeded()
    {
        if (hasSavedHorizontalLayout)
        {
            return;
        }

        RectTransform root = GetEventVerticalLayoutRoot(false);
        if (root != null)
        {
            savedGaugeRootLayout = new RectTransformLayout(root);
        }

        if (balanceBar != null)
        {
            savedBalanceBarLayout = new RectTransformLayout(balanceBar);
        }

        if (targetZone != null)
        {
            savedTargetZoneLayout = new RectTransformLayout(targetZone);
        }

        if (balancePoint != null)
        {
            savedBalancePointLayout = new RectTransformLayout(balancePoint);
        }

        hasSavedHorizontalLayout = true;
    }

    private void ApplyEventVerticalLayout()
    {
        RectTransform root = GetEventVerticalLayoutRoot(true);
        if (root == null)
        {
            Debug.LogWarning("[BalanceManager] ApplyEventVerticalLayout skipped because gaugeRoot is None and no safe fallback root was found.", this);
            return;
        }

        Vector2 beforePosition = root.anchoredPosition;

        root.anchorMin = eventVerticalAnchorMin;
        root.anchorMax = eventVerticalAnchorMax;
        root.pivot = eventVerticalPivot;
        root.anchoredPosition = eventVerticalAnchoredPosition;
        root.localRotation = Quaternion.Euler(0f, 0f, eventVerticalRotationZ);
        root.localScale = eventVerticalScale;

        Debug.Log($"[BalanceManager] ApplyEventVerticalLayout target={root.name} before={FormatVector2(beforePosition)} after={FormatVector2(root.anchoredPosition)} setting={FormatVector2(eventVerticalAnchoredPosition)}", this);
    }

    private RectTransform GetEventVerticalLayoutRoot(bool logWarning)
    {
        if (gaugeRoot != null)
        {
            return gaugeRoot;
        }

        gaugeRoot = GetParentRectTransform(balanceBar);
        if (gaugeRoot != null)
        {
            return gaugeRoot;
        }

        gaugeRoot = GetParentRectTransform(targetZone);
        if (gaugeRoot != null)
        {
            return gaugeRoot;
        }

        gaugeRoot = GetParentRectTransform(balancePoint);
        if (gaugeRoot != null)
        {
            return gaugeRoot;
        }

        if (logWarning)
        {
            Debug.LogWarning("[BalanceManager] gaugeRoot is None. Assign Canvas > BalanceGauge to Gauge Root so Event Vertical Layout can move the whole gauge.", this);
        }

        return null;
    }

    private RectTransform GetParentRectTransform(RectTransform child)
    {
        if (child == null)
        {
            return null;
        }

        return child.parent as RectTransform;
    }

    private string FormatVector2(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }

    private void RestoreHorizontalLayout()
    {
        if (!hasSavedHorizontalLayout)
        {
            return;
        }

        if (gaugeRoot != null)
        {
            savedGaugeRootLayout.ApplyTo(gaugeRoot);
        }

        if (balanceBar != null)
        {
            savedBalanceBarLayout.ApplyTo(balanceBar);
        }

        if (targetZone != null)
        {
            savedTargetZoneLayout.ApplyTo(targetZone);
        }

        if (balancePoint != null)
        {
            savedBalancePointLayout.ApplyTo(balancePoint);
        }
    }

    private float GetMinPointPosition()
    {
        return -GetGaugeHalfLength() + GetAxisSize(balancePoint) * 0.5f;
    }

    private float GetMaxPointPosition()
    {
        return GetGaugeHalfLength() - GetAxisSize(balancePoint) * 0.5f;
    }

    private float GetMinTargetPosition()
    {
        float usableHalfLength = GetGaugeHalfLength() * Mathf.Clamp01(targetMoveRangeRate);
        return -usableHalfLength + GetAxisSize(targetZone) * 0.5f;
    }

    private float GetMaxTargetPosition()
    {
        float usableHalfLength = GetGaugeHalfLength() * Mathf.Clamp01(targetMoveRangeRate);
        return usableHalfLength - GetAxisSize(targetZone) * 0.5f;
    }

    private float GetGaugeHalfLength()
    {
        if (balanceBar == null)
        {
            return 0f;
        }

        return GetAxisSize(balanceBar) * 0.5f;
    }

    private float GetAxisSize(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        return gaugeDirection == BalanceGaugeDirection.Horizontal
            ? rectTransform.rect.width
            : rectTransform.rect.height;
    }

    private float GetAxisAnchoredPosition(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return 0f;
        }

        return gaugeDirection == BalanceGaugeDirection.Horizontal
            ? rectTransform.anchoredPosition.x
            : rectTransform.anchoredPosition.y;
    }

    private void SetAxisAnchoredPosition(RectTransform rectTransform, float axisPosition)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 anchoredPosition = rectTransform.anchoredPosition;

        if (gaugeDirection == BalanceGaugeDirection.Horizontal)
        {
            anchoredPosition.x = axisPosition;
        }
        else
        {
            anchoredPosition.y = axisPosition;
        }

        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void SetCrossAxisAnchoredPosition(RectTransform rectTransform, float crossAxisPosition)
    {
        if (rectTransform == null)
        {
            return;
        }

        Vector2 anchoredPosition = rectTransform.anchoredPosition;

        if (gaugeDirection == BalanceGaugeDirection.Horizontal)
        {
            anchoredPosition.y = crossAxisPosition;
        }
        else
        {
            anchoredPosition.x = crossAxisPosition;
        }

        rectTransform.anchoredPosition = anchoredPosition;
    }

    private IEnumerator StopUnbalance()
    {
        // 指定秒数だけ待ちます。
        // この間はUnbalance boolをtrueのままにして、AnimatorがUnbalanceへ遷移できる時間を作ります。
        DebugLog($"StopUnbalance coroutine waiting {unbalanceDuration:F2} seconds.");
        yield return new WaitForSeconds(unbalanceDuration);

        // 待っている間にAnimator参照が外れた場合に備えて確認します。
        if (animator == null)
        {
            DebugLog("Animator became null before setting Unbalance false.");
            unbalanceCoroutine = null;
            yield break;
        }

        // 通常状態へ戻すためにUnbalanceをfalseへ戻します。
        // ここ以外で毎フレームfalseにしないことで、アニメーションに入る前に戻される事故を防ぎます。
        DebugLog($"Animator.SetBool({unbalanceBoolName}, false)");
        animator.SetBool(unbalanceBoolName, false);
        unbalanceCoroutine = null;
        DebugLog("StopUnbalance coroutine finished.");
    }

    private void LogOutsideTimer()
    {
        // enableDebugLogがfalseなら何も出しません。
        if (!enableDebugLog)
        {
            return;
        }

        // Time.timeはゲーム開始からの経過秒数です。
        // nextTimerLogTimeより前なら、ログを出すタイミングではないので戻ります。
        if (Time.time < nextTimerLogTime)
        {
            return;
        }

        nextTimerLogTime = Time.time + timerLogInterval;
        DebugLog($"outsideTimer={outsideTimer:F2}/{failTimeLimit:F2}, point={pointAxisPosition:F2}, target={targetAxisPosition:F2}, IsInsideTarget={isInsideTarget}");
    }

    private void DebugLog(string message)
    {
        // 調査用ログのON/OFFをInspectorで切り替えられるようにしています。
        if (!enableDebugLog)
        {
            return;
        }

        Debug.Log($"[BalanceManager] {message}", this);
    }
}
