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
    private bool isAttacking = false;

    private enum State { Idle, Walking, Limping, Attacking, Stunned }
    private State currentState = State.Idle;

    [Header("Sinking Settings")]
    public float sinkDelay = 3f;
    public float sinkDistance = 5f;
    public float sinkDuration = 2f;

    [Header("Audio Clips")]
    public AudioClip footstepClip;
    public AudioClip damageClip;
    public AudioClip deathClip;
    public AudioClip attackClip;
    public AudioClip growlClip;

    private AudioSource audioSource;
    private AudioSource growlSource;

    [Header("Damage Settings")]
    public float damageAmount = 10f;
    private PlayerHealth playerHealth;

    public event System.Action OnDeath;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.stoppingDistance = attackRange;
        navAgent.updateRotation = false;
        
        if (target == null)
        {
            target = GameObject.FindWithTag("Player")?.transform;
            if (target == null) Debug.LogError("Target not found! Ensure there's a GameObject tagged 'Player' in the scene.");
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null) Debug.LogError("Animator component not found on this GameObject.");
        }

        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null) Debug.LogError("AudioSource component is missing on MutantZombie!");

        if (attackClip == null) Debug.LogError("Attack Clip is not assigned in the Inspector!");

        playerHealth = target.GetComponent<PlayerHealth>();
        if (playerHealth == null) Debug.LogError("PlayerHealth component is missing on the target!");

        InitializeGrowlSource();
    }

    void InitializeGrowlSource()
    {
        if (growlClip == null)
        {
            Debug.LogError("Growl Clip is not assigned in the Inspector!");
            return;
        }

        Transform existingGrowl = transform.Find("GrowlAudioSource");
        if (existingGrowl != null)
        {
            growlSource = existingGrowl.GetComponent<AudioSource>();
            if (growlSource == null) growlSource = existingGrowl.gameObject.AddComponent<AudioSource>();
        }
        else
        {
            GameObject growlObject = new GameObject("GrowlAudioSource");
            growlObject.transform.parent = this.transform;
            growlSource = growlObject.AddComponent<AudioSource>();
            growlSource.playOnAwake = false;
            growlSource.loop = true;
            growlSource.spatialBlend = 1.0f;
        }

        growlSource.clip = growlClip;
        growlSource.volume = 1.0f;
    }

    void Update()
    {
        if (isDead)
        {
            StopFootstepAndGrowl();
            return;
        }

        if (target != null && !isStunned)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            switch (currentState)
            {
                case State.Idle:
                    StopFootstepAndGrowl();
                    if (distanceToTarget <= walkDistance)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                        navAgent.SetDestination(target.position);
                        RotateTowardsTarget();
                        PlayFootstepAndGrowl();
                    }
                    break;

                case State.Walking:
                    if (!isAttacking)
                    {
                        if (!audioSource.isPlaying || audioSource.clip != footstepClip)
                        {
                            if (footstepClip != null)
                            {
                                audioSource.clip = footstepClip;
                                audioSource.loop = true;
                                audioSource.Play();
                            }
                            else Debug.LogError("Footstep Clip is not assigned!");
                        }
                        if (!growlSource.isPlaying) PlayGrowlWithRandomStart();
                    }
                    else StopFootstepAndGrowl();

                    if (distanceToTarget > walkDistance)
                    {
                        currentState = State.Idle;
                        animator.SetBool("isWalking", false);
                        navAgent.ResetPath();
                        StopFootstepAndGrowl();
                    }
                    else if (distanceToTarget <= attackRange)
                    {
                        currentState = State.Attacking;
                        animator.SetBool("isWalking", false);
                        isAttacking = false;
                        StopFootstepAndGrowl();
                    }
                    else
                    {
                        navAgent.SetDestination(target.position);
                        RotateTowardsTarget();
                        animator.SetBool("isWalking", true);
                        if (!growlSource.isPlaying) PlayGrowlWithRandomStart();
                    }
                    break;

                case State.Attacking:
                    if (distanceToTarget > attackRange)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                        navAgent.SetDestination(target.position);
                        isAttacking = false;
                        PlayFootstepAndGrowl();
                    }
                    else
                    {
                        RotateTowardsTarget();
                        if (!isAttacking) HandleAttack();
                    }
                    break;

                case State.Limping:
                    break;

                case State.Stunned:
                    break;
            }
        }
        else StopFootstepAndGrowl();
    }

    void PlayGrowlWithRandomStart()
    {
        if (growlClip != null && growlSource != null)
        {
            float randomStartTime = Random.Range(0f, growlClip.length);
            growlSource.time = randomStartTime;
            growlSource.Play();
        }
        else
        {
            if (growlClip == null) Debug.LogError("Growl Clip is not assigned!");
            if (growlSource == null) Debug.LogError("Growl AudioSource is not initialized!");
        }
    }

    void PlayFootstepAndGrowl()
    {
        if (footstepClip != null && audioSource != null)
        {
            if (!audioSource.isPlaying || audioSource.clip != footstepClip)
            {
                audioSource.clip = footstepClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }
        else
        {
            if (footstepClip == null) Debug.LogError("Footstep Clip is not assigned!");
            if (audioSource == null) Debug.LogError("AudioSource is not assigned!");
        }

        if (growlClip != null && growlSource != null)
        {
            if (!growlSource.isPlaying) PlayGrowlWithRandomStart();
        }
        else
        {
            if (growlClip == null) Debug.LogError("Growl Clip is not assigned!");
            if (growlSource == null) Debug.LogError("Growl AudioSource is not initialized!");
        }
    }

    void StopFootstepAndGrowl()
    {
        if (audioSource != null && audioSource.isPlaying && audioSource.clip == footstepClip) audioSource.Stop();
        if (growlSource != null && growlSource.isPlaying) growlSource.Stop();
    }

    void RotateTowardsTarget()
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    void HandleAttack()
    {
        if (isAttacking) return;

        isAttacking = true;
        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;
        animator.SetTrigger("Attack");

        StopFootstepAndGrowl();

        if (attackClip != null) audioSource.PlayOneShot(attackClip);
        else Debug.LogError("Attack Clip is not assigned!");

        if (playerHealth != null && Vector3.Distance(transform.position, target.position) <= attackRange) playerHealth.TakeDamage(damageAmount);
        StartCoroutine(ResetAttack());
    }

    IEnumerator ResetAttack()
    {
        AnimatorStateInfo attackStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float attackDuration = attackStateInfo.length;

        if (!attackStateInfo.IsName("Attack")) attackDuration = 1.0f;
        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        if (currentState == State.Attacking && !isDead && !isStunned)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            if (distanceToTarget > attackRange)
            {
                currentState = State.Walking;
                animator.SetBool("isWalking", true);
                navAgent.SetDestination(target.position);
                PlayFootstepAndGrowl();
            }
            else HandleAttack();
        }
    }

    public void LimpMove()
    {
        if (!isLimping && currentState != State.Limping && !isStunned && !isDead) StartCoroutine(LimpTowardsTarget());
    }

    IEnumerator LimpTowardsTarget()
    {
        isLimping = true;
        currentState = State.Limping;
        navAgent.enabled = false;

        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Vector3 limpDestination = transform.position + directionToTarget * limpDistance;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(limpDestination, out hit, limpDistance, NavMesh.AllAreas)) limpDestination = hit.position;

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
        PlayFootstepAndGrowl();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (damageClip != null && audioSource != null) audioSource.PlayOneShot(damageClip);
        else
        {
            if (damageClip == null) Debug.LogError("Damage Clip is not assigned!");
            if (audioSource == null) Debug.LogError("AudioSource is not assigned!");
        }

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

        StopFootstepAndGrowl();

        yield return new WaitForSeconds(stunDuration);

        navAgent.enabled = true;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= attackRange)
        {
            currentState = State.Attacking;
            animator.SetBool("isWalking", false);
            isAttacking = false;
        }
        else if (distanceToTarget <= walkDistance)
        {
            currentState = State.Walking;
            animator.SetBool("isWalking", true);
            navAgent.SetDestination(target.position);
            PlayFootstepAndGrowl();
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
        playerHealth.AddMoney();
        OnDeath?.Invoke();
        animator.SetTrigger("Death");
        if (deathClip != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(deathClip);
        }
        else
        {
            if (deathClip == null) Debug.LogError("Death Clip is not assigned!");
            if (audioSource == null) Debug.LogError("AudioSource is not assigned!");
        }

        if (growlSource != null && growlSource.isPlaying) growlSource.Stop();
        else
        {
            if (growlSource == null) Debug.LogError("Growl AudioSource is not assigned!");
            else Debug.Log("Growl sound was not playing.");
        }

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders) col.enabled = false;
        navAgent.enabled = false;

        if (growlSource != null) growlSource.gameObject.SetActive(false);

        this.enabled = false;
        StartCoroutine(SinkIntoFloor());
    }

    IEnumerator SinkIntoFloor()
    {
        yield return new WaitForSeconds(sinkDelay);
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkDistance);
    }
}
