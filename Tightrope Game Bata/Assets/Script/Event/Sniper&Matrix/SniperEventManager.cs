using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

// スナイパーイベント全体の開始と終了を管理するスクリプトです。
public class SniperEventManager : MonoBehaviour
{
    [Header("Player")]
    // SampleSceneではSuitManに付いているTightropeAutoGoalMoverを設定します。
    [SerializeField] private TightropeAutoGoalMover playerMover;

    [Header("Balance Gauge")]
    // 通常バランス停止、縦ゲージ切り替え、防御判定に使います。
    [SerializeField] private BalanceManager balanceManager;

    [Header("Warning Laser")]
    // 通常視点で見せる警告レーザーです。
    [SerializeField] private SniperWarningLaserController warningLaserController;

    [Header("Sniper Warning Sound")]
    [SerializeField] private AudioSource sniperWarningAudioSource;
    [SerializeField] private AudioClip sniperWarningSound;
    [SerializeField] private float sniperWarningSoundVolume = 1.0f;

    [Header("Warning Paper")]
    // Canvas上で再生する紙演出です。
    [SerializeField] private SniperWarningPaperController warningPaperController;
    // 警告レーザーを見せてから紙演出を始めるまでの待ち時間です。
    [SerializeField] private float warningPaperDelay = 2.0f;

    [Header("Warning Paper Sound")]
    [SerializeField] private AudioSource paperStickAudioSource;
    [SerializeField] private AudioClip paperStickSound;
    [SerializeField] private float paperStickSoundVolume = 1.0f;

    [Header("Side View Phase")]
    // 紙演出後に切り替える横視点カメラです。
    [SerializeField] private SniperSideViewCameraController sideViewCameraController;
    // 横視点中に表示する専用レーザーです。
    [SerializeField] private SniperSideViewLaserController sideViewLaserController;
    // 横視点レーザーに沿って弾を飛ばす発射管理です。
    [SerializeField] private SniperBulletShooter sniperBulletShooter;

    [Header("Stick Break Phase")]
    // 棒破壊演出で非表示にする、プレイヤーが持っている棒です。
    [SerializeField] private GameObject stickObjectToHide;
    // 棒が消える瞬間に出すエフェクトPrefabです。
    [SerializeField] private GameObject stickBreakEffectPrefab;
    // エフェクトを出す位置です。未設定なら棒の位置を使います。
    [SerializeField] private Transform stickBreakEffectSpawnPoint;
    // 棒破壊音を鳴らすAudioSourceです。
    [SerializeField] private AudioSource stickBreakAudioSource;
    // 棒が壊れた時に鳴らす音です。
    [SerializeField] private AudioClip stickBreakSound;
    // 棒破壊演出後、次のフェーズへ進むまでの待ち時間です。
    [SerializeField] private float stickBreakWaitTime = 1.0f;
    // trueなら、棒破壊時に棒をSetActive(false)で非表示にします。
    [SerializeField] private bool hideStickOnBreak = true;

    [Header("Matrix Avoid Camera")]
    [Tooltip("棒破壊後に切り替える、マトリックス回避用のCinemachine Cameraです。")]
    [SerializeField] private CinemachineCamera matrixAvoidCamera;
    [Tooltip("マトリックス回避カメラへ切り替える時のPriorityです。横視点カメラより高い値にします。")]
    [SerializeField] private int matrixAvoidCameraPriority = 30;
    [Tooltip("マトリックス回避カメラを使わない時のPriorityです。")]
    [SerializeField] private int matrixAvoidInactiveCameraPriority = 0;

    [Header("Normal Camera Restore")]
    [Tooltip("スナイパーイベント終了後に戻す通常プレイヤー用カメラです。通常はManCameraを設定してください。")]
    [SerializeField] private CinemachineCamera normalPlayerCamera;
    [Tooltip("Normal Player Cameraが未設定の場合、この名前のCinemachine Cameraを自動検索します。")]
    [SerializeField] private string normalPlayerCameraName = "ManCamera";
    [Tooltip("スナイパーイベント終了時に、プレイヤーカメラより優先度を下げる怪獣系カメラ名です。")]
    [SerializeField] private string[] dinoCameraNamesToLowerOnRestore = { "DinoCamera", "EventDinoCamera" };

