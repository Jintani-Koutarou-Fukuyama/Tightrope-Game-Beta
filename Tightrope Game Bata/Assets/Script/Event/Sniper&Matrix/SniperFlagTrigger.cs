using UnityEngine;

// スナイパーイベントを開始するための当たり判定です。
// このスクリプトをTrigger Collider付きのSniperFlagオブジェクトに付けます。
public class SniperFlagTrigger : MonoBehaviour
{
    [Header("Sniper Event")]
    // 開始したいスナイパーイベント管理スクリプトです。
    // Inspectorで設定しておくと、どのSniperEventManagerを動かすか分かりやすくなります。
    [SerializeField] private SniperEventManager sniperEventManager;

    [Header("Trigger")]
    // trueにすると、一度イベントを開始した後は二度と発火しません。
    [SerializeField] private bool triggerOnlyOnce = true;

    private bool hasTriggered;

    private void Reset()
    {
        AutoFindSniperEventManager();
    }

    private void Awake()
    {
        if (sniperEventManager == null)
        {
            AutoFindSniperEventManager();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Playerタグ以外が入ってきた時は、スナイパーイベントを開始しません。
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (sniperEventManager == null)
        {
            Debug.LogWarning("SniperFlagTrigger: SniperEventManager が設定されていません。", this);
            return;
        }

        hasTriggered = true;
        sniperEventManager.StartSniperEvent();
    }

    private void AutoFindSniperEventManager()
    {
        sniperEventManager = FindFirstObjectByType<SniperEventManager>();
    }
}
