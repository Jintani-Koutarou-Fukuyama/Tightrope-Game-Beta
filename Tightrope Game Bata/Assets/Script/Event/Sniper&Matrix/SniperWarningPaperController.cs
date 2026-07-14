using System.Collections;
using System;
using UnityEngine;
using UnityEngine.UI;

// Canvas上で「紙が飛んできて画面に貼り付く」演出だけを管理するスクリプトです。
// スナイパーイベント本体やレーザー処理と分けることで、後から紙演出だけ調整しやすくしています。
[ExecuteAlways]
public class SniperWarningPaperController : MonoBehaviour
{
    [Header("Paper UI")]
    // Canvas上に置いた紙ImageのRectTransformです。
    // 画面外から中央へ動かすため、UIオブジェクトをInspectorで設定してください。
    [SerializeField] private RectTransform paperRect;
    // 紙として表示するImageです。未設定ならpaperRectから自動取得します。
    [SerializeField] private Image paperImage;
    [SerializeField] private CanvasGroup paperCanvasGroup;
    // 紙の画像です。Inspectorから好きなSpriteを設定できます。
    [SerializeField] private Sprite paperSprite;

    [Header("Positions")]
    // 紙が飛んでくる開始位置です。CanvasのRectTransform座標で指定します。
    [SerializeField] private Vector2 flyInStartPosition = new Vector2(-900f, 220f);
    // 紙が画面に貼り付く位置です。中央付近にしたい場合は(0, 0)付近にします。
    [SerializeField] private Vector2 stuckPosition = new Vector2(0f, 40f);
    // 紙が飛び去る位置です。飛来方向と逆側の画面外に設定します。
    [SerializeField] private Vector2 flyOutEndPosition = new Vector2(900f, -180f);

    [Header("Timing")]
    // 画面外から貼り付き位置まで飛んでくる時間です。
    [SerializeField] private float flyInDuration = 0.45f;
    // 画面に貼り付いたまま表示する時間です。
    [SerializeField] private float stuckDuration = 1.4f;
    // 画面外へ飛び去る時間です。
    [SerializeField] private float flyOutDuration = 0.45f;

    [Header("Flutter")]
    // 貼り付いた後、紙が上下左右に少し揺れる幅です。
    [SerializeField] private Vector2 flutterPositionAmount = new Vector2(8f, 5f);
    // 貼り付いた後、紙が回転してビラビラ見える角度です。
    [SerializeField] private float flutterRotationAmount = 5f;
    // 揺れの速さです。大きくすると細かく揺れます。
    [SerializeField] private float flutterSpeed = 12f;
    // 飛んでくる時の開始角度です。少し傾けると紙らしく見えます。
    [SerializeField] private float flyInStartRotation = -28f;
    // 貼り付き時の基本角度です。
    [SerializeField] private float stuckRotation = 4f;
    // 飛び去る時の終了角度です。
    [SerializeField] private float flyOutEndRotation = 32f;

    private Coroutine paperCoroutine;
    private Action onPaperEffectFinished;

    private void Awake()
    {
        AutoAssignPaperParts();
        EnsurePaperCanvasGroup();
        ApplyPaperSprite();
        HidePaperImmediately();
    }

    private void OnValidate()
    {
        AutoAssignPaperParts();
        EnsurePaperCanvasGroup();

        if (!Application.isPlaying)
        {
            SetPaperVisible(false, false);
        }
    }

    private void OnDisable()
    {
        // オブジェクトが無効化された時にCoroutineが残らないように止めます。
        StopPaperCoroutine();
    }

    // 紙演出を最初から再生します。
    // StartSniperEventから呼ばれる想定です。
    public void PlayPaperEffect()
    {
        PlayPaperEffect(null);
    }

    // 紙演出を最初から再生し、最後まで終わったら通知します。
    // SniperEventManagerはこの通知を使って、紙の後に横視点フェーズへ進みます。
    public void PlayPaperEffect(Action onFinished)
    {
        if (paperRect == null)
        {
            Debug.LogWarning("SniperWarningPaperController: Paper Rect が設定されていません。", this);
            return;
        }

        StopPaperCoroutine();
        ApplyPaperSprite();
        onPaperEffectFinished = onFinished;
        paperCoroutine = StartCoroutine(PlayPaperEffectRoutine());
    }

    // 紙が残っていても、すぐに非表示へ戻します。
    // EndSniperEventから呼ぶことで、イベント終了時に画面へ紙が残るのを防ぎます。
    public void HidePaperImmediately()
    {
        StopPaperCoroutine();
        onPaperEffectFinished = null;

        // このGameObjectを非アクティブにするとCoroutineを開始できなくなります。
        // そのため、紙の見た目だけをCanvasGroupで非表示にします。
        SetPaperVisible(false);

        if (paperRect == null)
        {
            return;
        }

        paperRect.anchoredPosition = flyInStartPosition;
        paperRect.localRotation = Quaternion.Euler(0f, 0f, flyInStartRotation);
    }

