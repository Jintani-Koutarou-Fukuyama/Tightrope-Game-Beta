using UnityEngine;

// 横視点フェーズ専用のレーザーを管理するスクリプトです。
// 通常視点の警告レーザーとは別物として扱い、後で弾発射に使える位置情報も公開します。
public class SniperSideViewLaserController : MonoBehaviour
{
    [System.Serializable]
    public class SideViewLaserLine
    {
        [Header("Optional Transforms")]
        // 弾発射位置を明示したい場合に使います。未設定ならプレイヤー右側から自動計算します。
        public Transform fireOrigin;
        // 狙う位置を明示したい場合に使います。未設定ならプレイヤー中心付近を自動計算します。
        public Transform targetPoint;

        [Header("Auto Position")]
        // 2本のレーザーを少しずらすための高さです。
        public float verticalOffset;
        // プレイヤー右側のどれくらい離れた位置から出すかです。
        public float rightDistance = 5.0f;
        // プレイヤー左側へどれくらい伸ばすかです。
        public float leftDistance = 1.2f;

        [HideInInspector] public LineRenderer lineRenderer;
    }

    [Header("Target")]
    // 横視点レーザーが狙うプレイヤーです。未設定ならPlayerタグから自動取得します。
    [SerializeField] private Transform playerTarget;
    // 頭上や足元ではなく、胸〜腹あたりを狙うための高さです。
    [SerializeField] private float targetHeightOffset = 1.0f;

    [Header("Laser Direction")]
    // 画面上の右から左へ伸ばすためのワールド方向です。
    // 横視点カメラの向きに合わせてInspectorで調整できます。
    [SerializeField] private Vector3 laserRightDirection = Vector3.right;

    [Header("Laser Stop")]
    // プレイヤーやターゲットを貫通して見えないように、レーザーを少し手前で止める距離です。
    [SerializeField] private float laserStopBeforeTargetDistance = 0.4f;

    [Header("Laser Lines")]
    // 防御フェーズ用の横視点レーザーです。最初は2本使います。
    [SerializeField] private SideViewLaserLine[] sideViewLasers =
    {
        new SideViewLaserLine { verticalOffset = 0.18f },
        new SideViewLaserLine { verticalOffset = -0.18f }
    };

    [Header("Laser Look")]
    [SerializeField] private Color laserColor = Color.red;
    [SerializeField] private float laserWidth = 0.05f;
    [SerializeField] private Material laserMaterial;

    private bool isLaserVisible;

    private void Awake()
    {
        AutoFindPlayerTarget();
        CreateLasersIfNeeded();
        HideSideViewLasers();
    }

    private void Update()
    {
        if (isLaserVisible)
        {
            UpdateLaserPositions();
        }
    }

    // 横視点フェーズに入った時に、2本の横視点レーザーを表示します。
    public void ShowSideViewLasers()
    {
        if (playerTarget == null)
        {
            AutoFindPlayerTarget();
        }

        if (playerTarget == null)
        {
            Debug.LogWarning("SniperSideViewLaserController: Player Target が設定されていません。", this);
            return;
        }

        CreateLasersIfNeeded();
        isLaserVisible = true;
        SetLaserEnabled(true);
        UpdateLaserPositions();
    }

    // イベント終了時に、横視点レーザーを非表示にします。
    public void HideSideViewLasers()
    {
        isLaserVisible = false;
        SetLaserEnabled(false);
    }

    // 今後の弾発射で使える、指定レーザーの発射位置です。
    public Vector3 GetLaserFirePosition(int index)
    {
        SideViewLaserLine laser = GetLaser(index);
        return laser != null ? GetFirePosition(laser) : Vector3.zero;
    }

    // 今後の弾発射で使える、指定レーザーの狙い位置です。
    public Vector3 GetLaserTargetPosition(int index)
    {
        SideViewLaserLine laser = GetLaser(index);
        return laser != null ? GetTargetPosition(laser) : Vector3.zero;
    }

    // 弾をレーザー表示に合わせて飛ばすため、画面上で見えているレーザー終点を返します。
    public Vector3 GetLaserEndPosition(int index)
    {
        SideViewLaserLine laser = GetLaser(index);
        if (laser == null)
        {
            return Vector3.zero;
        }

        Vector3 firePosition = GetFirePosition(laser);
        Vector3 targetPosition = GetTargetPosition(laser);
        return GetStoppedBeforeTargetPosition(firePosition, targetPosition);
    }

