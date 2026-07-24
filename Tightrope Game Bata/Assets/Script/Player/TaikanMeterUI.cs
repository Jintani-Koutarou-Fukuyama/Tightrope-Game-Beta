using UnityEngine;
using UnityEngine.UI;

/*
 * PlayerGameFeedbackControllerのダメージ回数に合わせて、
 * 0～5ミス用の完成画像を切り替えて表示するUIスクリプトです。
 *
 * 新しいSprite切り替え設定が不足している場合は、
 * 互換用として従来の5個のImageをON/OFFする方式を使用します。
 */
public class TaikanMeterUI : MonoBehaviour
{
    private const int MeterStageCount = 5;

    [Header("ダメージ回数の参照")]
    [Tooltip("GameManagerに付いているPlayerGameFeedbackControllerを設定します。")]
    [SerializeField] private PlayerGameFeedbackController damageSource;

    [Header("完成画像の切り替え（優先）")]
    [Tooltip("体幹メーターを表示するUI Imageを1個設定します。")]
    [SerializeField] private Image meterImage;

    [Tooltip("0ミス～5ミスの完成画像を、順番に6枚設定します。")]
    [SerializeField] private Sprite[] stageSprites = new Sprite[MeterStageCount + 1];

    [Header("旧5アイコン方式（フォールバック）")]
    [Tooltip("TaikanIcon_1～TaikanIcon_5を、1段階目から順番に5個設定します。")]
    [SerializeField] private Image[] stageIcons = new Image[MeterStageCount];

    [Header("4ミス時の警告")]
    [Tooltip("4ミス以上で表示する警告マーク用のUI Imageを設定します。")]
    [SerializeField] private Image warningMarkImage;

    [Tooltip("警告音を再生するAudioSourceを設定します。")]
    [SerializeField] private AudioSource warningAudioSource;

    [Tooltip("4ミスになった瞬間に1回だけ再生する警告音を設定します。")]
    [SerializeField] private AudioClip warningSound;

    [Tooltip("警告マークと警告音を有効にするミス回数です。通常は4のまま使用します。")]
    [Range(0, MeterStageCount)]
    [SerializeField] private int showWarningAtDamageCount = 4;

    [Header("確認用")]
    [Tooltip("参照不足がある場合にConsoleへ警告を表示します。")]
    [SerializeField] private bool showSetupWarnings = true;

    // 同じ4ミス通知が複数回来ても、警告音を重ねて鳴らさないために使用します。
    private bool hasPlayedWarningSound;

    private void Awake()
    {
        ValidateSettings();
    }

    private void OnEnable()
    {
        if (damageSource == null)
        {
            // 参照不足でもNullReferenceExceptionにせず、表示だけを0段階へ戻します。
            UpdateMeter(0);
            return;
        }

        damageSource.DamageCountChanged += UpdateMeter;
        UpdateMeter(damageSource.DamageCount);
    }

    private void OnDisable()
    {
        if (damageSource != null)
        {
            damageSource.DamageCountChanged -= UpdateMeter;
        }
    }

    /// <summary>
    /// PlayerGameFeedbackControllerからダメージ数が変わった時に呼ばれ、
    /// 0～5へ丸めたダメージ数に対応する完成画像を表示します。
    /// 完成画像の設定が不足している場合だけ、旧5アイコン方式を使用します。
    /// </summary>
    public void UpdateMeter(int damageCount)
    {
        int clampedDamageCount = Mathf.Clamp(damageCount, 0, MeterStageCount);
        UpdateWarning(clampedDamageCount);

        if (HasCompleteSpriteSetup())
        {
            // 既存アイコンの1個をMeter Imageとして再利用した場合も、そのImageは残します。
            UpdateLegacyIcons(0, meterImage);
            meterImage.gameObject.SetActive(true);
            meterImage.enabled = true;
            meterImage.sprite = stageSprites[clampedDamageCount];
            return;
        }

        UpdateLegacyIcons(clampedDamageCount);
    }

    private void UpdateWarning(int damageCount)
    {
        bool shouldShowWarning = damageCount >= showWarningAtDamageCount;

        if (warningMarkImage != null)
        {
            warningMarkImage.gameObject.SetActive(shouldShowWarning);
        }

        if (!shouldShowWarning)
        {
            // Retryなどで0～3ミスへ戻った後は、次の4ミス時に再び1回だけ鳴らせます。
            hasPlayedWarningSound = false;
            return;
        }

        if (damageCount != showWarningAtDamageCount || hasPlayedWarningSound)
        {
            return;
        }

        if (warningAudioSource == null || warningSound == null)
        {
            return;
        }

        warningAudioSource.PlayOneShot(warningSound);
        hasPlayedWarningSound = true;
    }

    private void UpdateLegacyIcons(int visibleIconCount, Image imageToKeepActive = null)
    {
        if (stageIcons == null)
        {
            return;
        }

        for (int i = 0; i < stageIcons.Length; i++)
        {
            Image icon = stageIcons[i];
            if (icon == null)
            {
                continue;
            }

            if (icon == imageToKeepActive)
            {
                continue;
            }

            // 0回なら全てOFF、1回なら1個目だけON、という順番で表示します。
            icon.gameObject.SetActive(i < visibleIconCount && i < MeterStageCount);
        }
    }

    private bool HasCompleteSpriteSetup()
    {
        if (meterImage == null || stageSprites == null || stageSprites.Length != MeterStageCount + 1)
        {
            return false;
        }

        for (int i = 0; i < stageSprites.Length; i++)
        {
            if (stageSprites[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool HasCompleteLegacySetup()
    {
        if (stageIcons == null || stageIcons.Length != MeterStageCount)
        {
            return false;
        }

        for (int i = 0; i < stageIcons.Length; i++)
        {
            if (stageIcons[i] == null)
            {
                return false;
            }
        }

        return true;
    }

    private void ValidateSettings()
    {
        if (!showSetupWarnings)
        {
            return;
        }

        if (damageSource == null)
        {
            Debug.LogWarning("TaikanMeterUI: Damage Sourceが未設定です。", this);
        }

        if (warningMarkImage == null)
        {
            Debug.LogWarning("TaikanMeterUI: Warning Mark Imageが未設定です。", this);
        }

        if (warningAudioSource == null)
        {
            Debug.LogWarning("TaikanMeterUI: Warning Audio Sourceが未設定です。", this);
        }

        if (warningSound == null)
        {
            Debug.LogWarning("TaikanMeterUI: Warning Soundが未設定です。", this);
        }

        if (!HasCompleteSpriteSetup())
        {
            Debug.LogWarning(
                "TaikanMeterUI: Meter ImageまたはStage Sprites（0～5ミス用の6枚）が不足しているため、旧5アイコン方式を使用します。",
                this);

            if (!HasCompleteLegacySetup())
            {
                Debug.LogWarning(
                    "TaikanMeterUI: フォールバック用のStage Iconsを5個すべて設定してください。未設定の項目は表示されません。",
                    this);
            }
        }
    }
}
