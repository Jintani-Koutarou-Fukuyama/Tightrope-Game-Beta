using UnityEngine;
using TMPro;

public class ClearResultManager : MonoBehaviour
{
    public enum ClearRank
    {
        S,
        A,
        B
    }

    [Header("Test Result")]
    [SerializeField] private ClearRank testRank = ClearRank.S;

    [Header("Camera")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform sCameraPoint;
    [SerializeField] private Transform aCameraPoint;
    [SerializeField] private Transform bCameraPoint;

    [Header("Result Text")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Dialogue Panel")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [SerializeField] private RectTransform sDialoguePanelPosition;
    [SerializeField] private RectTransform aDialoguePanelPosition;
    [SerializeField] private RectTransform bDialoguePanelPosition;

    private void Start()
    {
        if (ClearResultData.HasResult)
        {
            Debug.Log($"{nameof(ClearResultManager)}: Applying clear result. rank={ClearResultData.LastClearRank}, missCount={ClearResultData.LastMissCount}", this);
            ApplyResult(ClearResultData.LastClearRank);
            return;
        }

        ApplyResult(testRank);
    }

    public void ApplyResult(ClearRank rank)
    {
        ApplyCameraPoint(rank);
        ApplyResultText(rank);
        ApplyDialoguePanelPosition(rank);

        OnResultApplied(rank);
    }

    private void ApplyCameraPoint(ClearRank rank)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning($"{nameof(ClearResultManager)}: Main Camera is not assigned.", this);
            return;
        }

        Transform targetPoint = GetCameraPoint(rank);

        if (targetPoint == null)
        {
            Debug.LogWarning($"{nameof(ClearResultManager)}: Camera point for rank {rank} is not assigned.", this);
            return;
        }

        Transform cameraTransform = mainCamera.transform;
        cameraTransform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
    }

    private void ApplyResultText(ClearRank rank)
    {
        ClearResultText resultText = GetResultText(rank);

        SetText(rankText, resultText.Rank, nameof(rankText));
        SetText(endingTitleText, resultText.EndingTitle, nameof(endingTitleText));
        SetText(dialogueText, resultText.Dialogue, nameof(dialogueText));
    }

    private void ApplyDialoguePanelPosition(ClearRank rank)
    {
        if (dialoguePanelRect == null)
        {
            Debug.LogWarning($"{nameof(ClearResultManager)}: Dialogue Panel Rect is not assigned.", this);
            return;
        }

        RectTransform targetRect = GetDialoguePanelPosition(rank);

        if (targetRect == null)
        {
            Debug.LogWarning($"{nameof(ClearResultManager)}: Dialogue panel position for rank {rank} is not assigned.", this);
            return;
        }

        dialoguePanelRect.anchorMin = targetRect.anchorMin;
        dialoguePanelRect.anchorMax = targetRect.anchorMax;
        dialoguePanelRect.pivot = targetRect.pivot;
        dialoguePanelRect.anchoredPosition = targetRect.anchoredPosition;
        dialoguePanelRect.sizeDelta = targetRect.sizeDelta;
    }

    private void SetText(TMP_Text targetText, string value, string fieldName)
    {
        if (targetText == null)
        {
            Debug.LogWarning($"{nameof(ClearResultManager)}: {fieldName} is not assigned.", this);
            return;
        }

        targetText.text = value;
    }

    private Transform GetCameraPoint(ClearRank rank)
    {
        switch (rank)
        {
            case ClearRank.S:
                return sCameraPoint;
            case ClearRank.A:
                return aCameraPoint;
            case ClearRank.B:
                return bCameraPoint;
            default:
                Debug.LogWarning($"{nameof(ClearResultManager)}: Unsupported clear rank {rank}.", this);
                return null;
        }
    }

    private RectTransform GetDialoguePanelPosition(ClearRank rank)
    {
        switch (rank)
        {
            case ClearRank.S:
                return sDialoguePanelPosition;
            case ClearRank.A:
                return aDialoguePanelPosition;
            case ClearRank.B:
                return bDialoguePanelPosition;
            default:
                Debug.LogWarning($"{nameof(ClearResultManager)}: Unsupported clear rank {rank}.", this);
                return null;
        }
    }

    private ClearResultText GetResultText(ClearRank rank)
    {
        switch (rank)
        {
            case ClearRank.S:
                return new ClearResultText(
                    "S",
        "謎の組織加入END",
        "謎のボス「待っていたよ、A君。試験突破おめでとう。」");
            case ClearRank.A:
                return new ClearResultText(
                    "A",
        "出世END",
        "君みたいな人材はめったにいないよ。次はもっと大きな仕事をしてみないか？");

            case ClearRank.B:
                return new ClearResultText(
                    "B",
        "通常END",
        "……くそっ、仕留めそこなったか。");
            default:
                Debug.LogWarning($"{nameof(ClearResultManager)}: Unsupported clear rank {rank}.", this);
                return ClearResultText.Empty;
        }
    }

    protected virtual void OnResultApplied(ClearRank rank)
    {
    }

    private readonly struct ClearResultText
    {
        public static readonly ClearResultText Empty = new ClearResultText(string.Empty, string.Empty, string.Empty);

        public ClearResultText(string rank, string endingTitle, string dialogue)
        {
            Rank = rank;
            EndingTitle = endingTitle;
            Dialogue = dialogue;
        }

        public string Rank { get; }
        public string EndingTitle { get; }
        public string Dialogue { get; }
    }
}
