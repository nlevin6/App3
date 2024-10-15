using UnityEngine;
using System.Collections;

public class WeaponController : MonoBehaviour
{
    [Header("Recoil Pattern Settings")]
    public RecoilPattern recoilPattern;
    private int currentRecoilIndex = 0;

    [Header("Animation and Audio")]
    public Animator animator;
    public AudioSource audioSource;
    public AudioClip reloadingSound;
    public AudioClip shootingSound;

    private PlayerMovement playerMovement;
    private bool isAiming;
    private bool isSprinting;
    private bool isReloading;
    private bool isShooting;
    private float speed;

    [Header("Weapon Sway Settings")]
    public float swayAmount = 0.02f;
    public float maxSwayAmount = 0.06f;
    public float smoothAmount = 6f;
    public float tiltAmount = 5f;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    [Header("Recoil Settings")]
    public float recoilAmount = 0.1f;
    public float maxRecoilAmount = 1f;
    public float sidewaysRecoilAmount = 0.1f;
    public float maxSidewaysRecoilAmount = 0.5f;
    public float maxHorizontalOscillation = 2f;
    public float recoilDecayRate = 2f;
    private float currentRecoilAmount = 0f;

    [Header("Camera Recoil Settings")]
    public float recoilSmoothing = 5f;
    private Vector3 cameraRecoilOffset;

    [Header("Firing Settings")]
    public float fireRate = 10f;
    private float nextFireTime = 0f;

    [Header("Gun Position Settings")]
    public Vector3 loweredGunPosition = new Vector3(0, -0.5f, 0);
    public float loweringSpeed = 5f;
    private Vector3 targetGunPosition;

    [Header("Muzzle Flash Settings")]
    public GameObject muzzleFlashPrefab;
    public Transform muzzleTransform;

    [Header("ADS Muzzle Offset Settings")]
    public Vector3 adsMuzzlePositionOffset = Vector3.zero;
    public Vector3 adsMuzzleRotationOffset = Vector3.zero;

    [Header("Impact Effect Settings")]
    public ImpactInfo[] ImpactElements = new ImpactInfo[0];
    public float BulletDistance = 100f;

    [Header("Raycast Settings")]
    public LayerMask hitLayers;
    private Vector3 initialMuzzlePosition;
    private Quaternion initialMuzzleRotation;

    [Header("Camera Settings")]
    public Transform cameraTransform;

    public CrosshairController crosshairController;

    [Header("Damage Settings")]
    public float bodyDamage = 10f;
    public float headshotMultiplier = 2f;

    [Header("Blood Effect Settings")]
    public GameObject bloodEffectPrefab;

    private Vector3 currentCameraRecoil = Vector3.zero;
    private Vector3 recoilVelocity = Vector3.zero;

    [Header("Reload Settings")]
    public float reloadDuration = 2f;

    void Start()
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        initialMuzzlePosition = muzzleTransform.localPosition;
        initialMuzzleRotation = muzzleTransform.localRotation;

