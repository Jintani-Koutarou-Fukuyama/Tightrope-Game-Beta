using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("シェイクしたいもの")]
    [SerializeField] private CinemachineBasicMultiChannelPerlin shake;
    [Header("揺れの強さ/揺れの速さ/揺れの時間")]
    [SerializeField] private float amplitudeGain = 1;//揺れの強さ
    [SerializeField] private float frequencyGain = 1;//揺れの速さ
    [SerializeField] private float shakeDuration = 1;//揺れの時間

    CinemachineBasicMultiChannelPerlin perlin;
    private float shakeTimer;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        perlin = GetComponent<CinemachineBasicMultiChannelPerlin>();
       // Debug.Log("デバッグ用 Yでカメラ揺らす");
    }

    // Update is called once per frame
    void Update()
    {
        

        // 揺れ時間のカウントダウン
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            if (shakeTimer <= 0)
            {
                StopShake();
            }
        }

       /* if (Input.GetKeyDown(KeyCode.Y))//デバッグ用
        {
            Shake();

        }
       */
    }

    public void Shake()
    {
        

        shake.GetComponent<CinemachineBasicMultiChannelPerlin>();
        shake.AmplitudeGain = amplitudeGain;
        shake.FrequencyGain = frequencyGain;
        shakeTimer = shakeDuration;

    }
    public void StopShake()
    {
        shake.AmplitudeGain = 0f;
        shake.FrequencyGain = 0f;

    }



    
}
