using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/*
  ◆説明
　プレイヤーへのゲーム中フィードバックをまとめて管理するスクリプトです。
　成功音、カウント音、失敗音、ダメージ加算、赤点滅、ゲームオーバーシーンへの遷移を担当します。
　イベント側のスクリプトは、演出やダメージ発生時に下の public メソッドを呼んでください。

　◆使い方
　・成功音を鳴らしたい場合
　　PlaySuccessSound() を呼びます。

　・カウント音を鳴らしたい場合
　　PlayCountSound() を呼びます。

　・失敗音を鳴らしたい場合
　　PlayMissSound() を呼びます。

　・ダメージを与えたい場合
　　AddDamage() を呼ぶと 1 ダメージ加算します。
　　AddDamage(int amount) を呼ぶと指定した量だけダメージ加算します。
　　ダメージ加算時には赤点滅も発生します。

　・赤点滅だけ出したい場合
　　FlashRed() を呼びます。

　・ダメージ数をリセットしたい場合
　　ResetDamage() を呼びます。

　◆注意点
　・AddDamage() / AddDamage(int) により damageCount が maxDamageCount 以上になると、gameOverSceneName のシーンへ遷移します。
　・失敗音だけ鳴らす、赤点滅だけ出す、ダメージだけ加算するなど、イベント内容に合わせて呼ぶメソッドを使い分けてください。
　・Inspector で audioSource、successClip、countClip、missClip を設定してください。
　・赤点滅を使う場合は playerRenderers を設定してください。
　・gameOverSceneName は Build Settings に登録されているシーン名と合わせてください。
　・現在のダメージ数は DamageCount、最大ダメージ数は MaxDamageCount から読み取りできます。
 */
public class PlayerGameFeedbackController : MonoBehaviour
{
    [Header("Damage")]
    // 何回ダメージを受けたらゲームオーバーにするかです。
    [SerializeField] private int maxDamageCount = 5;
    // ゲームオーバー時に遷移するScene名です。
    [SerializeField] private string gameOverSceneName = "GameOverScene";
    [SerializeField] private string clearSceneName = "ClearScene";

    [Header("Audio")]
    // SEを再生するAudioSourceです。
    // 未設定なら、このGameObjectに付いているAudioSourceを探します。
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip countClip;
    [SerializeField] private AudioClip missClip;

    [Header("Red Flash")]
    // ミス/ダメージ時に赤く光らせたいRendererを入れます。
    [SerializeField] private Renderer[] playerRenderers;
    // 赤く光る時間です。
    [SerializeField] private float redFlashDuration = 0.2f;
    // 発光時の赤色です。
    [SerializeField] private Color redFlashColor = Color.red;
    // Emissionを持つMaterialの場合、この強さで発光させます。
    [SerializeField] private float redEmissionIntensity = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Material[][] cachedMaterials;
    private Color[][] originalBaseColors;
    private Color[][] originalEmissionColors;
    private bool[][] originalEmissionEnabled;
    private Coroutine redFlashCoroutine;
    private int damageCount;

