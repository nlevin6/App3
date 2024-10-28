using UnityEngine;

public class ScopedWeapon : MonoBehaviour
{
    [Header("ADS Settings")]
    public float scopedFOV = 30f;
    public float aimTransitionSpeed = 8f;
    public Camera playerCamera;
    public GameObject scopedCrosshair;
    public GameObject regularCrosshair;
    public bool isAiming = false;
    public bool isReloading = false;
    public float adsSensitivityMultiplier = 0.5f;

    private float defaultFOV;
    private float targetFOV;

    private void Start()
    {
        defaultFOV = playerCamera.fieldOfView;
        targetFOV = defaultFOV;
        scopedCrosshair.SetActive(false);
        regularCrosshair.SetActive(true);
    }

    private void Update()
    {
        HandleADS();
        AdjustFOV();
    }

    private void HandleADS()
    {
        if (Input.GetMouseButton(1) && !isReloading)
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }

        if (isAiming)
        {
            targetFOV = scopedFOV;
            scopedCrosshair.SetActive(true);
            regularCrosshair.SetActive(false);
        }
        else
        {
            targetFOV = defaultFOV;
            scopedCrosshair.SetActive(false);
            regularCrosshair.SetActive(true);
        }
    }

    private void AdjustFOV()
    {
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * aimTransitionSpeed);
    }
}
