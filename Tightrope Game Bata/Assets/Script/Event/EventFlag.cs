using UnityEngine;

public class EventFlag : MonoBehaviour
{
    // イベントマネージャー
    private EventManager eventManager;
    
    // イベントマネージャーの中身を設定する
    public void SetManager(EventManager manager)
    {
        eventManager = manager;
    }

    void OnTriggerEnter(Collider t)
    {
        //Playerに当たったら
        if (t.gameObject.CompareTag("Player"))
        {
            // イベントマネージャーからイベントを発生させる
            eventManager.RandomEvent();
            Destroy(this.gameObject);
        }


    }
}
