using UnityEngine;
using UnityEngine.AI;  // For zombie movement using NavMesh

public class MutantZombie : MonoBehaviour
{
    public Transform target;  // Set this to the DummyPlayer or player object later
    public Animator animator;  // Reference to the zombie's animator

    // Movement settings
    public float moveSpeed = 3f;
    public float rotationSpeed = 2f;
    public float attackRange = 2f;  // Distance at which the zombie will attack
    public float walkDistance = 15f; // Distance at which the zombie starts walking towards the target
    public float stopDistance = 1.5f; // How close zombie gets to the player

    private NavMeshAgent navAgent;  // NavMeshAgent to handle movement

    void Start()
    {
        // Assign the NavMeshAgent component for movement
        navAgent = GetComponent<NavMeshAgent>();
        navAgent.speed = moveSpeed;

        // Disable automatic rotation of the agent
        navAgent.updateRotation = false;

        // Assign the dummy target (can be replaced with actual player later)
        if (target == null)
        {
            Debug.LogError("Target is not assigned!");
        }

        // Make sure the animator is assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (target != null)
        {
            // Get distance to the target
            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget <= walkDistance && distanceToTarget > stopDistance)
            {
                // Move towards the target
                navAgent.SetDestination(target.position);

                // Rotate towards the target
                RotateTowardsTarget();

                // Handle movement animations
                HandleMovementAnimations();
            }
            else if (distanceToTarget <= attackRange)
            {
                // Stop moving and attack
                navAgent.SetDestination(transform.position);
                HandleAttack();
            }
            else
            {
                // Stop all movement animations when out of range
                animator.SetBool("isWalking", false);
                navAgent.ResetPath();
            }
        }
    }

    void RotateTowardsTarget()
    {
        // Get direction to the target
        Vector3 directionToTarget = (target.position - transform.position).normalized;
        
        // Calculate rotation towards the target
        Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
        
        // Smoothly rotate towards the target
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    void HandleMovementAnimations()
    {
        // If the zombie is moving, play the walking animation
        if (navAgent.velocity.magnitude > 0.1f)  // Check if there's enough movement
        {
            animator.SetBool("isWalking", true);
        }
        else
        {
            animator.SetBool("isWalking", false);
        }
    }


    void HandleAttack()
    {
        // Trigger the attack animation
        animator.SetTrigger("Attack");
    }

    public void TakeDamage()
    {
        // Play damage animation
        animator.SetTrigger("Damage");
    }

    public void Die()
    {
        // Play death animation and disable movement
        animator.SetTrigger("Death");
        navAgent.isStopped = true;  // Stop moving
        this.enabled = false;  // Disable this script after death
    }
}
