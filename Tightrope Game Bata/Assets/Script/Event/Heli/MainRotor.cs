using UnityEngine;

public class MainRotor : MonoBehaviour
{
   [SerializeField] public float speed = 1500f;

    void Update()
    {
        transform.Rotate(0, speed * Time.deltaTime, 0);
    }
}
