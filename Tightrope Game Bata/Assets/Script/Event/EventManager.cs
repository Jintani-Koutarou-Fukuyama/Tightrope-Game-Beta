using System;
using System.Data;
using System.Runtime.CompilerServices;
using UnityEngine;

/*
  ◆説明
　プレイヤーにあたるとイベントを起こすEventFlagの生成をして、イベントの発生を行います。
　◆使い方
　① イベントの処理を新しいスクリプトに書きます。
　➁ ヒエラルキーウィンドウのEventManagerの下階層に新しいゲームオブジェクトを作ります。
　③ ②で作ったゲームオブジェクトに①で作ったスクリプトをアタッチします。
　④ 「// イベントを起こすためのゲームオブジェクト」の部分に①で作ったクラスの変数を宣言します。
　⑤ インスペクターウィンドウから②で作ったゲームオブジェクトと④を紐付けます
　⑥ 必要であれば、EventTypeに新しい状態を追加してください
　⑦ RandomEvent()のイベントの種類の判定の部分に④から処理を呼び出すコードを記述してください
 */
public class EventManager : MonoBehaviour
{
    [SerializeField,Header("任意のイベントだけを引き起こせます,Falseで無し")]
                     bool Earthquake = true;
    [SerializeField] bool Helicopter = true;
    [SerializeField] bool Posing = true;
    [SerializeField] bool Sniper = true;
    [SerializeField] private GameObject player;//プレイヤーの座標取得用
    public int ramdomCount; // ランダムな数字
    [SerializeField]EventFlag eventFlag;  // イベントフラグのprefab

    // イベントを起こすためのゲームオブジェクト
    [SerializeField,Header("地震イベントを起こすゲームオブジェクト")] private Earthquake e_earthquake;
    [SerializeField,Header("ヘリイベントを起こすゲームオブジェクト")] private Helicopter heliscript;//ヘリイベント呼び出し用
    [SerializeField,Header("ポーズイベントを起こすゲームオブジェクト")] private PosingEvent posingscript;//ポーズイベント呼び出し用
    [SerializeField, Header("スナイパーイベントを起こすゲームオブジェクト")] private SniperEventManager sniperEventManager;//スナイパーイベント呼び出し用

    bool PosingFlag = false;//ポーズイベントのフラグ（１ゲーム一回限定にするため）
    bool HelicopterFlag = false;//ポーズイベントのフラグ（１ゲーム一回限定にするため）
    //イベントの種類
    public enum EventType
    {
        Earthquake,   // 地震
        Helicopter,   //ヘリ
        Posing,       //ポーズ
        Sniper,       //スナイパー
    }


    private void Start()
    {
        EventFlag e;

        // イベントフラグの生成、設定
        /* e = Instantiate(eventFlag,new Vector3(-1.2f,3.0f,-4.0f),Quaternion.identity);//１個目のイベント
         e.SetManager(this);
         e = Instantiate(eventFlag, new Vector3(-3.5f, 3.0f, -4.0f), Quaternion.identity);//2個目のイベント
         e.SetManager(this);
         e = Instantiate(eventFlag, new Vector3(-6.5f, 3.0f, -4.0f), Quaternion.identity);//3個目のイベント
         e.SetManager(this);*/

        //任意のイベントを付けたいときはSetEvent(EventType.イベント名)でできるよ

        e = Instantiate(eventFlag, new Vector3(12.74f, 3.0f, -2.0f), Quaternion.identity);//１個目のイベント
        e.SetManager(this);
        e.SetEvent(EventType.Earthquake);//1個目は地震を起こす
        e = Instantiate(eventFlag, new Vector3(6.057f, 3.0f, 3.082f), Quaternion.identity);//2個目のイベント
        e.SetManager(this);
        e.SetEvent(EventType.Posing);//2個目はポーズを起こす
        e = Instantiate(eventFlag, new Vector3(5.55f, 3.0f, 5.33f), Quaternion.identity);//3個目のイベント
        e.SetManager(this);
        e.SetEvent(EventType.Sniper);//3個目はスナイパーを起こす
        e = Instantiate(eventFlag, new Vector3(-4.66f, 3.0f, 6.96f), Quaternion.identity);//4個目のイベント
        e.SetManager(this);
        e.SetEvent(EventType.Helicopter);//4個目はヘリを起こす
        e = Instantiate(eventFlag, new Vector3(-4.44f, 3.0f, 3.58f), Quaternion.identity);//5個目のイベント
        e.SetManager(this);
        e.SetEvent(EventType.Helicopter);//5個目はヘリを起こす
    }

