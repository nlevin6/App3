using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float health = 100f;
    public float regenerationRate = 5f;
    public float regenerationDelay = 5f;

    [Header("Audio Settings")]
    public AudioClip grunt1;
    public AudioClip grunt2;
    public AudioClip grunt3;
    public AudioClip deathClip;
    public AudioClip regenerationClip;
    public AudioSource audioSource;

    [Header("Damage Settings")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;

    [Header("Screen Bleed Settings")]
    public RawImage screenBleedImage;

    [Header("Damage Animation Settings")]
    public float damageDuration = 1f;

    [Header("Camera Shake Settings")]
    public Camera mainCamera;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    private bool canRegenerate = false;
    private float timeSinceLastDamage;
    private bool isRegenerating = false;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            Debug.LogError("AudioSource component is missing on Player!");
        }

        if (screenBleedImage == null)
        {
            Debug.LogError("Screen Bleed Image is not assigned!");
        }
        else
        {
            SetScreenBleedOpacity(0f);
        }
    }

    private void Update()
    {
        if (canRegenerate)
        {
            RegenerateHealth();
        }

        if (timeSinceLastDamage < regenerationDelay)
        {
            timeSinceLastDamage += Time.deltaTime;
        }
        else
        {
            canRegenerate = true;
        }
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible || health <= 0f)
        {
            return;
        }

        PlayRandomGrunt();
        StartCoroutine(GradualDamage(damage));
        StartCoroutine(CameraShake());

        timeSinceLastDamage = 0f;
        canRegenerate = false;
        isRegenerating = false;

        if (health <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCooldown());
        }
    }

    private IEnumerator GradualDamage(float damage)
    {
        float startHealth = health;
        float targetHealth = Mathf.Max(health - damage, 0f);
        float elapsedTime = 0f;

        while (elapsedTime < damageDuration)
        {
            elapsedTime += Time.deltaTime;
            health = Mathf.Lerp(startHealth, targetHealth, elapsedTime / damageDuration);
            UpdateScreenBleedEffect();
            yield return null;
        }

        health = targetHealth;
        UpdateScreenBleedEffect();

        if (health <= 0f)
        {
            Die();
        }
    }

    private void PlayRandomGrunt()
    {
        AudioClip[] grunts = { grunt1, grunt2, grunt3 };
        int randomIndex = Random.Range(0, grunts.Length);
        PlaySound(grunts[randomIndex]);
    }

    private IEnumerator CameraShake()
    {
        Vector3 originalPosition = mainCamera.transform.localPosition;
        float elapsedTime = 0f;

        while (elapsedTime < shakeDuration)
        {
            float offsetX = Random.Range(-1f, 1f) * shakeMagnitude;
            float offsetY = Random.Range(-1f, 1f) * shakeMagnitude;
            mainCamera.transform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        mainCamera.transform.localPosition = originalPosition;
    }

    private void RegenerateHealth()
    {
        if (health < 100f)
        {
            if (!isRegenerating && health < 80f)
            {
                PlaySound(regenerationClip);
                isRegenerating = true;
            }

            health += regenerationRate * Time.deltaTime;
            health = Mathf.Min(health, 100f);
            UpdateScreenBleedEffect();
        }
        else
        {
            isRegenerating = false;
        }
    }

    private void UpdateScreenBleedEffect()
    {
        float opacity = 0f;
        
        if (health < 80f)
        {
            opacity = Mathf.Clamp01((80f - health) / 80f);
        }

        SetScreenBleedOpacity(opacity);
    }

    private void SetScreenBleedOpacity(float opacity)
    {
        if (screenBleedImage != null)
        {
            Color color = screenBleedImage.color;
            color.a = opacity;
            screenBleedImage.color = color;
        }
    }

    private void Die()
    {
        PlaySound(deathClip);
        GetComponent<PlayerMovement>().enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else
        {
            if (clip == null)
            {
                Debug.LogError("Sound clip is not assigned!");
            }
            if (audioSource == null)
            {
                Debug.LogError("AudioSource is not assigned!");
            }
        }
    }

    private IEnumerator InvincibilityCooldown()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        isInvincible = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        MutantZombie enemyAttack = other.GetComponent<MutantZombie>();
        if (enemyAttack != null)
        {
            TakeDamage(enemyAttack.damageAmount);
        }
    }

    public void Heal(float amount)
    {
        health += amount;
        health = Mathf.Min(health, 100f);
        UpdateScreenBleedEffect();
    }
}
