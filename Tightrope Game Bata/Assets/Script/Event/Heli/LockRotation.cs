using Unity.VisualScripting;
using UnityEngine;
public class LockRotation : MonoBehaviour
{
    [SerializeField]
    [Tooltip("対象物(向く方向)")]
    private GameObject target;
    private void Start()
    {
       GameObject targetParent = GameObject.Find("SuitMan");//CharacterArmature
       Transform target1 = targetParent.transform.Find("CharacterArmature");
        target = target1.gameObject;
    }
    private void Update()
    {
        // 対象物と自分自身の座標からベクトルを算出
        Vector3 vector3 = target.transform.position - this.transform.position;
        // もし上下方向の回転はしない(Baseオブジェクトが床から離れないようにする)ようにしたければ以下のようにする。
        // vector3.y = 0f;
        // Quaternion(回転値)を取得
        Quaternion quaternion = Quaternion.LookRotation(vector3);
        // 算出した回転値をこのゲームオブジェクトのrotationに代入
        this.transform.rotation = quaternion;
    }
}