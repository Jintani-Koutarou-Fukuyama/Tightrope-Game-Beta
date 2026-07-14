using UnityEngine;

public class CameraControlCollider_Kaiju : MonoBehaviour
{
    [SerializeField] private CameraSwhich camera;


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Kaiju"))
        {

            camera.DinoUnderRopeCameraSet();
        }
    }
}
