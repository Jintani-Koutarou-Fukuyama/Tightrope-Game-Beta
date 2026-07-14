using UnityEngine;

public class Damage : MonoBehaviour
{
    [SerializeField] AudioSource keikokuAudio;//警告音
    [SerializeField] Animator animator;//プレイヤーのアニメーション
    [SerializeField] PlayerGameFeedbackController damage;//ダメージ用
    [SerializeField] GameObject NICEText;
    [SerializeField] GameObject NOText;
    [SerializeField] GameObject Explosion;//爆発用
    [SerializeField] Transform ExplosionPoint;//爆発ポイント
    [SerializeField] Transform ExplosionPoint2;//爆発ポイント2(プレイヤー)
    [SerializeField] ExplosionFlash flash;//爆発の光
    [SerializeField] PosingEvent pose;//poseイベントを取得
    [SerializeField] CameraShake shake;//カメラを揺らす
    public bool DamageFlag = false;//ダメージを受けたか確認するフラグ
    public bool TextFlag = true;//Textを打ったか確認するフラグ
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(DamageFlag==true)
        {
            if(TextFlag==true)
            {
                //当たった時のセリフ
                Debug.Log("NOOO!");
                NOText.SetActive(true);
                TextFlag = false;
                Invoke(nameof(NoTextDestroy), 3.0f);
                Debug.Log("NICE!");
                //爆発させる
                Transform point = ExplosionPoint2.transform;
                Instantiate(Explosion, point);
                flash.Flash();
                //吹き飛びそうな動作をする
                animator.SetBool("Reaction", true);
                Invoke(nameof(ReactionReset), 3.0f);
                shake.Shake();//カメラを揺らす
                keikokuAudio.Stop();//警告音を止める
            }
           
        }
    }
    public void EventDamage()
    {
        damage.AddDamage();
    }
    public void NoDamage()
    {
        if(DamageFlag==false)
        {
            NICEText.SetActive(true);
            Invoke(nameof(NiceTextDestroy), 3.0f);
            //当たらなかった時のSEを入れる
            Debug.Log("NICE!");
           
           
            flash.Flash();
            //引き飛びそうな動作をする
            animator.SetBool("Reaction", true);
            Invoke(nameof(ReactionReset), 3.0f);
            shake.Shake();//カメラを揺らす
            keikokuAudio.Stop();//警告音を止める
        }
       
    }
    void NiceTextDestroy()
    {
        NICEText.SetActive(false);
        pose.Porsemp4.SetActive(false);//動画を非表示にする
        pose.PosingFinish();
    }
    void NoTextDestroy()
    {
        NOText.SetActive(false);
        pose.Porsemp4.SetActive(false);//動画を非表示にする
        pose.PosingFinish();
    }

    void ReactionReset()
    {
        animator.SetBool("Reaction", false);
    }
    public void Explo()
    {
        if (DamageFlag == false)
        {
            //爆発させる
            Transform point = ExplosionPoint.transform;
            Instantiate(Explosion, point);
        }
    }
}
