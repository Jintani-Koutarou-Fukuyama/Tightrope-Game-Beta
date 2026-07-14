using UnityEngine;

public class TailRotor : MonoBehaviour
{
   [SerializeField] public float speed = 2500f;

    void Update()
    {
        transform.Rotate(speed * Time.deltaTime,0, 0);
    }
}
