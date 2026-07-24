using Unity.VisualScripting;
using UnityEngine;

public class HahenMove : MonoBehaviour
{
    [SerializeField, Header("破片の飛ぶスピード")] float speed = 10.0f;
    [SerializeField, Header("破片の飛ぶスピード")] float slowspeed = 0.25f;
    [SerializeField] Transform player;
    [SerializeField] HahenRotation rotation;
    [SerializeField] GameObject cam;
    public bool slowmove = false;
    public bool reset = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameObject play = GameObject.Find("SuitMan");
        player = play.GetComponent<Transform>();
        transform.LookAt(player.transform);
        cam = GameObject.Find("PosingCamera3");
        
    }

    // Update is called once per frame
    void Update()
    {
        
        if(slowmove==true)
        {
            transform.Translate(Vector3.forward * speed * slowspeed * Time.deltaTime);
        }
        if(slowmove == false)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
       if(reset==true)
        {
            reset = false;
            
            Vector3 playerPos = player.position;
            float playerXPos = playerPos.x;
            float playerYPos = playerPos.y-0.7f;
            float playerZPos = playerPos.z;

            //Vector3 pos = new Vector3(-21.144f, 2.3f, -4.15f);
            Vector3 currentRotation = transform.eulerAngles;
            currentRotation.x = 0f;
            transform.eulerAngles = currentRotation;
            //カメラから見てプレイヤーの右に破片が来るようにする
            Vector3 leftDir = cam.transform.right * 0.2f;
            Vector3 pos = transform.position;
            pos.y = playerYPos;
            transform.position = pos+leftDir;
           
            Invoke(nameof(SlowFinish), 3.0f);
        }
    }

    void SlowFinish()
    {
        rotation.SlowFinish();
    }
}
