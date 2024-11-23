using UnityEngine;

public class FadeOutEffect : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeDuration = 2f;

    private ParticleSystem particleSystem;
    private ParticleSystem.MainModule mainModule;
    private float fadeTimer = 0f;

    void Start()
    {
        particleSystem = GetComponent<ParticleSystem>();
        if (particleSystem == null)
        {
            return;
        }

        mainModule = particleSystem.main;
    }

    void Update()
    {
        if (particleSystem == null)
            return;

        fadeTimer += Time.deltaTime;
        float normalizedTime = fadeTimer / fadeDuration;

        if (normalizedTime >= 1f)
        {
            Destroy(gameObject);
            return;
        }
        Color startColor = mainModule.startColor.color;
        startColor.a = Mathf.Lerp(1f, 0f, normalizedTime);
        mainModule.startColor = new ParticleSystem.MinMaxGradient(startColor);
    }
}
