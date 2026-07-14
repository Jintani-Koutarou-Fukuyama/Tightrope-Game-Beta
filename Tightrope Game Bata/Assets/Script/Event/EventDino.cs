using Unity.VisualScripting;
using UnityEngine;

public class EventDino : MonoBehaviour
{

    [SerializeField] GameObject raser;//レーザーのプレハブ用
    [SerializeField] Transform ebentdino;//イベント怪獣の座標取得用
    public AudioSource beamAudio;//ビーム音
    public AudioSource beamChargeAudio;//ビームチャージ音
    public AudioSource asiato;
    private Animator anim;
    private GameObject spawnraser;//レーザー呼び出す用
    float OffsetY = -5.5f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        if (beamAudio == null)
        {
            Transform origin = transform.Find("Beam");
            if (origin != null)
                beamAudio = origin.GetComponent<AudioSource>();
        }
        if (beamChargeAudio == null)
        {
            Transform origin = transform.Find("BeamCharge");
            if (origin != null)
                beamChargeAudio = origin.GetComponent<AudioSource>();
        }
    }
    void Start()
    {
        
        anim =GetComponent<Animator>();
        Invoke("DinoStop", 3.0f);
        Invoke("DinoHand", 4.0f);
        Invoke("DinoAttack", 6.0f);
        Invoke("DinoWalk", 12.0f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void DinoStop()
    {
        anim.SetTrigger("Stop");
    }
    void DinoHand()
    {
        anim.SetTrigger("Hand");
    }
    void DinoAttack()
    {
       
        anim.speed = 0.2f;
        anim.SetTrigger("Attack");
        Quaternion rotation = Quaternion.Euler(-48, -90, 0);//レーザーの向き
        //怪獣の座標を計算
        Vector3 dinopos =transform.position;
        //怪獣の口らへんにレーザーを召喚
        // 真上の座標を計算
        Vector3 spawnPos = new Vector3(
            dinopos.x+4.8f,
            dinopos.y + OffsetY,
            dinopos.z-1f);
             

        dinopos.y = OffsetY;
        spawnraser = Instantiate(raser, spawnPos, rotation);
        beamChargeAudio.Play();
        Invoke("Beam", 2.0f);
    }

    void DinoWalk()
    {
        anim.speed = 0.7f;
        anim.SetTrigger("Walk");
    }
    public void Asiato()
    {
        asiato.Play();
    }
    void Beam()
    {
        beamChargeAudio.Stop();
        beamAudio.Play();
    }
}
