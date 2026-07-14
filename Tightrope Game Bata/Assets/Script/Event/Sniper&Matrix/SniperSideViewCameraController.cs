using UnityEngine;
using Unity.Cinemachine;

// スナイパーイベント中の横視点カメラ切り替えだけを管理するスクリプトです。
// Main CameraのTransformは直接動かさず、既存イベントと同じようにCinemachine CameraのPriorityで切り替えます。
public class SniperSideViewCameraController : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    // 通常時に使っているCinemachine Cameraです。SampleSceneではManCameraを入れる想定です。
    [SerializeField] private CinemachineCamera normalCamera;
    // スナイパーイベント用に、シーン上で手動配置した固定の横視点Cinemachine Cameraです。
    [SerializeField] private CinemachineCamera sideViewCamera;

    [Header("Priority")]
    // 横視点へ切り替える時に、Side View Cameraへ設定する優先度です。
    [SerializeField] private int activePriority = 20;
    // 横視点ではない時に、Side View Cameraへ設定する優先度です。
    [SerializeField] private int inactivePriority = 0;

    private int originalNormalCameraPriority;
    private int originalSideViewCameraPriority;
    private bool hasSavedOriginalPriorities;
    private bool isSideViewActive;

    private void Awake()
    {
        SaveOriginalPrioritiesIfNeeded();
    }

    // 横視点カメラへ切り替えます。
    // 紙演出が終わった後にSniperEventManagerから呼ばれます。
    public void EnterSideView()
    {
        if (sideViewCamera == null)
        {
            Debug.LogWarning("SniperSideViewCameraController: Side View Camera が設定されていません。", this);
            return;
        }

        SaveOriginalPrioritiesIfNeeded();

        // 通常カメラより横視点カメラのPriorityを高くして、CinemachineBrainに横視点を選ばせます。
        if (normalCamera != null)
        {
            normalCamera.Priority.Value = Mathf.Min(originalNormalCameraPriority, activePriority - 1);
        }

        sideViewCamera.Priority.Value = activePriority;
        isSideViewActive = true;
    }

    // 横視点を終了して、イベント前のCinemachine Camera優先度へ戻します。
    public void ExitSideView()
    {
        if (!isSideViewActive)
        {
            return;
        }

        if (hasSavedOriginalPriorities)
        {
            if (normalCamera != null)
            {
                normalCamera.Priority.Value = originalNormalCameraPriority;
            }

            if (sideViewCamera != null)
            {
                sideViewCamera.Priority.Value = originalSideViewCameraPriority;
            }
        }
        else if (sideViewCamera != null)
        {
            sideViewCamera.Priority.Value = inactivePriority;
        }

        isSideViewActive = false;
    }

    private void SaveOriginalPrioritiesIfNeeded()
    {
        if (hasSavedOriginalPriorities)
        {
            return;
        }

        if (normalCamera != null)
        {
            originalNormalCameraPriority = normalCamera.Priority.Value;
        }

        if (sideViewCamera != null)
        {
            originalSideViewCameraPriority = sideViewCamera.Priority.Value;

            // 開始前に横視点カメラが選ばれないよう、待機中のPriorityへ下げておきます。
            sideViewCamera.Priority.Value = inactivePriority;
        }

        hasSavedOriginalPriorities = true;
    }
}
