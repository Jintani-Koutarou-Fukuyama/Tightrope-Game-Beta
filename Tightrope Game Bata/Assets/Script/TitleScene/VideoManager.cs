using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class VideoManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer player;
    [SerializeField, Header("再生される間隔")] private float time = 30.0f;
    [SerializeField, Header("ビデオが再生されるCanvas")] private Canvas canvas;
    [SerializeField] private TitleManager titleManager;

    bool enable = false; // ビデオが表示されているかどうか?
    float cnt;           // カウント用
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        canvas.gameObject.SetActive(enable);
        cnt = time;
    }

    // Update is called once per frame
    void Update()
    {
        if(enable == false)
        {
            cnt--;
            Debug.Log(cnt);
            if (cnt < 0)
            {
                PlayVideo();
            }
        }
        

        if(Keyboard.current.anyKey.wasPressedThisFrame && enable == true)
        {
            StopVideo();
            cnt = time;
        }
    }

    private void PlayVideo()
    {
        enable = true;
        canvas.gameObject.SetActive(true);
        titleManager.gameObject.SetActive(false);
        player.Play();
    }

    private void StopVideo()
    {
        enable = false;
        canvas.gameObject.SetActive(false);
        titleManager.gameObject.SetActive(true);
        player.Stop();
    }
}