        initialPosition = transform.localPosition;
        initialRotation = transform.localRotation;
        targetGunPosition = initialPosition;

        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform.parent;
            if (cameraTransform == null)
            {
                Debug.LogError("CameraRecoil GameObject not found. Please assign it in the Inspector.");
            }
        }
    }

    void Update()
    {
        HandleMovement();
        HandleAiming();
        HandleShooting();
        HandleReloading();
        ApplyRecoil();
        HandleGunPosition();
        HandleMuzzleTransform();
    }

    void HandleMovement()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 movement = new Vector3(horizontal, 0f, vertical).normalized;

        bool isGrounded = playerMovement.IsGrounded;
        if (isAiming)
        {
            isSprinting = false;
            speed = playerMovement.WalkSpeed;
        }
        else
        {
            if (isGrounded && Input.GetKey(KeyCode.LeftShift) && Input.GetKey(KeyCode.W) && movement.magnitude >= 0.1f)
            {
                isSprinting = true;
                targetGunPosition = loweredGunPosition;
            }
            else
            {
                isSprinting = false;
                targetGunPosition = initialPosition;
            }
            if (movement.magnitude >= 0.1f)
            {
                speed = isSprinting ? playerMovement.RunSpeed : playerMovement.WalkSpeed;
            }
            else
            {
                speed = 0f;
            }
        }

        if (!isGrounded)
        {
            animator.SetFloat("Speed", 0f);
        }
        else
        {
            animator.SetFloat("Speed", speed);
        }
        animator.SetBool("IsSprinting", isSprinting);
    }

    void HandleAiming()
    {
        isAiming = Input.GetMouseButton(1);

        if (isAiming)
        {
            if (playerMovement.IsGrounded)
            {
                isSprinting = false;
                speed = playerMovement.WalkSpeed;
                targetGunPosition = initialPosition;
            }
        }

        playerMovement.IsAiming = isAiming;
        animator.SetBool("IsAiming", isAiming);
    }

    void HandleShooting()
    {
        float timeBetweenShots = 1f / fireRate;
        if (Input.GetMouseButton(0) && Time.time >= nextFireTime && !isReloading)
        {
            PlayShootingSound();
            isShooting = true;
            animator.speed = fireRate / 10f;
            animator.SetBool("IsShooting", true);
            nextFireTime = Time.time + timeBetweenShots;
            FireWeapon();

            if (crosshairController != null)
            {
                crosshairController.StartShooting();
            }

            ApplyShootingRecoil();
        }

        if (Input.GetMouseButtonUp(0))
        {
            isShooting = false;
            animator.SetBool("IsShooting", false);
            animator.speed = 1f;
            currentRecoilIndex = 0;
            Debug.Log("Stopped shooting");

            if (crosshairController != null)
            {
                crosshairController.StopShooting();
            }
        }
    }


    void FireWeapon()
    {
        if (isReloading)
        {
            Debug.Log("Cannot fire while reloading.");
            return;
        }

        if (muzzleFlashPrefab != null && muzzleTransform != null)
        {
            GameObject muzzleFlashInstance = Instantiate(muzzleFlashPrefab, muzzleTransform.position, muzzleTransform.rotation, muzzleTransform);
            muzzleFlashInstance.transform.localPosition = Vector3.zero;
            muzzleFlashInstance.transform.localRotation = Quaternion.identity;
            Destroy(muzzleFlashInstance, 0.5f);
        }

        Camera mainCamera = Camera.main;
        Vector3 direction = mainCamera.transform.forward;

        if (recoilPattern != null &&
            currentRecoilIndex < recoilPattern.verticalRecoil.Length &&
            currentRecoilIndex < recoilPattern.horizontalRecoil.Length)
        {
            float verticalRecoil = recoilPattern.verticalRecoil[currentRecoilIndex];
            float horizontalRecoil = recoilPattern.horizontalRecoil[currentRecoilIndex];

            cameraRecoilOffset += new Vector3(-verticalRecoil, horizontalRecoil, 0f);

            currentRecoilIndex++;

            if (currentRecoilIndex >= recoilPattern.verticalRecoil.Length)
            {
                currentRecoilIndex = 0;
            }
        }
        else
        {
            Debug.LogWarning("RecoilPattern is not set or has no recoil steps.");
        }

        RaycastHit hit;
        if (Physics.Raycast(mainCamera.transform.position, direction, out hit, BulletDistance, hitLayers))
        {
            GameObject impactEffect = GetImpactEffect(hit.transform.gameObject);
            if (impactEffect != null)
            {
                GameObject impactInstance = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(impactInstance, 20f);
            }

            MutantZombie zombie = hit.transform.GetComponent<MutantZombie>();
            if (zombie != null)
            {
                float damage = bodyDamage;

                if (hit.collider.CompareTag("Head"))
                {
                    damage *= headshotMultiplier;
                    Debug.Log("Headshot! Damage: " + damage);
                }
                else if (hit.collider.CompareTag("Body"))
                {
                    Debug.Log("Body shot. Damage: " + damage);
                }

                zombie.TakeDamage(damage);

                if (bloodEffectPrefab != null)
                {
                    InstantiateBloodEffect(hit);
                }
                else
                {
                    Debug.LogWarning("Blood Effect Prefab is not assigned in the Inspector.");
                }
            }
        }
    }
    
    void InstantiateBloodEffect(RaycastHit hit)
    {
        GameObject bloodInstance = Instantiate(bloodEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
    }

    public void PlayShootingSound()
    {
        if (audioSource && shootingSound)
        {
            audioSource.PlayOneShot(shootingSound);
        }
    }

    void HandleReloading()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && !isShooting)
        {
            StartCoroutine(Reload());
        }
    }

    private IEnumerator Reload()
    {
        isReloading = true;
        animator.SetBool("IsReloading", true);
        PlayReloadingSound();
        yield return new WaitForSeconds(reloadDuration);
        isReloading = false;
        animator.SetBool("IsReloading", false);
    }

    public void PlayReloadingSound()
    {
        if (audioSource && reloadingSound)
        {
            audioSource.PlayOneShot(reloadingSound);
        }
    }

    void HandleGunPosition()
    {
        Vector3 verticalPosition = Vector3.Lerp(transform.localPosition, new Vector3(initialPosition.x, targetGunPosition.y, initialPosition.z), Time.deltaTime * loweringSpeed);

        float swayX = -Input.GetAxis("Mouse X") * swayAmount;
        float swayY = -Input.GetAxis("Mouse Y") * swayAmount;

        swayX = Mathf.Clamp(swayX, -maxSwayAmount, maxSwayAmount);
        swayY = Mathf.Clamp(swayY, -maxSwayAmount, maxSwayAmount);

        Vector3 swayPosition = new Vector3(swayX, swayY, 0f);

        transform.localPosition = Vector3.Lerp(verticalPosition, swayPosition + verticalPosition, Time.deltaTime * smoothAmount);

        float horizontalMovement = Input.GetAxis("Horizontal");
        float tiltZ = horizontalMovement * tiltAmount;

        Quaternion targetRotationQuat = Quaternion.Euler(0f, 0f, -tiltZ);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, initialRotation * targetRotationQuat, Time.deltaTime * smoothAmount);
    }

    void HandleMuzzleTransform()
    {
        if (!isShooting)
        {
            if (isAiming)
            {
                muzzleTransform.localPosition = initialMuzzlePosition + adsMuzzlePositionOffset;
                muzzleTransform.localRotation = Quaternion.Euler(initialMuzzleRotation.eulerAngles + adsMuzzleRotationOffset);
            }
            else
            {
                muzzleTransform.localPosition = initialMuzzlePosition;
                muzzleTransform.localRotation = initialMuzzleRotation;
            }
        }
    }

    void ApplyShootingRecoil()
    {
        currentRecoilAmount += recoilAmount * Time.deltaTime;
        currentRecoilAmount = Mathf.Clamp(currentRecoilAmount, 0f, maxRecoilAmount);
        float horizontalOscillation = Mathf.Sin(Time.time * fireRate) * Mathf.Clamp(currentRecoilAmount, 0f, maxRecoilAmount) * 2f;
        horizontalOscillation = Mathf.Clamp(horizontalOscillation, -maxHorizontalOscillation, maxHorizontalOscillation);
        float sidewaysRecoil = Random.Range(-sidewaysRecoilAmount, sidewaysRecoilAmount);
        sidewaysRecoil = Mathf.Clamp(sidewaysRecoil, -maxSidewaysRecoilAmount, maxSidewaysRecoilAmount);
        transform.localRotation = Quaternion.Euler(initialRotation.eulerAngles + new Vector3(-currentRecoilAmount, horizontalOscillation, sidewaysRecoil));
    }

    void ApplyRecoil()
    {
        float horizontalOscillation = Mathf.Sin(Time.time * fireRate) * Mathf.Clamp(currentRecoilAmount, 0f, maxRecoilAmount) * 2f;
        horizontalOscillation = Mathf.Clamp(horizontalOscillation, -maxHorizontalOscillation, maxHorizontalOscillation);

        float sidewaysRecoil = Random.Range(-sidewaysRecoilAmount, sidewaysRecoilAmount);
        sidewaysRecoil = Mathf.Clamp(sidewaysRecoil, -maxSidewaysRecoilAmount, maxSidewaysRecoilAmount);

        if (!isShooting)
        {
            currentRecoilAmount -= recoilDecayRate * Time.deltaTime;
            currentRecoilAmount = Mathf.Clamp(currentRecoilAmount, 0f, maxRecoilAmount);
        }

        Vector3 recoilOffset = new Vector3(-currentRecoilAmount, horizontalOscillation, sidewaysRecoil);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, Quaternion.Euler(initialRotation.eulerAngles + recoilOffset), Time.deltaTime * smoothAmount);

        if (cameraTransform != null)
        {
            currentCameraRecoil = Vector3.SmoothDamp(currentCameraRecoil, cameraRecoilOffset, ref recoilVelocity, 1f / recoilSmoothing);
            cameraTransform.localRotation = Quaternion.Euler(currentCameraRecoil);

            cameraRecoilOffset = Vector3.Lerp(cameraRecoilOffset, Vector3.zero, Time.deltaTime * recoilDecayRate);
        }
    }

    [System.Serializable]
    public class ImpactInfo
    {
        public MaterialType.MaterialTypeEnum MaterialType;
        public GameObject ImpactEffect;
    }

    GameObject GetImpactEffect(GameObject impactedGameObject)
    {
        MaterialType materialTypeComponent = impactedGameObject.GetComponent<MaterialType>();
        if (materialTypeComponent == null)
            return null;

        foreach (var impactInfo in ImpactElements)
        {
            if (impactInfo.MaterialType == materialTypeComponent.TypeOfMaterial)
                return impactInfo.ImpactEffect;
        }

        return null;
    }
}