    [Header("Matrix Avoid Bullets")]
    // マトリックス回避フェーズで、4発同時に飛ぶ弾を管理するShooterです。
    [SerializeField] private MatrixAvoidBulletShooter matrixAvoidBulletShooter;

    [Header("Matrix Avoid Animation")]
    // マトリックス回避フェーズで体を反るアニメーションを再生するAnimatorです。
    [SerializeField] private Animator matrixAvoidAnimator;
    // Animator Controller側に用意するTrigger名です。
    [SerializeField] private string matrixAvoidTriggerName = "MatrixAvoid";
    // Time.timeScaleは使わず、このAnimatorだけを遅く再生する速度です。
    [SerializeField] private float matrixAvoidAnimatorSpeed = 0.35f;
    // マトリックス回避終了時にAnimator.speedを開始前へ戻すかどうかです。
    [SerializeField] private bool restoreAnimatorSpeedOnMatrixAvoidEnd = true;

    [Header("Debug")]
    [SerializeField] private bool enableDebugSniperEventHotkeys = false;

    private bool isSniperEventActive;
    private Coroutine warningPaperCoroutine;
    private Coroutine stickBreakCoroutine;
    private SniperBulletShooter subscribedBulletShooter;
    private MatrixAvoidBulletShooter subscribedMatrixAvoidBulletShooter;
    private bool hasSavedStickActiveState;
    private bool stickActiveStateBeforeBreak;
    private int originalMatrixAvoidCameraPriority;
    private bool hasSavedMatrixAvoidCameraPriority;
    private bool isMatrixAvoidCameraActive;
    private float originalMatrixAvoidAnimatorSpeed = 1f;
    private bool hasSavedMatrixAvoidAnimatorSpeed;
    private bool isMatrixAvoidAnimationPlaying;
    private bool hasPlayedSniperWarningSound;
    private bool hasPlayedPaperStickSound;
    private bool hasReturnedToNormalGameplay;
    private readonly List<SavedCameraPriority> savedSniperEventCameraPriorities = new List<SavedCameraPriority>();
    private bool hasSavedSniperEventCameraState;

    private struct SavedCameraPriority
    {
        public CinemachineCamera Camera;
        public int Priority;

        public SavedCameraPriority(CinemachineCamera camera)
        {
            Camera = camera;
            Priority = camera.Priority.Value;
        }
    }

    // 外からイベント中か確認するための読み取り専用プロパティです。
    public bool IsSniperEventActive => isSniperEventActive;

    private void Reset()
    {
        AutoFindPlayerMover();
        AutoFindBalanceManager();
        AutoFindWarningLaserController();
        AutoFindWarningPaperController();
        AutoFindSideViewControllers();
        AutoFindMatrixAvoidBulletShooter();
    }

    private void Awake()
    {
        if (playerMover == null)
        {
            AutoFindPlayerMover();
        }

        if (warningLaserController == null)
        {
            AutoFindWarningLaserController();
        }

        if (balanceManager == null)
        {
            AutoFindBalanceManager();
        }

        if (warningPaperController == null)
        {
            AutoFindWarningPaperController();
        }

        if (sideViewCameraController == null || sideViewLaserController == null || sniperBulletShooter == null)
        {
            AutoFindSideViewControllers();
        }

        if (matrixAvoidBulletShooter == null)
        {
            AutoFindMatrixAvoidBulletShooter();
        }

        SubscribeBulletShooterEvents();
        SubscribeMatrixAvoidBulletShooterEvents();
        SaveMatrixAvoidCameraPriorityIfNeeded();
    }

    private void OnEnable()
    {
        SubscribeBulletShooterEvents();
        SubscribeMatrixAvoidBulletShooterEvents();
    }

    private void OnDisable()
    {
        UnsubscribeBulletShooterEvents();
        UnsubscribeMatrixAvoidBulletShooterEvents();
    }

    private void Update()
    {
        if (!enableDebugSniperEventHotkeys)
        {
            return;
        }

        // デバッグ用の仮入力です。後で正式なイベント開始方法ができたら削除します。
        if (Input.GetKeyDown(KeyCode.F))
        {
            StartSniperEvent();
        }

        // デバッグ用の仮入力です。Gキーでイベントを終了します。
        if (Input.GetKeyDown(KeyCode.G))
        {
            EndSniperEvent();
        }
    }

