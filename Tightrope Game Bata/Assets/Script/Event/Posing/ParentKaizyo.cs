using UnityEngine;

public class ParentKaizyo : MonoBehaviour
{
    public void ParentReset()
    {
        this.gameObject.transform.parent = null;
    }
}
