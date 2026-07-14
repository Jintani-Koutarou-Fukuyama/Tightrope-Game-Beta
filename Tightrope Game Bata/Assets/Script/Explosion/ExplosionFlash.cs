using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;


public class ExplosionFlash : MonoBehaviour
{
    [SerializeField] Volume volume;
    Bloom bloom;
    

    void Start()
    {
        volume.profile.TryGet(out bloom);
    }
    private void Update()
    {
        if(bloom.intensity.value>0)
        {
            bloom.intensity.value -= 1.0f;
        }
    }
    public void Flash()
    {
        StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // Œ»İ‚Ì Bloom ‚Ì–¾‚é‚³‚ğ•Û‘¶
        float originalIntensity = bloom.intensity.value;

        // ”š”­‚ÌuŠÔ‚ÌŒõ
        float flashIntensity = 300f;
        Debug.Log("Before flash: " + bloom.intensity.value);

        // ˆêu‚ÅŒõ‚ç‚¹‚é
        bloom.intensity.value = flashIntensity;

        Debug.Log("After flash: " + bloom.intensity.value);

        // 0.05•bˆÛ
        yield return new WaitForSeconds(0.05f);

        // ™X‚ÉˆÃ‚­‚·‚é
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = time / duration;

            // 20 ¨ originalIntensity ‚É–ß‚·
            bloom.intensity.value = Mathf.Lerp(flashIntensity, originalIntensity, t);
            Debug.Log("Lerp: " + bloom.intensity.value);

            yield return null;
        }

        // ÅI“I‚ÉŒ³‚Ì’l‚Ö
        bloom.intensity.value = originalIntensity;
    }


}