    // スナイパーイベントを開始します。
    public void StartSniperEvent()
    {
        if (isSniperEventActive)
        {
            return;
        }

        isSniperEventActive = true;
        hasReturnedToNormalGameplay = false;
        hasPlayedSniperWarningSound = false;
        hasPlayedPaperStickSound = false;
        SaveSniperEventCameraState();
        StopPlayerAutoMove();
        OnSniperEventStarted();
    }

    // スナイパーイベントを終了し、通常状態へ戻します。
    public void EndSniperEvent()
    {
        if (!isSniperEventActive)
        {
            return;
        }

        ReturnToNormalGameplay();
    }

    private void ReturnToNormalGameplay()
    {
        if (hasReturnedToNormalGameplay)
        {
            return;
        }

        if (!isSniperEventActive)
        {
            return;
        }

        Debug.Log("SniperEventManager: Returning to normal gameplay.", this);
        hasReturnedToNormalGameplay = true;
        isSniperEventActive = false;
        hasPlayedSniperWarningSound = false;
        hasPlayedPaperStickSound = false;
        ResumePlayerAutoMove();
        OnSniperEventEnded();
        Debug.Log("SniperEventManager: Sniper event finished.", this);
    }

    private void StopPlayerAutoMove()
    {
        if (playerMover == null)
        {
            Debug.LogWarning("SniperEventManager: PlayerMover が設定されていません。", this);
            return;
        }

        playerMover.PlayerStoping = true;
    }

    private void ResumePlayerAutoMove()
    {
        if (playerMover == null)
        {
            return;
        }

        playerMover.PlayerStoping = false;
        Debug.Log("SniperEventManager: Player control restored.", this);
    }

    private void OnSniperEventStarted()
    {
        SaveStickActiveStateForSniperEvent();
        HideNormalCountUiForSniperEvent();
        PauseNormalBalanceGauge();

        PlaySniperWarningSound();

        if (warningLaserController != null)
        {
            warningLaserController.ShowWarningLasers();
        }

        StartWarningPaperAfterDelay();
    }

    private void OnSniperEventEnded()
    {
        CancelWarningPaperDelay();

        if (warningLaserController != null)
        {
            warningLaserController.HideWarningLasers();
        }

        if (warningPaperController != null)
        {
            warningPaperController.HidePaperImmediately();
        }

        EndSideViewPhase();
        RestoreMatrixAvoidCameraPriority();
        RestoreNormalPlayerCameraState();
        RestoreMatrixAvoidAnimatorSpeed();
        StopMatrixAvoidBullets();
        ReturnBalanceManagerToNormalState();
        RestoreNormalCountUiAfterSniperEvent();
        CancelStickBreakSequence();
        RestoreStickObjectToSavedState();
    }

