using UnityEngine;

// 通常視点で見せる「警告用レーザー」だけを管理するスクリプトです。
// 横視点用レーザーや弾の判定とは分けて、見た目の表示/非表示だけを担当します。
public class SniperWarningLaserController : MonoBehaviour
{
    [Header("Target")]
    // レーザーが狙うプレイヤーのTransformです。
    // 未設定ならPlayerタグのオブジェクトを自動で探します。
    [SerializeField] private Transform playerTarget;
    // 頭上や足元ではなく、体の中心付近を狙うための高さです。
    // SuitManの腰から胸あたりに合うようにInspectorで調整できます。
    [SerializeField] private float targetHeightOffset = 1.0f;

    [Header("Warning Laser Origins")]
    // 1本目のレーザー発射位置です。建物の屋上や窓付近に置いた空オブジェクトを入れます。
    [SerializeField] private Transform firstWarningLaserOrigin;
    // 2本目のレーザー発射位置です。1本目とは別の建物に置くと、別方向から狙われて見えます。
    [SerializeField] private Transform secondWarningLaserOrigin;

    [Header("Laser Look")]
    // レーザーの色です。警告として見えやすい赤を初期値にしています。
    [SerializeField] private Color laserColor = Color.red;
    // レーザーの太さです。大きくすると見やすくなります。
    [SerializeField] private float laserWidth = 0.04f;
    // 必要ならInspectorで専用マテリアルを入れます。未設定なら簡易マテリアルを自動作成します。
    [SerializeField] private Material laserMaterial;

    private LineRenderer firstWarningLaser;
    private LineRenderer secondWarningLaser;
    private bool isWarningLaserVisible = false;

    private void Awake()
    {
        if (playerTarget == null)
        {
            AutoFindPlayerTarget();
        }

        CreateWarningLasersIfNeeded();
        HideWarningLasers();
    }

    private void Update()
    {
        // 表示中だけ毎フレーム位置を更新します。
        // プレイヤーが動いてもレーザーが体の中心付近を追い続けるためです。
        if (isWarningLaserVisible)
        {
            UpdateWarningLaserPositions();
        }
    }

    // 警告レーザー2本を同時に表示するメソッドです。
    // SniperEventManagerのStartSniperEventから呼び出します。
    public void ShowWarningLasers()
    {
        if (!CanUpdateWarningLasers())
        {
            Debug.LogWarning("SniperWarningLaserController: レーザー発射位置またはプレイヤーが設定されていません。", this);
            return;
        }

        CreateWarningLasersIfNeeded();
        isWarningLaserVisible = true;
        SetWarningLaserEnabled(true);
        UpdateWarningLaserPositions();
    }

    // 警告レーザー2本を同時に非表示にするメソッドです。
    // SniperEventManagerのEndSniperEventから呼び出します。
    public void HideWarningLasers()
    {
        isWarningLaserVisible = false;
        SetWarningLaserEnabled(false);
    }

    // LineRendererがまだ無ければ、このスクリプトの子オブジェクトとして自動作成します。
    // Sceneに手作業でLineRendererを用意しなくてもテストできるようにしています。
    private void CreateWarningLasersIfNeeded()
    {
        firstWarningLaser = CreateWarningLaserIfNeeded(firstWarningLaser, "WarningLaser_1");
        secondWarningLaser = CreateWarningLaserIfNeeded(secondWarningLaser, "WarningLaser_2");
    }

    private LineRenderer CreateWarningLaserIfNeeded(LineRenderer currentLaser, string objectName)
    {
        if (currentLaser != null)
        {
            return currentLaser;
        }

        GameObject laserObject = new GameObject(objectName);
        laserObject.transform.SetParent(transform);

        LineRenderer lineRenderer = laserObject.AddComponent<LineRenderer>();
        SetupLineRenderer(lineRenderer);
        lineRenderer.enabled = false;
        return lineRenderer;
    }

    // LineRendererの見た目を設定します。
    // positionCountを2にすると「発射元」と「狙う位置」を結ぶ1本の線になります。
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

    // 2本のレーザーを、それぞれの発射位置からプレイヤー中心付近へ向けます。
    private void UpdateWarningLaserPositions()
    {
        Vector3 aimPosition = GetPlayerAimPosition();
        SetWarningLaserPosition(firstWarningLaser, firstWarningLaserOrigin, aimPosition);
        SetWarningLaserPosition(secondWarningLaser, secondWarningLaserOrigin, aimPosition);
    }

    private void SetWarningLaserPosition(LineRenderer lineRenderer, Transform origin, Vector3 aimPosition)
    {
        if (lineRenderer == null || origin == null)
        {
            return;
        }

        lineRenderer.SetPosition(0, origin.position);
        lineRenderer.SetPosition(1, aimPosition);
    }

    // プレイヤーのTransform位置に高さを足して、体の中心付近を狙う座標を作ります。
    private Vector3 GetPlayerAimPosition()
    {
        return playerTarget.position + Vector3.up * targetHeightOffset;
    }

    private void SetWarningLaserEnabled(bool enabled)
    {
        if (firstWarningLaser != null)
        {
            firstWarningLaser.enabled = enabled;
        }

        if (secondWarningLaser != null)
        {
            secondWarningLaser.enabled = enabled;
        }
    }

    // 表示に必要な参照がそろっているか確認します。
    // 足りない場合はレーザーを出さず、Unity Consoleに警告を出します。
    private bool CanUpdateWarningLasers()
    {
        return playerTarget != null && firstWarningLaserOrigin != null && secondWarningLaserOrigin != null;
    }

    // Playerタグのオブジェクトを探して、レーザーの狙い先にします。
    private void AutoFindPlayerTarget()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTarget = playerObject.transform;
        }
    }
}