    public int GetLaserCount()
    {
        return sideViewLasers != null ? sideViewLasers.Length : 0;
    }

    private void CreateLasersIfNeeded()
    {
        if (sideViewLasers == null)
        {
            return;
        }

        for (int i = 0; i < sideViewLasers.Length; i++)
        {
            SideViewLaserLine laser = sideViewLasers[i];
            if (laser == null || laser.lineRenderer != null)
            {
                continue;
            }

            GameObject laserObject = new GameObject($"SideViewLaser_{i + 1}");
            laserObject.transform.SetParent(transform);
            laser.lineRenderer = laserObject.AddComponent<LineRenderer>();
            SetupLineRenderer(laser.lineRenderer);
            laser.lineRenderer.enabled = false;
        }
    }

    private void SetupLineRenderer(LineRenderer lineRenderer)
    {
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = laserWidth;
        lineRenderer.endWidth = laserWidth;
        lineRenderer.startColor = laserColor;
        lineRenderer.endColor = laserColor;

        if (laserMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                laserMaterial = new Material(shader);
            }
        }

        if (laserMaterial != null)
        {
            lineRenderer.material = laserMaterial;
        }
    }

    private void UpdateLaserPositions()
    {
        if (sideViewLasers == null)
        {
            return;
        }

        for (int i = 0; i < sideViewLasers.Length; i++)
        {
            SideViewLaserLine laser = sideViewLasers[i];
            if (laser == null || laser.lineRenderer == null)
            {
                continue;
            }

            Vector3 firePosition = GetFirePosition(laser);
            Vector3 targetPosition = GetTargetPosition(laser);

            laser.lineRenderer.SetPosition(0, firePosition);
            laser.lineRenderer.SetPosition(1, GetStoppedBeforeTargetPosition(firePosition, targetPosition));
        }
    }

    private Vector3 GetFirePosition(SideViewLaserLine laser)
    {
        if (laser.fireOrigin != null)
        {
            return laser.fireOrigin.position;
        }

        Vector3 rightDirection = GetRightDirection();
        Vector3 aimPosition = GetAutoTargetPosition(laser);
        return aimPosition + rightDirection * laser.rightDistance;
    }

    private Vector3 GetTargetPosition(SideViewLaserLine laser)
    {
        if (laser.targetPoint != null)
        {
            return laser.targetPoint.position;
        }

        Vector3 aimPosition = GetAutoTargetPosition(laser);
        return aimPosition;
    }

    private Vector3 GetStoppedBeforeTargetPosition(Vector3 firePosition, Vector3 targetPosition)
    {
        Vector3 fireToTarget = targetPosition - firePosition;
        float distanceToTarget = fireToTarget.magnitude;
        float stopDistance = Mathf.Max(0f, laserStopBeforeTargetDistance);

        if (distanceToTarget <= 0.0001f || stopDistance <= 0f)
        {
            return targetPosition;
        }

        float laserLength = Mathf.Max(0f, distanceToTarget - stopDistance);
        return firePosition + fireToTarget.normalized * laserLength;
    }

    private Vector3 GetAutoTargetPosition(SideViewLaserLine laser)
    {
        return playerTarget.position + Vector3.up * (targetHeightOffset + laser.verticalOffset);
    }

    private Vector3 GetRightDirection()
    {
        return laserRightDirection.sqrMagnitude > 0.0001f ? laserRightDirection.normalized : Vector3.right;
    }

    private SideViewLaserLine GetLaser(int index)
    {
        if (sideViewLasers == null || index < 0 || index >= sideViewLasers.Length)
        {
            return null;
        }

        return sideViewLasers[index];
    }

    private void SetLaserEnabled(bool enabled)
    {
        if (sideViewLasers == null)
        {
            return;
        }

        for (int i = 0; i < sideViewLasers.Length; i++)
        {
            SideViewLaserLine laser = sideViewLasers[i];
            if (laser?.lineRenderer != null)
            {
                laser.lineRenderer.enabled = enabled;
            }
        }
    }

    private void AutoFindPlayerTarget()
    {
        if (playerTarget != null)
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }
}
