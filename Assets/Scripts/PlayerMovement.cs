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
    private float slideTimer = 0f;
    private float initialSlideSpeed;
    private Vector3 originalCameraPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentSpeed = walkSpeed;
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
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraBobbing();
        HandleJump();
        HandleCrouch();
    }

    void FixedUpdate()
    {
        HandleMovement();
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

        Vector3 move = transform.right * x + transform.forward * z;

        if (move.magnitude > 1f)
        {
            move.Normalize();
        }

        if (IsAiming)
        {
            currentSpeed = walkSpeed;
        }
        else if (isSliding)
        {
            // During sliding, currentSpeed is managed by sliding logic
            // Do not override currentSpeed here
        }
        else if (isCrouching)
        {
            currentSpeed = crouchSpeed;  // Slower movement while crouching
        }
        else
        {
            // Prevent sprinting while crouched by adding !isCrouching
            if (Input.GetKey(KeyCode.LeftShift) && z > 0 && !isCrouching)
            {
                currentSpeed = runSpeed;
            }
            else
            {
                currentSpeed = walkSpeed;
            }
        }

        if (move.magnitude == 0 && !isSliding)
        {
            currentSpeed = 0f;
        }

        Vector3 newPosition = rb.position + move * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);

        // *** Handle walking and sprinting sounds using a single AudioSource ***
        if (currentSpeed > 0 && isGrounded && !IsAiming)
        {
            // Determine the pitch based on speed
            if (currentSpeed == runSpeed)
            {
                walkingAudioSource.pitch = 1.5f; // Higher pitch for sprinting
            }
            else if (currentSpeed == walkSpeed)
            {
                walkingAudioSource.pitch = 1f; // Normal pitch for walking
            }
            else if (currentSpeed == crouchSpeed)
            {
                walkingAudioSource.pitch = 0.8f; // Lower pitch for crouching
            }

            if (!walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Play();
                Debug.Log("Walking/Sprinting sound started.");
            }
        }
        else
        {
            // Stop walking sound when not moving or not grounded
            if (walkingAudioSource.isPlaying)
            {
                walkingAudioSource.Stop();
                walkingAudioSource.pitch = 1f; // Reset pitch to normal
                Debug.Log("Walking/Sprinting sound stopped.");
            }
        }
    }

    void HandleCameraBobbing()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // *** Skip camera bobbing during sliding ***
        if (isSliding)
        {
            // Optionally, reset camera position to slideHeight to ensure stability
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, slideHeight, playerCamera.localPosition.z);
            return;
        }

        if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
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
                // **Perform a jump if grounded, not crouching, and not sliding**
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
                // **Cancel the slide if already sliding**
                CancelSlide();
            }
            else
            {
                if (!isGrounded)
                {
                    StartSlide();
                }
                else
                {
                    isCrouching = true;
                }
            }
        }

        if (isSliding)
        {
            ContinueSlide();
        }
        else if (Input.GetKey(KeyCode.C) && isGrounded)
        {
            // Existing crouch logic (without setting currentSpeed)
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

            isCrouching = true;
        }
        else
        {
            if (!isSliding && !Input.GetKey(KeyCode.C))
            {
                // Revert to standing if not sliding and crouch button is not held
                playerCamera.localPosition = Vector3.Lerp(
                    playerCamera.localPosition,
                    new Vector3(playerCamera.localPosition.x, defaultHeight, playerCamera.localPosition.z),
                    Time.deltaTime * crouchTransitionSpeed
                );
                playerCollider.height = Mathf.Lerp(playerCollider.height, originalColliderHeight, Time.deltaTime * crouchTransitionSpeed);
                playerCollider.center = Vector3.Lerp(playerCollider.center, originalColliderCenter, Time.deltaTime * crouchTransitionSpeed);
                isCrouching = false;
            }
        }
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;

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
        if (slideTimer > 0)
        {
            // *** Calculate the proportion of slide completed ***
            float speedDecayFactor = slideTimer / slideDuration; // From 1 to 0

            // *** Linearly interpolate speed from initialSlideSpeed to crouchSpeed ***
            currentSpeed = Mathf.Lerp(crouchSpeed, initialSlideSpeed, speedDecayFactor);

            slideTimer -= Time.deltaTime;
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
        slideTimer = 0f;

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