    // イベントフラグがプレイヤーにあたると呼び出されるメソッド
    public void RandomEvent()
    {
        // 0からEventTypeの要素数までの乱数
        ramdomCount = UnityEngine.Random.Range((int)0, (int)Enum.GetValues(typeof(EventType)).Length);
        Debug.Log($"乱数（整数）: {ramdomCount}");

        // イベントの種類を判定する
        switch (ramdomCount)
        {
            // 地震イベントの場合
            case (int)EventType.Earthquake:
                if(Earthquake==false)
                {
                    RandomEvent();
                    break;
                }
                e_earthquake.StartEvent(); // 地震を起こす
                break;
            // ヘリイベントの場合
            case (int)EventType.Helicopter:
                if(Helicopter==false)
                {
                    RandomEvent();
                    break;
                }
                if (HelicopterFlag == true)
                {
                    RandomEvent();//ヘリイベントをやったことがあったらもう一回イベント抽選する
                    Debug.Log("再抽選");
                    break;
                }
                heliscript.EventFlag(); // ヘリイベントを起こす
                HelicopterFlag = true;
                break;
            // ポーズイベントの場合
            case (int)EventType.Posing:
                if(Posing==false)
                {
                    RandomEvent();
                    break;
                }
                if(PosingFlag==true)
                {
                    RandomEvent();//ポーズイベントをやったことがあったらもう一回イベント抽選する
                    Debug.Log("再抽選");
                    break;
                }
                posingscript.EventFlag(); // ポーズイベントを起こす
                PosingFlag = true;
                break;
            // スナイパーイベントの場合
            case (int)EventType.Sniper:
                if(Sniper==false)
                { 
                    RandomEvent();
                    break;
                }
                if (sniperEventManager == null)
                {
                    Debug.LogWarning("EventManager: SniperEventManager が設定されていません。");
                    break;
                }

                sniperEventManager.StartSniperEvent();
                break;
            default:
                Debug.Log("イベントが起こりました。");
                break;
        }

    }
    public void Event(EventType type)
    {
        //Debug.Log(type + ("マネージャー"));
        // イベントの種類を判定する
        switch (type)
        {
            // 地震イベントの場合
            case EventType.Earthquake:
                if (Earthquake == false)
                {
                    RandomEvent();
                    break;
                }
                e_earthquake.StartEvent(); // 地震を起こす
                break;
            // ヘリイベントの場合
            case EventType.Helicopter:
                if (Helicopter == false)
                {
                    RandomEvent();
                    break;
                }
                if (HelicopterFlag == true)
                {
                    RandomEvent();//ヘリイベントをやったことがあったらもう一回イベント抽選する
                    Debug.Log("再抽選");
                    break;
                }
                heliscript.EventFlag(); // ヘリイベントを起こす
                HelicopterFlag = true;
                break;
            // ポーズイベントの場合
            case EventType.Posing:
                if (Posing == false)
                {
                    RandomEvent();
                    break;
                }
                if (PosingFlag == true)
                {
                    RandomEvent();//ポーズイベントをやったことがあったらもう一回イベント抽選する
                    Debug.Log("再抽選");
                    break;
                }
                posingscript.EventFlag(); // ポーズイベントを起こす
                PosingFlag = true;
                break;
            // スナイパーイベントの場合
            case EventType.Sniper:
                if (Sniper == false)
                {
                    RandomEvent();
                    break;
                }
                if (sniperEventManager == null)
                {
                    Debug.LogWarning("EventManager: SniperEventManager が設定されていません。");
                    break;
                }

                sniperEventManager.StartSniperEvent();
                break;
            default:
                Debug.Log("イベントが起こりました。");
                break;
        }

    }

}
