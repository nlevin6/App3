using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MutantZombie : MonoBehaviour
{
    [Header("Target and Animator")]
    public Transform target;
    public Animator animator;

    [Header("Movement Settings")]
    public float attackRange = 2f;
    public float walkDistance = 15f;

    [Header("Limping Settings")]
    public float limpDistance = 0.5f;
    public float limpDuration = 0.2f;

    [Header("Health Settings")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Stun Settings")]
    public float stunDuration = 1f;

    private NavMeshAgent navAgent;
    private bool isLimping = false;
    private bool isStunned = false;
    private bool isDead = false;

    private enum State { Idle, Walking, Limping, Attacking, Stunned }
    private State currentState = State.Idle;

    // MutantZombie despawning after death settings
    [Header("Sinking Settings")]
    public float sinkDelay = 3f;
    public float sinkDistance = 5f;
    public float sinkDuration = 2f;

    [Header("Audio Clips")]
    public AudioClip footstepClip;
    public AudioClip damageClip;
    public AudioClip deathClip;

    private AudioSource audioSource; // For playing sound effects

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();

        navAgent.stoppingDistance = attackRange;
        navAgent.updateRotation = false;

        if (target == null)
        {
            Debug.LogError("Target is not assigned!");
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        currentHealth = maxHealth;

        // Initialize AudioSource
        audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (target != null && !isStunned && !isDead)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            switch (currentState)
            {
                case State.Idle:
                    if (audioSource.isPlaying) audioSource.Stop(); // Stop footsteps
                    if (distanceToTarget <= walkDistance)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                        navAgent.SetDestination(target.position);
                        RotateTowardsTarget();
                    }
                    break;

                case State.Walking:
                    if (!audioSource.isPlaying)
                    {
                        audioSource.clip = footstepClip;
                        audioSource.loop = true;
                        audioSource.Play(); // Play footsteps sound when walking
                    }
                    if (distanceToTarget > walkDistance)
                    {
                        currentState = State.Idle;
                        animator.SetBool("isWalking", false);
                        navAgent.ResetPath();
                    }
                    else if (distanceToTarget <= attackRange)
                    {
                        currentState = State.Attacking;
                        animator.SetBool("isWalking", false);
                        HandleAttack();
                    }
                    else
                    {
                        navAgent.SetDestination(target.position);
                        RotateTowardsTarget();
                        animator.SetBool("isWalking", true);
                    }
                    break;

                case State.Attacking:
                    if (audioSource.isPlaying) audioSource.Stop(); // Stop footsteps during attack
                    if (distanceToTarget > attackRange)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                        navAgent.SetDestination(target.position);
                    }
                    else
                    {
                        HandleAttack();
                    }
                    break;

                case State.Limping:
                    break;

                case State.Stunned:
                    break;
            }
        }
    }

    void RotateTowardsTarget()
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    void HandleAttack()
    {
        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;

        animator.SetTrigger("Attack");
    }

    public void LimpMove()
    {
        if (!isLimping && currentState != State.Limping && !isStunned && !isDead)
        {
            StartCoroutine(LimpTowardsTarget());
        }
    }

    IEnumerator LimpTowardsTarget()
    {
        Debug.Log("LimpTowardsTarget coroutine started");
        isLimping = true;
        currentState = State.Limping;

        navAgent.enabled = false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;

        Vector3 limpDestination = transform.position + directionToTarget * limpDistance;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(limpDestination, out hit, limpDistance, NavMesh.AllAreas))
        {
            limpDestination = hit.position;
        }

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        while (elapsedTime < limpDuration && !isStunned && !isDead)
        {
            transform.position = Vector3.Lerp(startPosition, limpDestination, (elapsedTime / limpDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = limpDestination;

        navAgent.enabled = true;
        navAgent.SetDestination(target.position);

        currentState = State.Walking;
        isLimping = false;

        Debug.Log("LimpTowardsTarget coroutine ended");
    }

    public void TakeDamage(float damage)
    {
        if (isDead)
            return;

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. Remaining health: {currentHealth}");

        // Play damage sound
        audioSource.PlayOneShot(damageClip);

        if (currentHealth <= 0f && !isDead)
        {
            isDead = true;
            Die();
        }
        else
        {
            animator.SetTrigger("Damage");
            StartCoroutine(Stun());
        }
    }

    IEnumerator Stun()
    {
        isStunned = true;
        currentState = State.Stunned;

        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        navAgent.enabled = false;

        if (isLimping)
        {
            StopCoroutine(LimpTowardsTarget());
            isLimping = false;
        }

        yield return new WaitForSeconds(stunDuration);

        navAgent.enabled = true;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            currentState = State.Attacking;
            animator.SetBool("isWalking", false);
            HandleAttack();
        }
        else if (distanceToTarget <= walkDistance)
        {
            currentState = State.Walking;
            animator.SetBool("isWalking", true);
            navAgent.SetDestination(target.position);
        }
        else
        {
            currentState = State.Idle;
            animator.SetBool("isWalking", false);
            navAgent.ResetPath();
        }

        isStunned = false;
    }

    public void Die()
    {
        animator.SetTrigger("Death");

        // Play death sound
        audioSource.PlayOneShot(deathClip);

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }
        navAgent.enabled = false;
        this.enabled = false;
        StartCoroutine(SinkIntoFloor());
    }

    IEnumerator SinkIntoFloor()
    {
        yield return new WaitForSeconds(sinkDelay);

        Debug.Log("Sinking into the floor...");

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.down * sinkDistance;

        float elapsedTime = 0f;

        while (elapsedTime < sinkDuration)
        {
            transform.position = Vector3.Lerp(startPosition, endPosition, elapsedTime / sinkDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        Destroy(gameObject);
    }

    // Visualize attack and walk ranges in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkDistance);
    }
}
