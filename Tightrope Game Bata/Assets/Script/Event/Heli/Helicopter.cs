using Unity.VisualScripting;
using UnityEngine;

public class Helicopter : MonoBehaviour
{
    [SerializeField] Animator animator;//プレイヤーのアニメーション
    [SerializeField] Transform player;//プレイヤーの座標取得用
    [SerializeField] Transform Dino;//イベント中は怪獣を見えないところへ移動させる
    [SerializeField] GameObject helicopter;//ヘリのプレハブ用
    [SerializeField] GameObject EventDino;//イベント怪獣のプレハブ用
    [SerializeField] GameObject panel;//会話パネル
    [SerializeField] GameObject cameraFrame;//カメラフレーム
    [SerializeField] GameObject explosion;//Explosion読み込み
    [SerializeField] MessageSequencer Message;//会話を進める
    [SerializeField] CameraSwhich cam;//カメラを切り替えれるように
    [SerializeField] TightropeAutoGoalMover playerMover;//プレイヤーの動き取得
    [SerializeField] BalanceShake balance;
    [SerializeField] AudioSource explosionAudio;//爆発音
    [SerializeField] AudioSource cameraOnAudio;//カメラ起動音
    [SerializeField] float offsetX = 10f;
    [SerializeField] float offsetZ = 10f;
    [SerializeField] float speed =1f;//ヘリの移動速度
    [SerializeField, Header("プレイヤーの揺れの強さ")] float shake = 0.1f;
    [SerializeField, Header("プレイヤーの揺れの切り替え時間")] float clocktime =2f;
    public GameObject spawnedObj;
    private GameObject spawneDino;
    private GameObject StartHeli;
    
    bool Flag = false;//イベントフラグ
    bool HeliIdouflag = true;//ヘリ移動フラグ
    bool DinoIdouflag = true;//怪獣移動フラグ
    bool DinoFinishIdouFlag=false;
    bool KaiwaFlag = false;//会話フラグ
    bool DinoStoping = false;//怪獣を動かなくするフラグ
    bool PlayerShake = false;//プレイヤーの揺れフラグ
    float time = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if (KaiwaFlag==true)
        {
            KaiwaFlag = false;
            Invoke("KaiwaNext", 5.0f);//5秒ごとに会話を進める
           
        }
       
        if (Flag == true)
        {
            HeliEventStart();
           // HeliEvent();
            Flag = false;
        }

        if(HeliIdouflag==true)
        {
            //ヘリの移動
            //Vector3 move = new Vector3(0.0f, 0.0f, -1.0f) * speed * Time.deltaTime;
            if(spawnedObj!=null)
            {
                // spawnedObj.transform.position += move;
                spawnedObj.transform.Translate(Vector3.forward * 1.0f * Time.deltaTime);
            }
           
        }
        if (DinoIdouflag == true)
        {
            //怪獣の移動
            //Vector3 move = new Vector3(-1.0f, 0.0f, 0.0f) * speed * Time.deltaTime;
            if(spawneDino!=null)
            {
                //spawneDino.transform.position += move;
                spawneDino.transform.Translate(Vector3.forward * 1.0f * Time.deltaTime);
            }
           
        }
        if(DinoFinishIdouFlag==true)
        {
            //怪獣の移動
            Vector3 move = new Vector3(1.0f, 0.0f, 0.0f) * speed * Time.deltaTime;
            if (spawneDino != null)
            {
                spawneDino.transform.Translate(Vector3.forward * 1.0f * Time.deltaTime);
            }
        }
        if(DinoStoping==true)
        {
          Dino.transform.position = new Vector3(50.0f, 0.0f, 50.0f);
        }

        if(PlayerShake==true)
        {
            
           
            if (player !=null)
            {
                time+=Time.deltaTime;
                
                if (time<clocktime)
                {
                    player.transform.Rotate(new Vector3(0, 0, shake));
                  
                }
                if(time>clocktime)
                {
                    player.transform.Rotate(new Vector3(0, 0, -shake));
                   
                }
                if(time>clocktime*2)
                {
                    time = 0;
                }
            }
        }
       
