using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Earthquake : MonoBehaviour
{

    [SerializeField] private Image Telop_earthquake;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip AlarmSound;
    [SerializeField] private AudioClip earthquakeSound;
    [SerializeField] private CameraShake cameara;

    [SerializeField, Header("テロップが消えるまでの時間")] private float activetime = 6.0f;
    [SerializeField, Header("ロープに与える力")] private float addforce = 100;
    [SerializeField, Header("ロープが揺れる時間")] private int count = 5;
    [SerializeField] GameObject rope;
    private RopeParts ropeParts;
    private Rigidbody rope_rb;

    private bool enable_event = false;

    void Start()
    {
        Telop_earthquake.gameObject.SetActive(false);
        rope_rb = rope.GetComponent<Rigidbody>();
    }


    public void StartEvent()
    {
        Debug.Log("地震が起こりました。");
        if (enable_event)
        {
            return;
        }

        StartCoroutine("Telop_Earthquake");
    }

    IEnumerator Telop_Earthquake()
    {
        Telop_earthquake.gameObject.SetActive(true);
        audioSource.PlayOneShot(AlarmSound);
        enable_event = true;

        while (true)
        {

            yield return new WaitForSeconds(activetime);

            enable_event = false;
            Telop_earthquake.gameObject.SetActive(false);

            StartCoroutine("E_Earthquake");

            yield break;
        }
    }

    IEnumerator E_Earthquake()
    {

        // 地震の音を出す
        audioSource.PlayOneShot(earthquakeSound);

        // カメラを揺らす
        cameara.Shake();


        int cnt = count;
        while (true)
        {

            
            Debug.Log("与えています。");

            // cntが減っていくたび揺らす力が大きくなる
            ShakeRope(addforce / cnt);
            yield return new WaitForSeconds(1.0f);
            ShakeRope(addforce /cnt);
            yield return new WaitForSeconds(1.0f);
            ShakeRope(addforce / cnt);
            yield return new WaitForSeconds(1.0f);
            ShakeRope(addforce / cnt);


            if (--cnt <= 0)
            {
                
                Debug.Log("終わりました。");
                yield break;
            }
            
        }   

    }

    // ロープを揺らす
    private void ShakeRope(float force)
    {
        // 揺らすロープパーツを取得(大体真ん中らへんのパーツ)
        ropeParts = rope.GetComponent<Rope>().GetRandomRopePart(30, 60);
        rope_rb = ropeParts.GetComponent<Rigidbody>();

        // 取得したロープパーツを揺らす
        rope_rb.AddForce(0.0f, 0.0f, force);
    }
}
