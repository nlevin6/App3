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

    [Header("Physics Settings")]
    public float stickToGroundForce = 10f;

    [Header("Physics Materials")]
    public PhysicMaterial highFrictionMaterial;
    public PhysicMaterial lowFrictionMaterial;

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

    private bool requestedSlide = false;

    private float lastGroundedSpeed = 0f;

    private Vector3 airMoveDirection = Vector3.zero;
    private bool wasGrounded = true;

    private float mouseXInput = 0f;
    private float mouseYInput = 0f;

    private bool isJumping = false;

    private Vector3 groundNormal = Vector3.up; // Added for ground normal tracking

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<CapsuleCollider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        currentSpeed = walkSpeed;
        lastGroundedSpeed = walkSpeed;
        currentBobbingSpeed = walkBobbingSpeed;
        currentBobbingAmount = bobbingAmount;
        defaultCameraYPos = playerCamera.localPosition.y;
        defaultHeight = playerCamera.localPosition.y;
        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;
        originalCameraPosition = playerCamera.localPosition;
        if (walkingAudioSource != null && walkingSound != null)
        {
            walkingAudioSource.clip = walkingSound;
            walkingAudioSource.loop = true;
        }
        if (slideAudioSource != null && slidingSound != null)
        {
            slideAudioSource.clip = slidingSound;
            slideAudioSource.loop = true;
            slideAudioSource.playOnAwake = false;
        }
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.drag = 0f; // Adjusted drag if needed
    }

    void Update()
    {
        HandleMouseLookInput();
        HandleCameraBobbing();
        HandleJump();
        HandleCrouch();
        UpdateGroundCheckPosition();
    }

    void FixedUpdate()
    {
        HandleMovement();
        HandleRotation();
        UpdatePhysicsMaterial(); // Added to switch physics materials
        if (requestedSlide && isGrounded && !isSliding)
        {
            StartSlide();
            requestedSlide = false;
        }
    }

    void LateUpdate()
    {
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleMouseLookInput()
    {
        mouseXInput = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        mouseYInput = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseYInput;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
    }

    void HandleRotation()
    {
        Quaternion deltaRotation = Quaternion.Euler(0f, mouseXInput, 0f);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    void HandleMovement()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 inputMove = transform.right * x + transform.forward * z;
        Vector3 move;
        RaycastHit hitInfo;

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
                // Sliding logic
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

            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hitInfo, groundDistance + 1f, groundMask))
            {
                groundNormal = hitInfo.normal; // Store ground normal
                inputMove = Vector3.ProjectOnPlane(inputMove, groundNormal).normalized;
            }

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

        if (isGrounded && !isJumping)
        {
            rb.AddForce(-groundNormal * stickToGroundForce, ForceMode.Acceleration); // Adjusted force direction
        }

        if (!isGrounded)
        {
            isJumping = false;
        }

        // Zero out horizontal velocity when stationary
        if (isGrounded && inputMove.magnitude == 0f && !isSliding)
        {
            Vector3 velocity = rb.velocity;
            velocity.x = 0f;
            velocity.z = 0f;
            rb.velocity = velocity;
        }

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
        if (isSliding)
        {
            playerCamera.localPosition = new Vector3(playerCamera.localPosition.x, slideHeight, playerCamera.localPosition.z);
            return;
        }
        bool isMoving = false;
        if (isGrounded)
        {
            isMoving = (Input.GetAxis("Horizontal") != 0f || Input.GetAxis("Vertical") != 0f);
        }
        else
        {
            isMoving = airMoveDirection.magnitude > 0.1f;
        }
        if (isMoving)
        {
            float bobbingSpeed;
            float bobbingAmountLocal;
            if (isSliding)
            {
                bobbingSpeed = runBobbingSpeed;
                bobbingAmountLocal = bobbingAmount;
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
                CancelSlide();
            }
            else if (isGrounded && !isCrouching)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJumping = true;
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
            float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
            groundCheck.localPosition = new Vector3(0, colliderBottom, 0);
            isCrouching = true;
        }
        else
        {
            if (!isSliding && !Input.GetKey(KeyCode.C))
            {
                playerCamera.localPosition = Vector3.Lerp(
                    playerCamera.localPosition,
                    new Vector3(playerCamera.localPosition.x, defaultHeight, playerCamera.localPosition.z),
                    Time.deltaTime * crouchTransitionSpeed
                );
                playerCollider.height = Mathf.Lerp(playerCollider.height, originalColliderHeight, Time.deltaTime * crouchTransitionSpeed);
                playerCollider.center = Vector3.Lerp(playerCollider.center, originalColliderCenter, Time.deltaTime * crouchTransitionSpeed);
                float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
                groundCheck.localPosition = new Vector3(0, colliderBottom, 0);
                isCrouching = false;
            }
        }
    }

    void StartSlide()
    {
        if (!isGrounded)
        {
            return;
        }
        if (lastGroundedSpeed <= walkSpeed)
        {
            return;
        }
        isSliding = true;
        slideTimerInternal = slideDuration;
        initialSlideSpeed = walkSpeed * slideSpeedMultiplier;
        currentSpeed = initialSlideSpeed;
        isCrouching = true;
        if (slideAudioSource != null && slidingSound != null)
        {
            slideAudioSource.Play();
        }
        StartCoroutine(SmoothTransition(
            playerCamera.localPosition,
            new Vector3(playerCamera.localPosition.x, slideHeight, playerCamera.localPosition.z),
            slideTransitionSpeed
        ));
        StartCoroutine(AdjustColliderHeight(crouchColliderHeight, slideTransitionSpeed));
    }

    void ContinueSlide()
    {
        if (slideTimerInternal > 0)
        {
            float speedDecayFactor = slideTimerInternal / slideDuration;
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
        currentSpeed = crouchSpeed;
        if (!Input.GetKey(KeyCode.C))
        {
            isCrouching = false;
        }
        if (slideAudioSource != null)
        {
            slideAudioSource.Stop();
        }
        StartCoroutine(SmoothTransition(
            playerCamera.localPosition,
            originalCameraPosition,
            slideTransitionSpeed
        ));
        StartCoroutine(AdjustColliderHeight(originalColliderHeight, slideTransitionSpeed));
    }

    void CancelSlide()
    {
        isSliding = false;
        slideTimerInternal = 0f;
        if (Input.GetKey(KeyCode.LeftShift) && IsGrounded && !isCrouching)
        {
            currentSpeed = runSpeed;
        }
        else
        {
            currentSpeed = walkSpeed;
        }
        if (!Input.GetKey(KeyCode.C))
        {
            isCrouching = false;
        }
        if (slideAudioSource != null)
        {
            slideAudioSource.Stop();
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
        float colliderBottom = playerCollider.center.y - (playerCollider.height / 2f);
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

    void UpdatePhysicsMaterial()
    {
        if (isGrounded && rb.velocity.magnitude < 0.1f && !isSliding)
        {
            playerCollider.material = highFrictionMaterial;
        }
        else
        {
            playerCollider.material = lowFrictionMaterial;
        }
    }
}