    private void ReturnBalanceManagerToNormalState()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.DisableSniperDefenseMode();
        balanceManager.DisableMatrixAvoidMode();
        balanceManager.ResumeNormalBalanceGauge();
        Debug.Log("SniperEventManager: BalanceManager returned to normal mode.", this);
    }

    private void AutoFindPlayerMover()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMover = playerObject.GetComponent<TightropeAutoGoalMover>();
        }

        if (playerMover == null)
        {
            playerMover = FindFirstObjectByType<TightropeAutoGoalMover>();
        }
    }

    private void AutoFindWarningLaserController()
    {
        warningLaserController = FindFirstObjectByType<SniperWarningLaserController>();
    }

    private void AutoFindWarningPaperController()
    {
        warningPaperController = FindFirstObjectByType<SniperWarningPaperController>();
    }

    private void AutoFindSideViewControllers()
    {
        if (sideViewCameraController == null)
        {
            sideViewCameraController = FindFirstObjectByType<SniperSideViewCameraController>();
        }

        if (sideViewLaserController == null)
        {
            sideViewLaserController = FindFirstObjectByType<SniperSideViewLaserController>();
        }

        if (sniperBulletShooter == null)
        {
            sniperBulletShooter = FindFirstObjectByType<SniperBulletShooter>();
        }
    }

    private void AutoFindMatrixAvoidBulletShooter()
    {
        matrixAvoidBulletShooter = FindFirstObjectByType<MatrixAvoidBulletShooter>();
    }

    private void AutoFindNormalPlayerCamera(CinemachineCamera[] sceneCameras)
    {
        if (normalPlayerCamera != null)
        {
            return;
        }

        if (string.IsNullOrEmpty(normalPlayerCameraName))
        {
            return;
        }

        for (int i = 0; i < sceneCameras.Length; i++)
        {
            CinemachineCamera sceneCamera = sceneCameras[i];
            if (sceneCamera != null && sceneCamera.name == normalPlayerCameraName)
            {
                normalPlayerCamera = sceneCamera;
                return;
            }
        }
    }

    private void SaveSniperEventCameraState()
    {
        savedSniperEventCameraPriorities.Clear();

        CinemachineCamera[] sceneCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AutoFindNormalPlayerCamera(sceneCameras);

        for (int i = 0; i < sceneCameras.Length; i++)
        {
            CinemachineCamera sceneCamera = sceneCameras[i];
            if (sceneCamera == null)
            {
                continue;
            }

            savedSniperEventCameraPriorities.Add(new SavedCameraPriority(sceneCamera));
        }

        hasSavedSniperEventCameraState = true;
        Debug.Log($"SniperEventManager: Sniper event camera state saved. cameraCount={savedSniperEventCameraPriorities.Count}", this);
    }

    private void RestoreNormalPlayerCameraState()
    {
        if (!hasSavedSniperEventCameraState)
        {
            Debug.LogWarning("SniperEventManager: スナイパーイベント開始前のカメラ状態が保存されていません。カメラ復元をスキップします。", this);
            return;
        }

        Debug.Log("SniperEventManager: Restoring normal player camera.", this);

        for (int i = 0; i < savedSniperEventCameraPriorities.Count; i++)
        {
            SavedCameraPriority savedCameraPriority = savedSniperEventCameraPriorities[i];
            if (savedCameraPriority.Camera == null)
            {
                continue;
            }

            savedCameraPriority.Camera.Priority.Value = savedCameraPriority.Priority;
        }

        CinemachineCamera[] sceneCameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        AutoFindNormalPlayerCamera(sceneCameras);

        if (normalPlayerCamera == null)
        {
            Debug.LogWarning("SniperEventManager: 通常プレイヤーカメラが見つかりません。Normal Player Camera に ManCamera を設定してください。", this);
            savedSniperEventCameraPriorities.Clear();
            hasSavedSniperEventCameraState = false;
            return;
        }

        int playerPriority = Mathf.Max(normalPlayerCamera.Priority.Value, 15);
        normalPlayerCamera.Priority.Value = playerPriority;

        LowerDinoCamerasBelowPlayer(sceneCameras, playerPriority);

        int highestOtherPriority = GetHighestCameraPriorityExcept(sceneCameras, normalPlayerCamera);
        if (normalPlayerCamera.Priority.Value <= highestOtherPriority)
        {
            normalPlayerCamera.Priority.Value = highestOtherPriority + 1;
        }

        Debug.Log($"SniperEventManager: {normalPlayerCamera.name} priority restored to {normalPlayerCamera.Priority.Value}.", this);

        savedSniperEventCameraPriorities.Clear();
        hasSavedSniperEventCameraState = false;
    }

    private void LowerDinoCamerasBelowPlayer(CinemachineCamera[] sceneCameras, int playerPriority)
    {
        for (int i = 0; i < sceneCameras.Length; i++)
        {
            CinemachineCamera sceneCamera = sceneCameras[i];
            if (sceneCamera == null || sceneCamera == normalPlayerCamera || !ShouldLowerDinoCamera(sceneCamera.name))
            {
                continue;
            }

            sceneCamera.Priority.Value = Mathf.Min(sceneCamera.Priority.Value, playerPriority - 1);
            Debug.Log($"SniperEventManager: {sceneCamera.name} priority lowered to {sceneCamera.Priority.Value}.", this);
        }
    }

    private bool ShouldLowerDinoCamera(string cameraName)
    {
        if (dinoCameraNamesToLowerOnRestore == null)
        {
            return false;
        }

        for (int i = 0; i < dinoCameraNamesToLowerOnRestore.Length; i++)
        {
            string dinoCameraName = dinoCameraNamesToLowerOnRestore[i];
            if (!string.IsNullOrEmpty(dinoCameraName) && cameraName == dinoCameraName)
            {
                return true;
            }
        }

        return false;
    }

    private int GetHighestCameraPriorityExcept(CinemachineCamera[] sceneCameras, CinemachineCamera excludedCamera)
    {
        int highestPriority = int.MinValue;

        for (int i = 0; i < sceneCameras.Length; i++)
        {
            CinemachineCamera sceneCamera = sceneCameras[i];
            if (sceneCamera == null || sceneCamera == excludedCamera)
            {
                continue;
            }

            highestPriority = Mathf.Max(highestPriority, sceneCamera.Priority.Value);
        }

        return highestPriority == int.MinValue ? 0 : highestPriority;
    }

    private void AutoFindBalanceManager()
    {
        balanceManager = FindFirstObjectByType<BalanceManager>();
    }

    private void PauseNormalBalanceGauge()
    {
        if (balanceManager == null)
        {
            Debug.LogWarning("SniperEventManager: BalanceManager が設定されていません。", this);
            return;
        }

        balanceManager.PauseNormalBalanceGauge();
    }

    private void ResumeNormalBalanceGauge()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.ResumeNormalBalanceGauge();
    }

    private void HideNormalCountUiForSniperEvent()
    {
        if (balanceManager == null)
        {
            AutoFindBalanceManager();
        }

        if (balanceManager == null)
        {
            Debug.LogWarning("SniperEventManager: BalanceManager が設定されていません。通常カウント数字UIの非表示をスキップします。", this);
            return;
        }

        // 通常プレイ用のカウント数字だけを消し、スナイパー用ゲージ表示は残します。
        balanceManager.HideNormalCountUiForSniperEvent();
    }

    private void RestoreNormalCountUiAfterSniperEvent()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.RestoreNormalCountUiAfterSniperEvent();
    }

    private void StartWarningPaperAfterDelay()
    {
        CancelWarningPaperDelay();

        if (warningPaperController == null)
        {
            return;
        }

        warningPaperCoroutine = StartCoroutine(PlayWarningPaperAfterDelay());
    }

    private void PlaySniperWarningSound()
    {
        if (hasPlayedSniperWarningSound)
        {
            return;
        }

        hasPlayedSniperWarningSound = true;

        bool hasMissingAudioSetting = false;

        if (sniperWarningAudioSource == null)
        {
            Debug.LogWarning("SniperEventManager: sniperWarningAudioSource not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (sniperWarningSound == null)
        {
            Debug.LogWarning("SniperEventManager: sniperWarningSound not assigned.", this);
            hasMissingAudioSetting = true;
        }

        if (hasMissingAudioSetting)
        {
            return;
        }

        sniperWarningAudioSource.PlayOneShot(sniperWarningSound, sniperWarningSoundVolume);
        Debug.Log("SniperEventManager: Sniper warning sound played.", this);
    }

    private void CancelWarningPaperDelay()
    {
        if (warningPaperCoroutine == null)
        {
            return;
        }

        StopCoroutine(warningPaperCoroutine);
        warningPaperCoroutine = null;
    }

    private IEnumerator PlayWarningPaperAfterDelay()
    {
        yield return new WaitForSeconds(Mathf.Max(0f, warningPaperDelay));
        warningPaperCoroutine = null;

        if (!isSniperEventActive || warningPaperController == null)
        {
            yield break;
        }

        PlayPaperStickSound();
        warningPaperController.PlayPaperEffect(StartSideViewPhase);
    }

    private void PlayPaperStickSound()
    {
        if (hasPlayedPaperStickSound)
        {
            return;
        }

        hasPlayedPaperStickSound = true;

        bool hasMissingAudioSetting = false;

        if (paperStickAudioSource == null)
        {
            Debug.LogWarning("SniperEventManager: Paper Stick AudioSource が設定されていません。", this);
            hasMissingAudioSetting = true;
        }

        if (paperStickSound == null)
        {
            Debug.LogWarning("SniperEventManager: Paper Stick Sound が設定されていません。", this);
            hasMissingAudioSetting = true;
        }

        if (hasMissingAudioSetting)
        {
            return;
        }

        paperStickAudioSource.PlayOneShot(paperStickSound, paperStickSoundVolume);
    }

    private void StartSideViewPhase()
    {
        if (!isSniperEventActive)
        {
            return;
        }

        if (warningLaserController != null)
        {
            warningLaserController.HideWarningLasers();
        }

        SwitchToEventVerticalBalanceGauge();

        if (sideViewCameraController != null)
        {
            sideViewCameraController.EnterSideView();
        }

        if (sideViewLaserController != null)
        {
            sideViewLaserController.ShowSideViewLasers();
        }

        StartSideViewBulletShooting();
    }

    private void EndSideViewPhase()
    {
        RestoreNormalHorizontalBalanceGauge();
        StopSideViewBulletShooting();

        if (sideViewLaserController != null)
        {
            sideViewLaserController.HideSideViewLasers();
        }

        if (sideViewCameraController != null)
        {
            sideViewCameraController.ExitSideView();
        }

        Debug.Log("SniperEventManager: Side view camera exited.", this);
    }

    private void SwitchToEventVerticalBalanceGauge()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.SetEventVerticalLayoutActive(true);
    }

    private void RestoreNormalHorizontalBalanceGauge()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.SetEventVerticalLayoutActive(false);
    }

    private void StartSideViewBulletShooting()
    {
        if (sniperBulletShooter == null)
        {
            return;
        }

        SubscribeBulletShooterEvents();
        sniperBulletShooter.StartShooting();
    }

    private void StopSideViewBulletShooting()
    {
        if (sniperBulletShooter == null)
        {
            return;
        }

        sniperBulletShooter.StopShooting();
    }

    private void SubscribeBulletShooterEvents()
    {
        if (subscribedBulletShooter == sniperBulletShooter)
        {
            return;
        }

        UnsubscribeBulletShooterEvents();

        if (sniperBulletShooter == null)
        {
            return;
        }

        sniperBulletShooter.AllShotsResolved += OnSniperDefenseShotsFinished;
        subscribedBulletShooter = sniperBulletShooter;
    }

    private void UnsubscribeBulletShooterEvents()
    {
        if (subscribedBulletShooter == null)
        {
            return;
        }

        subscribedBulletShooter.AllShotsResolved -= OnSniperDefenseShotsFinished;
        subscribedBulletShooter = null;
    }

    private void OnSniperDefenseShotsFinished()
    {
        if (!isSniperEventActive)
        {
            return;
        }

        StartStickBreakSequence();
    }

    private void SubscribeMatrixAvoidBulletShooterEvents()
    {
        if (subscribedMatrixAvoidBulletShooter == matrixAvoidBulletShooter)
        {
            return;
        }

        UnsubscribeMatrixAvoidBulletShooterEvents();

        if (matrixAvoidBulletShooter == null)
        {
            return;
        }

        matrixAvoidBulletShooter.AllMatrixBulletsPassed += OnMatrixAvoidBulletsPassed;
        subscribedMatrixAvoidBulletShooter = matrixAvoidBulletShooter;
    }

    private void UnsubscribeMatrixAvoidBulletShooterEvents()
    {
        if (subscribedMatrixAvoidBulletShooter == null)
        {
            return;
        }

        subscribedMatrixAvoidBulletShooter.AllMatrixBulletsPassed -= OnMatrixAvoidBulletsPassed;
        subscribedMatrixAvoidBulletShooter = null;
    }

    private void OnMatrixAvoidBulletsPassed()
    {
        if (!isSniperEventActive)
        {
            return;
        }

        Debug.Log("SniperEventManager: MatrixAvoid succeeded by bullet pass.", this);

        if (balanceManager != null)
        {
            balanceManager.CompleteMatrixAvoidByBulletPass();
        }

        ReturnToNormalGameplay();
    }

    private void StartStickBreakSequence()
    {
        if (stickBreakCoroutine != null)
        {
            return;
        }

        stickBreakCoroutine = StartCoroutine(StickBreakSequence());
    }

    private IEnumerator StickBreakSequence()
    {
        Vector3 effectPosition = GetStickBreakEffectPosition();
        Quaternion effectRotation = GetStickBreakEffectRotation();

        PlayStickBreakEffect(effectPosition, effectRotation);
        PlayStickBreakSound();
        HideStickObjectForBreak();

        yield return new WaitForSeconds(Mathf.Max(0f, stickBreakWaitTime));
        stickBreakCoroutine = null;

        if (!isSniperEventActive)
        {
            yield break;
        }

        StartMatrixAvoidPhase();
    }

    private void CancelStickBreakSequence()
    {
        if (stickBreakCoroutine == null)
        {
            return;
        }

        StopCoroutine(stickBreakCoroutine);
        stickBreakCoroutine = null;
    }

    private void SaveStickActiveStateForSniperEvent()
    {
        if (stickObjectToHide == null)
        {
            hasSavedStickActiveState = false;
            return;
        }

        stickActiveStateBeforeBreak = stickObjectToHide.activeSelf;
        hasSavedStickActiveState = true;
    }

    private void HideStickObjectForBreak()
    {
        if (!hideStickOnBreak || stickObjectToHide == null)
        {
            return;
        }

        stickObjectToHide.SetActive(false);
    }

    private void RestoreStickObjectToSavedState()
    {
        if (stickObjectToHide == null || !hasSavedStickActiveState)
        {
            return;
        }

        stickObjectToHide.SetActive(stickActiveStateBeforeBreak);
        hasSavedStickActiveState = false;
    }

    private void PlayStickBreakEffect(Vector3 position, Quaternion rotation)
    {
        if (stickBreakEffectPrefab == null)
        {
            return;
        }

        GameObject effectObject = Instantiate(stickBreakEffectPrefab, position, rotation);
        ParticleSystem[] particleSystems = effectObject.GetComponentsInChildren<ParticleSystem>();
        for (int i = 0; i < particleSystems.Length; i++)
        {
            particleSystems[i].Play();
        }
    }

    private void PlayStickBreakSound()
    {
        if (stickBreakAudioSource == null || stickBreakSound == null)
        {
            return;
        }

        stickBreakAudioSource.PlayOneShot(stickBreakSound);
    }

    private Vector3 GetStickBreakEffectPosition()
    {
        if (stickBreakEffectSpawnPoint != null)
        {
            return stickBreakEffectSpawnPoint.position;
        }

        if (stickObjectToHide != null)
        {
            return stickObjectToHide.transform.position;
        }

        return transform.position;
    }

    private Quaternion GetStickBreakEffectRotation()
    {
        if (stickBreakEffectSpawnPoint != null)
        {
            return stickBreakEffectSpawnPoint.rotation;
        }

        if (stickObjectToHide != null)
        {
            return stickObjectToHide.transform.rotation;
        }

        return transform.rotation;
    }

    private void HideSniperDefenseLasersForMatrixAvoid()
    {
        if (sideViewLaserController == null)
        {
            AutoFindSideViewControllers();
        }

        if (sideViewLaserController == null)
        {
            Debug.LogWarning("SniperEventManager: SniperSideViewLaserController が設定されていません。MatrixAvoid開始時の赤い線非表示をスキップします。", this);
            return;
        }

        // 防御フェーズ用の赤いLineRendererは、マトリックス回避中には不要なのでここで消します。
        sideViewLaserController.HideSideViewLasers();
        Debug.Log("SniperEventManager: Sniper defense lasers hidden for MatrixAvoid.", this);
    }

    private void StartMatrixAvoidPhase()
    {
        Debug.Log("SniperEventManager: StartMatrixAvoidPhase called after stick break.", this);
        HideSniperDefenseLasersForMatrixAvoid();
        SwitchToMatrixAvoidCamera();
        StartMatrixAvoidGauge();
        StartMatrixAvoidAnimation();
        StartMatrixAvoidBullets();
    }

    private void StartMatrixAvoidGauge()
    {
        if (balanceManager == null)
        {
            Debug.LogWarning("SniperEventManager: BalanceManager が設定されていません。MatrixAvoidゲージを開始できません。", this);
            return;
        }

        balanceManager.EnableMatrixAvoidMode();
    }

    private void StopMatrixAvoidGauge()
    {
        if (balanceManager == null)
        {
            return;
        }

        balanceManager.DisableMatrixAvoidMode();
    }

    private void StartMatrixAvoidAnimation()
    {
        if (matrixAvoidAnimator == null)
        {
            Debug.LogWarning("SniperEventManager: Matrix Avoid Animator が設定されていません。アニメーション再生なしで進みます。", this);
            return;
        }

        if (!isMatrixAvoidAnimationPlaying)
        {
            originalMatrixAvoidAnimatorSpeed = matrixAvoidAnimator.speed;
            hasSavedMatrixAvoidAnimatorSpeed = true;
        }

        matrixAvoidAnimator.speed = Mathf.Max(0f, matrixAvoidAnimatorSpeed);
        isMatrixAvoidAnimationPlaying = true;
        Debug.Log($"SniperEventManager: MatrixAvoid animation started. Animator speed={matrixAvoidAnimator.speed:F2}", this);

        if (string.IsNullOrEmpty(matrixAvoidTriggerName))
        {
            Debug.LogWarning("SniperEventManager: Matrix Avoid Trigger Name が空です。Animator.speedだけ変更して進みます。", this);
            return;
        }

        matrixAvoidAnimator.SetTrigger(matrixAvoidTriggerName);
    }

    private void RestoreMatrixAvoidAnimatorSpeed()
    {
        if (!isMatrixAvoidAnimationPlaying)
        {
            return;
        }

        if (matrixAvoidAnimator != null && restoreAnimatorSpeedOnMatrixAvoidEnd && hasSavedMatrixAvoidAnimatorSpeed)
        {
            matrixAvoidAnimator.speed = originalMatrixAvoidAnimatorSpeed;
            Debug.Log($"SniperEventManager: MatrixAvoid animator speed restored. Animator speed={matrixAvoidAnimator.speed:F2}", this);
        }

        isMatrixAvoidAnimationPlaying = false;
        hasSavedMatrixAvoidAnimatorSpeed = false;
        Debug.Log("SniperEventManager: Animator speed restored.", this);
    }

    private void StartMatrixAvoidBullets()
    {
        if (matrixAvoidBulletShooter == null)
        {
            Debug.LogWarning("SniperEventManager: Matrix Avoid Bullet Shooter が設定されていません。弾発射なしで進みます。", this);
            return;
        }

        SubscribeMatrixAvoidBulletShooterEvents();
        matrixAvoidBulletShooter.StartShooting();
    }

    private void StopMatrixAvoidBullets()
    {
        if (matrixAvoidBulletShooter == null)
        {
            return;
        }

        matrixAvoidBulletShooter.StopShooting();
        Debug.Log("SniperEventManager: MatrixAvoid bullets cleared.", this);
    }

    private void SwitchToMatrixAvoidCamera()
    {
        if (matrixAvoidCamera == null)
        {
            Debug.LogWarning("SniperEventManager: Matrix Avoid Camera が設定されていません。カメラ切り替えなしで次の処理へ進みます。", this);
            return;
        }

        SaveMatrixAvoidCameraPriorityIfNeeded();

        if (sideViewCameraController != null)
        {
            sideViewCameraController.ExitSideView();
        }

        matrixAvoidCamera.Priority.Value = matrixAvoidCameraPriority;
        isMatrixAvoidCameraActive = true;
        Debug.Log("SniperEventManager: Matrix Avoid Camera に切り替えました。", this);
    }

    private void RestoreMatrixAvoidCameraPriority()
    {
        if (matrixAvoidCamera == null || !isMatrixAvoidCameraActive)
        {
            return;
        }

        if (hasSavedMatrixAvoidCameraPriority)
        {
            matrixAvoidCamera.Priority.Value = originalMatrixAvoidCameraPriority;
        }
        else
        {
            matrixAvoidCamera.Priority.Value = matrixAvoidInactiveCameraPriority;
        }

        isMatrixAvoidCameraActive = false;
        Debug.Log($"SniperEventManager: MatrixAvoidCamera priority restored to {matrixAvoidCamera.Priority.Value}.", this);
        Debug.Log("SniperEventManager: Normal camera restored.", this);
    }

    private void SaveMatrixAvoidCameraPriorityIfNeeded()
    {
        if (hasSavedMatrixAvoidCameraPriority || matrixAvoidCamera == null)
        {
            return;
        }

        originalMatrixAvoidCameraPriority = matrixAvoidCamera.Priority.Value;
        matrixAvoidCamera.Priority.Value = matrixAvoidInactiveCameraPriority;
        hasSavedMatrixAvoidCameraPriority = true;
    }
}