    public int DamageCount => damageCount;
    public int MaxDamageCount => maxDamageCount;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        CacheOriginalMaterialColors();
    }

    public void PlaySuccessSound()
    {
        PlayOneShot(successClip, "success");
    }

    public void PlayCountSound()
    {
        PlayOneShot(countClip, "count");
    }

    public void PlayMissSound()
    {
        PlayOneShot(missClip, "miss");
    }

    public void AddDamage()
    {
        AddDamage(1);
    }

    public void AddDamage(int amount)
    {
        int addAmount = Mathf.Max(0, amount);
        damageCount += addAmount;
        DebugLog($"Damage added. damageCount={damageCount}/{maxDamageCount}");

        FlashRed();

        if (damageCount >= maxDamageCount)
        {
            LoadGameOverScene();
        }
    }

    public void ResetDamage()
    {
        damageCount = 0;
        DebugLog("Damage reset.");
    }

    public void LoadClearScene()
    {
        if (string.IsNullOrEmpty(clearSceneName))
        {
            DebugLog("Clear scene name is empty. Scene load skipped.");
            return;
        }

        if (damageCount >= maxDamageCount)
        {
            DebugLog($"Clear scene load skipped because damageCount={damageCount}/{maxDamageCount}. GameOver should handle this state.");
            return;
        }

        ClearResultManager.ClearRank clearRank = CalculateClearRank();
        ClearResultData.SetResult(clearRank, damageCount);

        DebugLog($"Loading clear scene: {clearSceneName}, rank={clearRank}, missCount={damageCount}");
        SceneManager.LoadScene(clearSceneName);
    }

    public void FlashRed()
    {
        if (redFlashCoroutine != null)
        {
            StopCoroutine(redFlashCoroutine);
            RestoreOriginalMaterialColors();
        }

        redFlashCoroutine = StartCoroutine(RedFlashRoutine());
    }

    private IEnumerator RedFlashRoutine()
    {
        ApplyRedFlashColor();

        if (redFlashDuration > 0f)
        {
            yield return new WaitForSeconds(redFlashDuration);
        }

        RestoreOriginalMaterialColors();
        redFlashCoroutine = null;
    }

    private void LoadGameOverScene()
    {
        if (string.IsNullOrEmpty(gameOverSceneName))
        {
            DebugLog("GameOver scene name is empty. Scene load skipped.");
            return;
        }

        DebugLog($"Loading game over scene: {gameOverSceneName}");
        SceneManager.LoadScene(gameOverSceneName);
    }

    private ClearResultManager.ClearRank CalculateClearRank()
    {
        if (damageCount <= 1)
        {
            return ClearResultManager.ClearRank.S;
        }

        if (damageCount == 2)
        {
            return ClearResultManager.ClearRank.A;
        }

        return ClearResultManager.ClearRank.B;
    }

    private void PlayOneShot(AudioClip clip, string label)
    {
        if (audioSource == null)
        {
            DebugLog($"AudioSource is not assigned. {label} sound skipped.");
            return;
        }

        if (clip == null)
        {
            DebugLog($"{label} clip is not assigned.");
            return;
        }

        audioSource.PlayOneShot(clip);
    }

    private void CacheOriginalMaterialColors()
    {
        if (playerRenderers == null)
        {
            return;
        }

        cachedMaterials = new Material[playerRenderers.Length][];
        originalBaseColors = new Color[playerRenderers.Length][];
        originalEmissionColors = new Color[playerRenderers.Length][];
        originalEmissionEnabled = new bool[playerRenderers.Length][];

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            Renderer renderer = playerRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            cachedMaterials[i] = renderer.materials;
            originalBaseColors[i] = new Color[cachedMaterials[i].Length];
            originalEmissionColors[i] = new Color[cachedMaterials[i].Length];
            originalEmissionEnabled[i] = new bool[cachedMaterials[i].Length];

            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                originalBaseColors[i][j] = GetBaseColor(material);
                originalEmissionColors[i][j] = GetEmissionColor(material);
                originalEmissionEnabled[i][j] = material != null && material.IsKeywordEnabled("_EMISSION");
            }
        }
    }

    private void ApplyRedFlashColor()
    {
        EnsureMaterialCache();
        if (cachedMaterials == null)
        {
            return;
        }

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null)
            {
                continue;
            }

            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                if (material == null)
                {
                    continue;
                }

                SetBaseColor(material, redFlashColor);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", redFlashColor * redEmissionIntensity);
                }
            }
        }
    }

    private void RestoreOriginalMaterialColors()
    {
        EnsureMaterialCache();
        if (cachedMaterials == null)
        {
            return;
        }

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null)
            {
                continue;
            }

            for (int j = 0; j < cachedMaterials[i].Length; j++)
            {
                Material material = cachedMaterials[i][j];
                if (material == null)
                {
                    continue;
                }

                SetBaseColor(material, originalBaseColors[i][j]);

                if (material.HasProperty("_EmissionColor"))
                {
                    material.SetColor("_EmissionColor", originalEmissionColors[i][j]);

                    if (originalEmissionEnabled[i][j])
                    {
                        material.EnableKeyword("_EMISSION");
                    }
                    else
                    {
                        material.DisableKeyword("_EMISSION");
                    }
                }
            }
        }
    }

    private void EnsureMaterialCache()
    {
        if (cachedMaterials == null)
        {
            CacheOriginalMaterialColors();
        }
    }

    private Color GetBaseColor(Material material)
    {
        if (material == null)
        {
            return Color.white;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return Color.white;
    }

    private void SetBaseColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private Color GetEmissionColor(Material material)
    {
        if (material != null && material.HasProperty("_EmissionColor"))
        {
            return material.GetColor("_EmissionColor");
        }

        return Color.black;
    }

    private void DebugLog(string message)
    {
        if (!debugLog)
        {
            return;
        }

        Debug.Log($"[PlayerGameFeedbackController] {message}", this);
    }
}
