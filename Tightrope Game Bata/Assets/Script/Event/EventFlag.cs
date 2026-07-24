using UnityEngine;

public class EventFlag : MonoBehaviour
{
    // イベントマネージャー
    private EventManager eventManager;
    private EventManager.EventType eventType;
    public bool _EventFlag = false;
    // イベントマネージャーの中身を設定する
    public void SetManager(EventManager manager)
    {
        eventManager = manager;
    }
    public void SetEvent(EventManager.EventType type)
    {
        eventType = type;
        _EventFlag = true;//任意のイベントに設定されたらランダム優先を行わない
    }
    void OnTriggerEnter(Collider t)
    {
        //Playerに当たったら
        if (t.gameObject.CompareTag("Player"))
        {
            if(_EventFlag==false)
            {
                // イベントマネージャーからイベントを発生させる
                eventManager.RandomEvent();
            }
            else
            {
                eventManager.Event(eventType);
               // Debug.Log(eventType);
            }


                Destroy(this.gameObject);
        }


    }
}