        if(StartHeli!=null)
        {
            //最初のヘリの移動
            StartHeli.transform.Translate(Vector3.forward * 5.0f * Time.deltaTime);
        }
       

    }
    void HeliEventStart()
    {
        Quaternion rotation = Quaternion.Euler(0, 70, 0);//ヘリの向き
        Vector3 playerpos = player.transform.position + -player.right * 9f + player.forward * 1.5f+-player.up*6f;
        StartHeli = Instantiate(helicopter, playerpos, rotation);
        LockRotation Lock;
        Lock =StartHeli.GetComponent<LockRotation>();
        Lock.enabled = false;
        Invoke(nameof(HeliEvent), 5.0f);
        
    }
    void HeliEvent()
    {
        Destroy(StartHeli);//初めのヘリを削除
        DinoStoping = true;//怪獣を固定
        playerMover.PlayerStoping = true;//プレイヤーを固定
        Quaternion rotation = Quaternion.Euler(0, 0, 0);//ヘリの向き
        //プレイヤーの右にヘリを生成
        Vector3 playerpos = player.transform.position + player.right * offsetX + player.forward * offsetZ;
        spawnedObj=Instantiate(helicopter, playerpos, rotation);
       
        //3.2秒後にヘリを停止
        Invoke("Idouflag", 3.2f);
        Invoke("CameraOn", 22.0f);
        Invoke("DinoEvent", 27.0f);//27秒に怪獣イベントを始める
        Invoke("Explosion", 35.0f);//35秒後に爆発させる
        //会話の開始
        KaiwaFlag = true;
        panel.SetActive(true);
        

    }

    public void EventFlag() //イベントマネージャーで呼び出す
    {
       
        Flag = true;

    }
    void Idouflag()
    {
        HeliIdouflag = false;
        PlayerShake = true;
        balance.ShakeOn = true;
       
    }
    void DinoIdouFlag()
    {
        DinoIdouflag = false;
        cam.EventCameraSet();
    }
    void KaiwaNext()
    {
        Message.MoveNext();
        KaiwaFlag = true;
    }
    void CameraOn()
    {
        cameraFrame.SetActive(true);
        cameraOnAudio.Play();
    }
    void DinoEvent()
    {
        Quaternion rotation = Quaternion.Euler(0, -90, 0);//怪獣の向き
        //ヘリの横に怪獣を生成
        //Vector3 helipos = spawnedObj.transform.position + new Vector3(5.0f, -3.18f, 0.0f);
        Transform Obj = spawnedObj.transform;
        Vector3 leftDirection = -Obj.right;
        Vector3 YDirection =new Vector3(0, -2.8f, 0);
        Vector3 helipos = spawnedObj.transform.position + leftDirection * 5.0f+YDirection;
        spawneDino = Instantiate(EventDino,helipos,rotation);
        spawneDino.transform.LookAt(spawnedObj.transform);
        Vector3 currentRotation =spawneDino.transform.eulerAngles;
        currentRotation.x = 0;
        spawneDino.transform.eulerAngles = currentRotation;
        //３秒後に怪獣を停止
        Invoke("DinoIdouFlag", 3.0f);
        
    }
   void Explosion()
    {
        //ヘリを爆発させる
        Vector3 helipos = spawnedObj.transform.position;
        Instantiate(explosion, helipos, Quaternion.identity);
        spawnedObj.SetActive(false);
        explosionAudio.Play();
        Invoke("DinoWalk", 5.0f);
        Invoke("Destroy", 8.8f);
        PlayerShake = false;
        //バランスゲージを大きくゆらす
        balance.ExplosionOn = true;
        Invoke(nameof(ExplosionFinish), 2.0f);
        animator.SetBool("Reaction", true);
        //プレイヤーのバランスを戻す
        player.transform.rotation = Quaternion.Euler(0, -90, 0);
    }

    void DinoWalk()
    {
        spawneDino.transform.eulerAngles = new Vector3(0, 180, 0);
        DinoFinishIdouFlag = true;
        balance.ShakeOn = false;
        animator.SetBool("Reaction", false);
    }
    private void Destroy()
    {
        DinoStoping = false;
        playerMover.PlayerStoping = false;
        panel.SetActive(false);
        cameraFrame.SetActive(false);
        //Dino.transform.position = new Vector3(17.0f, 0.0f, 14.0f);
        Vector3 pos = cam.mancamera.transform.position - cam.mancamera.transform.forward*3;//カメラから3座標後ろにワープさせる
        pos.y = 0;
        Dino.transform.position = pos;
        Destroy(spawnedObj);
        Destroy(spawneDino);
    }
   void ExplosionFinish()
    {
        balance.ExplosionOn = false;
    }
   
}
