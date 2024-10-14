using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float crouchSpeed = 2f;
    public float jumpForce = 5f;
    public float mouseSensitivity = 100f;
    public Transform playerCamera;

    [Header("Camera Bobbing Settings")]
    public float walkBobbingSpeed = 10f;
    public float runBobbingSpeed = 15f;
    public float crouchBobbingSpeed = 5f;
    public float bobbingAmount = 0.05f;
    public float crouchBobbingAmount = 0.02f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float crouchTransitionSpeed = 5f;
    public float crouchColliderHeight = 1f;

    [Header("Sliding Settings")]
    public float slideDuration = 1f;
    public float slideSpeedMultiplier = 1.5f;
    public float slideHeight = 0.5f;
    public float slideTransitionSpeed = 10f;

    [Header("Audio Settings")]
    public AudioClip walkingSound;
    public AudioClip slidingSound;
    public AudioSource walkingAudioSource;
    public AudioSource slideAudioSource;

    private Rigidbody rb;
    private float xRotation = 0f;
    private float currentSpeed;
    private float currentBobbingAmount;
    private float currentBobbingSpeed;
    private float defaultCameraYPos;
    private float bobbingTimer = 0f;
    private bool isCrouching = false;

    private bool isGrounded;
    public Transform groundCheck;
    public LayerMask groundMask;
    public float groundDistance = 0.4f;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private CapsuleCollider playerCollider;

    public bool IsAiming { get; set; }

    public float WalkSpeed { get { return walkSpeed; } }
    public float RunSpeed { get { return runSpeed; } }
    public bool IsGrounded { get { return isGrounded; } }

    private float defaultHeight;
    private bool isSliding = false;
    private float slideTimerInternal = 0f;
    private float initialSlideSpeed;
    private Vector3 originalCameraPosition;

    // *** New flag to request slide upon landing ***
    private bool requestedSlide = false;

    // *** New variable to store the last grounded speed ***
    private float lastGroundedSpeed = 0f;

    // *** New variables to handle movement direction locking mid-air ***
    private Vector3 airMoveDirection = Vector3.zero;
    private bool wasGrounded = true; // To track previous grounded state

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentSpeed = walkSpeed;
        lastGroundedSpeed = walkSpeed; // Initialize with walkSpeed
        currentBobbingSpeed = walkBobbingSpeed;
        currentBobbingAmount = bobbingAmount;
        defaultCameraYPos = playerCamera.localPosition.y;
        defaultHeight = playerCamera.localPosition.y;

        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;
        originalCameraPosition = playerCamera.localPosition;

        if (walkingAudioSource == null)
        {
            Debug.LogError("Walking AudioSource is not assigned. Please assign it in the Inspector.");
        }
        if (slideAudioSource == null)
        {
            Debug.LogError("Slide AudioSource is not assigned. Please assign it in the Inspector.");
        }

        if (walkingAudioSource != null && walkingSound != null)
        {
            walkingAudioSource.clip = walkingSound;
            walkingAudioSource.loop = true;
        }
        else
        {
            Debug.LogError("Walking AudioSource or Walking Sound AudioClip is not assigned.");
        }

        if (slideAudioSource != null && slidingSound != null)
        {
            slideAudioSource.clip = slidingSound;
            slideAudioSource.loop = true;
            slideAudioSource.playOnAwake = false;
        }
        else
        {
            Debug.LogError("Slide AudioSource or Sliding Sound AudioClip is not assigned.");
        }

        // *** Configure Rigidbody Settings ***
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraBobbing();
        HandleJump();
        HandleCrouch();
        UpdateGroundCheckPosition();
    }

    void FixedUpdate()
    {
        HandleMovement();

        // *** Check for slide request upon landing ***
        if (requestedSlide && isGrounded && !isSliding)
        {
            StartSlide();
            requestedSlide = false;
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 inputMove = transform.right * x + transform.forward * z;
        Vector3 move;

        if (isGrounded)
        {
            if (!wasGrounded)
            {
                wasGrounded = true;
                airMoveDirection = Vector3.zero;
            }

            if (IsAiming)
            {
                currentSpeed = walkSpeed;
            }
            else if (isSliding)
            {
                // Slide manages currentSpeed internally
            }
            else if (isCrouching)
            {
                currentSpeed = crouchSpeed;
            }
            else
            {
                currentSpeed = (Input.GetKey(KeyCode.LeftShift) && z > 0 && !isCrouching) ? runSpeed : walkSpeed;
            }

            if (inputMove.magnitude > 1f)
            {
                inputMove.Normalize();
            }

            if (inputMove.magnitude == 0 && !isSliding)
            {
                currentSpeed = 0f;
            }

            lastGroundedSpeed = currentSpeed;
            move = inputMove;
        }
        else
        {
            if (wasGrounded)
            {
                airMoveDirection = inputMove.normalized;
                wasGrounded = false;
            }

            move = airMoveDirection;
            currentSpeed = lastGroundedSpeed;
        }

        Vector3 desiredVelocity = move * currentSpeed;
        Vector3 currentVelocity = rb.velocity;
        Vector3 newVelocity = new Vector3(desiredVelocity.x, currentVelocity.y, desiredVelocity.z);
        rb.velocity = newVelocity;

        // Handle walking and sprinting sounds
        if (currentSpeed > 0 && isGrounded && !IsAiming && !isCrouching)
        {
            if (currentSpeed == runSpeed)
            {
                walkingAudioSource.pitch = 1.5f;
            }
            else if (currentSpeed == walkSpeed)
            {
                walkingAudioSource.pitch = 1f;
            }

            if (!walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Play();
            }
        }
        else
        {
            if (walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Stop();
                walkingAudioSource.pitch = 1f;
            }
        }

    }


    void HandleCameraBobbing()
    {
        // *** Skip camera bobbing during sliding ***
        if (isSliding)
        {
            // Optionally, reset camera position to slideHeight to ensure stability
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, slideHeight, playerCamera.localPosition.z);
            return;
        }

        // Determine if the player is moving based on move direction
        bool isMoving = false;
        if (isGrounded)
        {
            isMoving = (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f);
        }
        else
        {
            // While airborne, consider moving if airMoveDirection is significant
            isMoving = airMoveDirection.magnitude > 0.1f;
        }

        if (isMoving)
        {
            float bobbingSpeed;
            float bobbingAmountLocal;

            if (isSliding)
            {
                bobbingSpeed = runBobbingSpeed; // Or a different value for sliding
                bobbingAmountLocal = bobbingAmount; // Adjust as needed
            }
            else
            {
                bobbingSpeed = (currentSpeed == runSpeed) ? runBobbingSpeed : currentBobbingSpeed;
                bobbingAmountLocal = (isCrouching) ? crouchBobbingAmount : currentBobbingAmount;
            }

            bobbingTimer += Time.deltaTime * bobbingSpeed;
            float newCameraYPos = defaultCameraYPos + Mathf.Sin(bobbingTimer) * bobbingAmountLocal;
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, newCameraYPos, playerCamera.localPosition.z);
        }
        else
        {
            bobbingTimer = 0f;
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, defaultCameraYPos, playerCamera.localPosition.z);
        }
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump"))
        {
            if (isSliding)
            {
                // **Cancel the slide if already sliding**
                CancelSlide();
            }
            else if (isGrounded && !isCrouching)
            {
                // **Perform a jump if grounded, not crouching, and not sliding **
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                // Jump sound effect removed
            }
        }
    }

    void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isSliding)
            {
                CancelSlide();
            }
            else
            {
                if (isGrounded)
                {
                    if (lastGroundedSpeed > walkSpeed)
                    {
                        StartSlide();
                    }
                }
                else
                {
                    if (lastGroundedSpeed > walkSpeed)
                    {
                        requestedSlide = true;
                    }
                }
            }
        }

        if (isSliding)
        {
            ContinueSlide();
        }
        else if (Input.GetKey(KeyCode.C) && isGrounded)
        {
            // Existing crouch logic
            playerCamera.localPosition = Vector3.Lerp(
                playerCamera.localPosition,
                new Vector3(playerCamera.localPosition.x, crouchHeight, playerCamera.localPosition.z),
                Time.deltaTime * crouchTransitionSpeed
            );

            playerCollider.height = Mathf.Lerp(playerCollider.height, crouchColliderHeight, Time.deltaTime * crouchTransitionSpeed);
            playerCollider.center = Vector3.Lerp(
                playerCollider.center,
                new Vector3(0, crouchColliderHeight / 2f, 0),
                Time.deltaTime * crouchTransitionSpeed
            );

            // Adjust groundCheck position
            float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
            groundCheck.localPosition = new Vector3(0, colliderBottom, 0);

            isCrouching = true;
        }
        else
        {
            if (!isSliding && !Input.GetKey(KeyCode.C))
            {
                // Revert to standing
                playerCamera.localPosition = Vector3.Lerp(
                    playerCamera.localPosition,
                    new Vector3(playerCamera.localPosition.x, defaultHeight, playerCamera.localPosition.z),
                    Time.deltaTime * crouchTransitionSpeed
                );
                playerCollider.height = Mathf.Lerp(playerCollider.height, originalColliderHeight, Time.deltaTime * crouchTransitionSpeed);
                playerCollider.center = Vector3.Lerp(playerCollider.center, originalColliderCenter, Time.deltaTime * crouchTransitionSpeed);

                // Adjust groundCheck position
                float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
                groundCheck.localPosition = new Vector3(0, colliderBottom, 0);

                isCrouching = false;
            }
        }
    }


    void StartSlide()
    {
        // *** Ensure sliding only starts when grounded ***
        if (!isGrounded)
        {
            Debug.LogWarning("Cannot start slide: Player is not grounded.");
            return;
        }

        // *** Additional check to ensure speed is greater than walking speed ***
        if (lastGroundedSpeed <= walkSpeed)
        {
            Debug.Log("Cannot start slide: Speed is not greater than walking speed.");
            return;
        }

        isSliding = true;
        slideTimerInternal = slideDuration;

        // *** Initialize sliding speed ***
        initialSlideSpeed = walkSpeed * slideSpeedMultiplier;
        currentSpeed = initialSlideSpeed;

        // *** Set isCrouching to true while sliding ***
        isCrouching = true;

        // *** Log slide initiation ***
        Debug.Log("Slide started.");

        // *** Play sliding sound ***
        if (slideAudioSource != null && slidingSound != null)
        {
            slideAudioSource.Play();
            Debug.Log("Sliding sound started.");
        }

        // Adjust camera position for sliding
        StartCoroutine(SmoothTransition(
            playerCamera.localPosition,
            new Vector3(playerCamera.localPosition.x, slideHeight, playerCamera.localPosition.z),
            slideTransitionSpeed
        ));

        // Adjust collider for sliding
        StartCoroutine(AdjustColliderHeight(crouchColliderHeight, slideTransitionSpeed));
    }

    void ContinueSlide()
    {
        if (slideTimerInternal > 0)
        {
            // *** Calculate the proportion of slide completed ***
            float speedDecayFactor = slideTimerInternal / slideDuration; // From 1 to 0

            // *** Linearly interpolate speed from crouchSpeed to initialSlideSpeed ***
            currentSpeed = Mathf.Lerp(crouchSpeed, initialSlideSpeed, speedDecayFactor);

            slideTimerInternal -= Time.deltaTime;
        }
        else
        {
            EndSlide();
        }
    }

    void EndSlide()
    {
        isSliding = false;

        // Revert speed back to crouchSpeed
        currentSpeed = crouchSpeed;

        // *** Only keep isCrouching true if the player is holding the crouch button ***
        if (!Input.GetKey(KeyCode.C))
        {
            isCrouching = false;
        }

        // *** Log slide end ***
        Debug.Log("Slide ended.");

        // *** Stop sliding sound ***
        if (slideAudioSource != null)
        {
            slideAudioSource.Stop();
            Debug.Log("Sliding sound stopped.");
        }

        // Smoothly transition the camera back to original position
        StartCoroutine(SmoothTransition(
            playerCamera.localPosition,
            originalCameraPosition,
            slideTransitionSpeed
        ));

        // Revert collider to original size
        StartCoroutine(AdjustColliderHeight(originalColliderHeight, slideTransitionSpeed));
    }

    void CancelSlide()
    {
        isSliding = false;
        slideTimerInternal = 0f;

        // Determine the new speed based on whether LeftShift is held
        if (Input.GetKey(KeyCode.LeftShift) && IsGrounded && !isCrouching)
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }

        // *** Update isCrouching based on whether the crouch button is held ***
        if (!Input.GetKey(KeyCode.C))
        {
            isCrouching = false;
        }
        if (slideAudioSource != null)
        {
            slideAudioSource.Stop();
            Debug.Log("Sliding sound stopped.");
        }
        StartCoroutine(SmoothTransition(
            playerCamera.localPosition,
            originalCameraPosition,
            slideTransitionSpeed
        ));
        StartCoroutine(AdjustColliderHeight(originalColliderHeight, slideTransitionSpeed));
    }

    void UpdateGroundCheckPosition()
    {
        // Calculate the bottom position of the collider
        float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
        // Set groundCheck position relative to the player's position
        groundCheck.position = transform.position + new Vector3(0, colliderBottom, 0);
    }


    IEnumerator SmoothTransition(Vector3 from, Vector3 to, float speed)
    {
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            playerCamera.localPosition = Vector3.Lerp(from, to, elapsed);
            elapsed += Time.deltaTime * speed;
            yield return null;
        }
        playerCamera.localPosition = to;
    }

    IEnumerator AdjustColliderHeight(float targetHeight, float speed)
    {
        float elapsed = 0f;
        float startingHeight = playerCollider.height;
        Vector3 startingCenter = playerCollider.center;

        Vector3 targetCenter = new Vector3(0, targetHeight / 2f, 0);
        Vector3 startingColliderCenter = playerCollider.center;

        while (elapsed < 1f)
        {
            playerCollider.height = Mathf.Lerp(startingHeight, targetHeight, elapsed);
            playerCollider.center = Vector3.Lerp(startingCenter, targetCenter, elapsed);
            elapsed += Time.deltaTime * speed;
            yield return null;
        }

        playerCollider.height = targetHeight;
        playerCollider.center = targetCenter;
    }
}
