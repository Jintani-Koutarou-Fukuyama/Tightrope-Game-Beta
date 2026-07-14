using UnityEngine;
using UnityEngine.Windows.WebCam;

public class PosingDino : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] GameObject Hahen;//破片のプレハブ用
    [SerializeField] CameraSwhich cam;
  
    private GameObject spawnedHahen;
    public AudioSource asiato;
    BreakBuilding bill;
    void Awake()
    {
        GameObject Camera = GameObject.Find("CameraManager");
        cam = Camera.GetComponent<CameraSwhich>();

        anim = GetComponentInChildren<Animator>();
            Debug.Log("Animator 自動取得：" + anim);
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(DinoStop), 4.0f);
        Invoke(nameof(DinoAttack), 4.5f);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void DinoStop()
    {
        anim.SetTrigger("Stop");
    }
    public void DinoAttack()
    {
        anim.SetTrigger("Attack");
    }
    public void Asiato()
    {
        asiato.Play();
    }

    public void SlowPunch()
    {
        anim.speed = 0.5f;
    }
    public void StopPunch()
    {
        anim.speed = 0.1f;
    }
    public void PowerPunch()
    {
        //Effect入れる
    }
    public void FinishPunch()
    {
        anim.speed=1.0f;
    }
    public void BreakBill()
    {
        GameObject targetObject = GameObject.Find("破片飛ばすビル");
        if (targetObject != null)
        {
           bill = targetObject.GetComponent<BreakBuilding>();
        }
        bill.Break(transform.position);//ビルを壊す
        Debug.Log("ビル破壊");
        Vector3 pos = new Vector3(-21.144f, 2.25f, -4.15f);//+が左、-が右
        spawnedHahen=Instantiate(Hahen,pos,Quaternion.identity);
        Invoke(nameof(DestroyDino), 3.0f);
        Invoke(nameof(CamSet), 1.0f);
    }
   
    void CamSet()
    {
        cam.PosingFinish();
    }
    void DestroyDino()
    {
        Destroy(gameObject);
    }

}
