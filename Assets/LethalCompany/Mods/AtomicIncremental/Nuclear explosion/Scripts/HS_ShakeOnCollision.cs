using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HS_ShakeOnCollision : MonoBehaviour
{
    [Space]
    [Header("Camera Shaker script")]
    private HS_CameraShaker cameraShaker;
    public float amplitude;
    public float frequency;
    public float duration;
    public float timeRemaining;

    [Space]
    [Header("Explosion sphere")]
    public float explosionFinalRadious = 850;
    public float explosionCurrentRadious = 0;
    public AnimationCurve sizeCurve;
    public float shockWaveLifetime = 6f;
    public float repeatingTime = 15f;
    public LayerMask layers = ~0;
    private List<Collider> addedColliders = new List<Collider>();

    [Space]
    [Header("Sound effects")]
    private AudioSource soundComponent;
    private AudioClip explosionClip;

    void Start()
    {
        soundComponent = GetComponent<AudioSource>();
        explosionClip = soundComponent.clip;
        StartCoroutine(ExplosionShockWave());
    }

    public void Update()
    {

    }

    public IEnumerator ExplosionShockWave()
    {
        float timer = 0;
        addedColliders.Clear();
        soundComponent.PlayOneShot(explosionClip);

        while (true)
        {
            timer += Time.deltaTime / shockWaveLifetime;
            explosionCurrentRadious = Mathf.Lerp(0, explosionFinalRadious, sizeCurve.Evaluate(timer));

            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionCurrentRadious, layers, QueryTriggerInteraction.UseGlobal);
            foreach (var hitCollider in hitColliders)
            {
                if (!addedColliders.Contains(hitCollider))
                {
                    if (hitCollider.GetComponent<HS_CameraShaker>() != null && hitCollider.GetComponent<AudioSource>())
                    {
                        AudioSource soundComponent2 = hitCollider.GetComponent<AudioSource>();
                        AudioClip shockwaveClip = soundComponent2.clip;
                        soundComponent2.PlayOneShot(shockwaveClip);

                        cameraShaker = hitCollider.GetComponent<HS_CameraShaker>();
                        StartCoroutine(cameraShaker.Shake(amplitude, frequency, duration, timeRemaining));
                    }
                    addedColliders.Add(hitCollider);
                }
            }

            if (explosionFinalRadious <= explosionCurrentRadious)
            {
                yield return new WaitForSeconds(repeatingTime- shockWaveLifetime);
                StartCoroutine(ExplosionShockWave());
                yield break;
            }
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionCurrentRadious);
    }
}
