using NUnit.Framework.Constraints;
using UnityEngine;

public class HahenRotation : MonoBehaviour
{
    [SerializeField] float roteX = 1.0f;
    [SerializeField] float roteY = 1.0f;
    [SerializeField] float roteZ = 1.0f;
    [SerializeField, Header("破片の飛ぶスピード")] float speed = 1.0f;
    [SerializeField, Header("破片の飛ぶスピード")] float slowspeed = 0.25f;
    public bool slow = false;//スローモーション
        // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Invoke(nameof(SlowFlagTrue), 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
       
       

        if(slow==false)
        {
            //破片の移動
            transform.position += new Vector3(1.0f, 0.0f, 0.0f) * speed * Time.deltaTime;
            transform.Rotate(roteX, roteY, roteZ * Time.deltaTime);//破片の回転
        }
        if (slow == true)
        {
            //破片の移動
            transform.position += new Vector3(1.0f, 0.0f, 0.0f) * speed *slowspeed * Time.deltaTime;
            transform.Rotate(roteX*slowspeed, roteY*slowspeed, roteZ*slowspeed * Time.deltaTime);//破片の回転
            Debug.Log("遅くなった");
        }
        
    }
    public void SlowFlagTrue()
    {
        slow = true;
        Debug.Log("スロー");
        Invoke(nameof(HahenPositionReset), 5.0f);       
    }
   void HahenPositionReset()
    {
        Vector3 pos = new Vector3(-21.144f, 2.3f, -4.15f);
        transform.position = pos;
        Invoke(nameof(SlowFinish), 3.0f);
    }
    void SlowFinish()
    {
        slow = false;
    }
}
