using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float health = 100f;

    [Header("Audio Settings")]
    public AudioClip damageClip;
    public AudioClip deathClip;
    public AudioSource audioSource;

    [Header("Damage Settings")]
    public float invincibilityDuration = 1f;
    private bool isInvincible = false;

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
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible || health <= 0f)
        {
            return;
        }

        health -= damage;
        //Debug.Log(health);
        PlaySound(damageClip);

        if (health <= 0f)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCooldown());
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
    }
}
