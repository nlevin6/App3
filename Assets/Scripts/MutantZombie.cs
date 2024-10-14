using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MutantZombie : MonoBehaviour
{
    public Transform target;           // Set this to the DummyPlayer or player object later
    public Animator animator;         // Reference to the zombie's animator

    public float attackRange = 2f;    // Distance at which the zombie will attack
    public float walkDistance = 15f;  // Distance at which the zombie starts walking towards the target

    private NavMeshAgent navAgent;    // NavMeshAgent to handle pathfinding
    public float limpDistance = 0.5f; // The distance the zombie moves with each limp
    public float limpDuration = 0.2f; // Duration for each limp movement

    private bool isLimping = false;   // To avoid starting multiple limping movements at once

    // Define states for clarity
    private enum State { Idle, Walking, Limping, Attacking }
    private State currentState = State.Idle;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();

        // Set the stopping distance to attack range
        navAgent.stoppingDistance = attackRange;

        // Disable automatic rotation updates of NavMeshAgent
        navAgent.updateRotation = false;

        if (target == null)
        {
            Debug.LogError("Target is not assigned!");
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            switch (currentState)
            {
                case State.Idle:
                    if (distanceToTarget <= walkDistance)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                    }
                    break;

                case State.Walking:
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
                        // Continue walking towards the target
                        navAgent.SetDestination(target.position);
                        RotateTowardsTarget();
                        animator.SetBool("isWalking", true);
                    }
                    break;

                case State.Attacking:
                    if (distanceToTarget > attackRange)
                    {
                        currentState = State.Walking;
                        animator.SetBool("isWalking", true);
                    }
                    else
                    {
                        // Continue attacking
                        HandleAttack();
                    }
                    break;

            }
        }
    }


    void RotateTowardsTarget()
    {
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f); // Smoothly rotate
    }

    void HandleAttack()
    {
        // Stop the NavMeshAgent from moving
        navAgent.ResetPath();
        navAgent.velocity = Vector3.zero;

        // Trigger attack animation
        animator.SetTrigger("Attack");
    }

    // Animation Event Handler for moving the zombie during the limp
    public void LimpMove()
    {
        if (!isLimping && currentState != State.Limping)
        {
            // Start the limping movement as a coroutine
            StartCoroutine(LimpTowardsTarget());
        }
    }

    IEnumerator LimpTowardsTarget()
    {
        Debug.Log("LimpTowardsTarget coroutine started");
        isLimping = true;
        currentState = State.Limping;

        // Completely disable the NavMeshAgent to prevent it from controlling movement
        navAgent.enabled = false;

        // Calculate the direction towards the target
        Vector3 directionToTarget = (target.position - transform.position).normalized;

        // Determine the limp destination
        Vector3 limpDestination = transform.position + directionToTarget * limpDistance;

        // Ensure the limp destination is on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(limpDestination, out hit, limpDistance, NavMesh.AllAreas))
        {
            limpDestination = hit.position;
        }

        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;

        // Move the zombie towards limpDestination over limpDuration
        while (elapsedTime < limpDuration)
        {
            transform.position = Vector3.Lerp(startPosition, limpDestination, (elapsedTime / limpDuration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is exact
        transform.position = limpDestination;

        // Re-enable the NavMeshAgent and set the new destination
        navAgent.enabled = true;
        navAgent.SetDestination(target.position);

        currentState = State.Walking;
        isLimping = false;

        Debug.Log("LimpTowardsTarget coroutine ended");
    }


    public void TakeDamage()
    {
        animator.SetTrigger("Damage");
    }

    public void Die()
    {
        animator.SetTrigger("Death");
        this.enabled = false; // Disable this script after death
    }

    // Optional: Visualize attack and walk ranges in the Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkDistance);
    }
}
