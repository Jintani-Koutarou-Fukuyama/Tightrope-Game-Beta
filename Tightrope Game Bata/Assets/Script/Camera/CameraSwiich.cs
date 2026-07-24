using UnityEngine;
using Unity.Cinemachine;


public class CameraSwhich : MonoBehaviour
{
    //CameraManagerにアタッチしてある
    //Camera1にManCamera,Camera2にDinoCameraを入れる
    [SerializeField,Header("カメラ")] public CinemachineCamera mancamera;
    [SerializeField] private CinemachineCamera dinocamera;
    [SerializeField] private CinemachineCamera eventdinocamera;
    [SerializeField] private CinemachineCamera posingdinocamera;
    [SerializeField] private CinemachineCamera posingdinocamera2;
    [SerializeField] private CinemachineCamera posingdinocamera3;
    [SerializeField] private CinemachineCamera dinounderropecamera;
    [SerializeField,Header("イベント用")] PosingEvent Posing;
    [SerializeField] Transform player;//ポーズ取る用
    [SerializeField] Transform Stick;//棒持ち上げ用
   

    void Start()
    {
        Debug.Log("怪獣注目のやつEscでスキップできるよ");
        //カメラの描画優先度を変える
        mancamera.Priority.Value = 15;
        dinocamera.Priority.Value = 5;
        Invoke("GameStartCamera",0.7f);
        Invoke("CameraSet", 5f);//5秒でカメラを切り替える
        Debug.Log("startカメラ実行");
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            
            //カメラの描画優先度を変える
            mancamera.Priority.Value = 15;
            dinocamera.Priority.Value = 5;
            Debug.Log("スキップカメラ実行");

        }
    }
    void GameStartCamera()
    {
        //カメラの描画優先度を変える
        mancamera.Priority.Value = 5;
        dinocamera.Priority.Value = 15;
        Debug.Log("GameStartカメラ実行");
    }
    public void CameraSet()
    {
        //カメラの描画優先度を変える
        BlendEaseInOut();//カメラをゆっくり切り替え
        mancamera.Priority.Value = 15;
        dinocamera.Priority.Value = 5;
        posingdinocamera.Priority.Value = 5;
        posingdinocamera2.Priority.Value = 5;
        posingdinocamera3.Priority.Value = 5;
        dinounderropecamera.Priority.Value = 2;
        Debug.Log("CameraSetカメラ実行");
    }
    public void EventCameraSet()
    {
        BlendEaseInOut();//カメラをゆっくり切り替え

        mancamera.Priority.Value = 5;
        eventdinocamera.Priority.Value = 15;
        Invoke("EventFinish", 8.0f);
        Debug.Log("EventCameraSetカメラ実行");
    }
    void EventFinish()
    {
        mancamera.Priority.Value = 15;
        eventdinocamera.Priority.Value = 0;
        Debug.Log("EventFinishカメラ実行");
    }
    public void PosingCameraSet()
    {
        BlendEaseInOut();//カメラをゆっくり切り替え

        mancamera.Priority.Value = 5;
        posingdinocamera.Priority.Value = 15;
        Debug.Log("PosingCameraSetカメラ実行");
    }
    public void PosingFinish()
    {
        Posing.HahenTextTrue();

        BleinCut();//カメラを瞬時切り替え
        //カメラの描画優先度を変える
        posingdinocamera2.Priority.Value = 15;
        posingdinocamera.Priority.Value = 0;
        Invoke(nameof(PosingCamera2), 2.0f);
        Debug.Log("PosingFinishカメラ実行");
    }
    void PosingCamera2()
    {
        Posing.HahenTextFalse();
        //カメラの描画優先度を変える
        posingdinocamera3.Priority.Value = 15;
        posingdinocamera2.Priority.Value = 0;
        Debug.Log("PosingCamera2カメラ実行");
    }

    public void DinoUnderRopeCameraSet()
    {
        BleinCut();//カメラを瞬時切り替え
        //カメラの描画優先度を変える
        mancamera.Priority.Value = 5;
        dinounderropecamera.Priority.Value = 15;
        Invoke("DinoUnderRopeFinish", 8.0f);
        Debug.Log("DinoUnderRopeCameraSetカメラ実行");
    }
    void DinoUnderRopeFinish()
    {
        mancamera.Priority.Value = 15;
        dinounderropecamera.Priority.Value = 0;
        Debug.Log("DinoUnderRopeFinishカメラ実行");
    }

    void BlendEaseInOut()//カメラをゆっくり切り替え

    {
        var brain = Camera.main.GetComponent<CinemachineBrain>();

        //DefaltBlendをEaseInOut(カメラをゆっくり切り替え)に変更
        var blend = brain.DefaultBlend;
        blend.Style = CinemachineBlendDefinition.Styles.EaseInOut;
        blend.Time = 2f;
        brain.DefaultBlend = blend;
    }
    void BleinCut()//カメラを瞬時切り替え
    {
        var brain = Camera.main.GetComponent<CinemachineBrain>();

        //DefaltBlendをcut(カメラを瞬時切り替え)に変更
        var blend = brain.DefaultBlend;
        blend.Style = CinemachineBlendDefinition.Styles.Cut;
        blend.Time = 0f;
        brain.DefaultBlend = blend;
    }
}