    private IEnumerator PlayPaperEffectRoutine()
    {
        // GameObjectは常にアクティブのまま、CanvasGroupだけを表示します。
        SetPaperVisible(true);

        yield return MovePaper(
            flyInStartPosition,
            stuckPosition,
            flyInStartRotation,
            stuckRotation,
            flyInDuration);

        yield return FlutterPaper();

        yield return MovePaper(
            stuckPosition,
            flyOutEndPosition,
            stuckRotation,
            flyOutEndRotation,
            flyOutDuration);

        SetPaperVisible(false, true, "Sniper warning paper hidden after effect");
        paperCoroutine = null;
        onPaperEffectFinished?.Invoke();
        onPaperEffectFinished = null;
    }

    // 紙を開始位置から終了位置へ動かし、同時に回転も変えます。
    private IEnumerator MovePaper(Vector2 startPosition, Vector2 endPosition, float startRotation, float endRotation, float duration)
    {
        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.01f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float rate = Mathf.Clamp01(elapsed / safeDuration);
            float easedRate = EaseOutCubic(rate);

            paperRect.anchoredPosition = Vector2.Lerp(startPosition, endPosition, easedRate);
            float rotation = Mathf.Lerp(startRotation, endRotation, easedRate);
            paperRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        paperRect.anchoredPosition = endPosition;
        paperRect.localRotation = Quaternion.Euler(0f, 0f, endRotation);
    }

    // 画面に貼り付いた紙を、風でビラビラしているように揺らします。
    private IEnumerator FlutterPaper()
    {
        float elapsed = 0f;

        while (elapsed < stuckDuration)
        {
            elapsed += Time.deltaTime;
            float wave = Mathf.Sin(elapsed * flutterSpeed);
            float secondWave = Mathf.Cos(elapsed * flutterSpeed * 0.7f);

            Vector2 flutterOffset = new Vector2(
                flutterPositionAmount.x * wave,
                flutterPositionAmount.y * secondWave);

            paperRect.anchoredPosition = stuckPosition + flutterOffset;
            float rotation = stuckRotation + flutterRotationAmount * wave;
            paperRect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            yield return null;
        }

        paperRect.anchoredPosition = stuckPosition;
        paperRect.localRotation = Quaternion.Euler(0f, 0f, stuckRotation);
    }

    // 0から1へ進む値を、最後だけゆっくり止まる動きに変えます。
    private float EaseOutCubic(float value)
    {
        float reverse = 1f - value;
        return 1f - reverse * reverse * reverse;
    }

    private void StopPaperCoroutine()
    {
        if (paperCoroutine == null)
        {
            return;
        }

        StopCoroutine(paperCoroutine);
        paperCoroutine = null;
    }

    private void AutoAssignPaperParts()
    {
        if (paperRect == null)
        {
            paperRect = GetComponent<RectTransform>();
        }

        if (paperImage == null && paperRect != null)
        {
            paperImage = paperRect.GetComponent<Image>();
        }
    }

    private void EnsurePaperCanvasGroup()
    {
        if (paperCanvasGroup == null)
        {
            paperCanvasGroup = GetComponent<CanvasGroup>();
        }

        if (paperCanvasGroup != null)
        {
            return;
        }

        paperCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        Debug.Log("SniperWarningPaperController: CanvasGroup was missing, added automatically.", this);
    }

    private void ApplyPaperSprite()
    {
        if (paperImage == null || paperSprite == null)
        {
            return;
        }

        paperImage.sprite = paperSprite;
    }

    private void SetPaperVisible(bool visible)
    {
        SetPaperVisible(visible, true);
    }

    private void SetPaperVisible(bool visible, bool writeLog)
    {
        string message = visible
            ? "Sniper warning paper shown"
            : "Sniper warning paper hidden by CanvasGroup";

        SetPaperVisible(visible, writeLog, message);
    }

    private void SetPaperVisible(bool visible, bool writeLog, string logMessage)
    {
        EnsurePaperCanvasGroup();

        if (paperCanvasGroup == null)
        {
            return;
        }

        paperCanvasGroup.alpha = visible ? 1f : 0f;
        paperCanvasGroup.interactable = false;
        paperCanvasGroup.blocksRaycasts = false;

        if (paperImage != null)
        {
            paperImage.enabled = true;
        }

        if (writeLog)
        {
            Debug.Log($"SniperWarningPaperController: {logMessage}.", this);
        }
    }
}
