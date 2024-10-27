using UnityEngine;
using UnityEngine.UI;

public class AmmoCrate : MonoBehaviour
{
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.F;
    public Text ammoCratePrompt;
    public AudioClip refillSound;

    private Transform playerTransform;
    private AudioSource audioSource;

    void Start()
    {
        playerTransform = Camera.main.transform;
        if (ammoCratePrompt != null)
            ammoCratePrompt.gameObject.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        WeaponController weaponController = playerTransform.GetComponentInChildren<WeaponController>();

        if (weaponController != null && ShouldShowPrompt(weaponController))
        {
            bool isLookingAtCrate = IsLookingAtCrate();
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (isLookingAtCrate && distance <= interactionDistance)
            {
                if (ammoCratePrompt != null)
                    ammoCratePrompt.gameObject.SetActive(true);

                if (Input.GetKeyDown(interactionKey))
                {
                    RefillPlayerAmmo(weaponController);
                }
            }
            else
            {
                if (ammoCratePrompt != null)
                    ammoCratePrompt.gameObject.SetActive(false);
            }
        }
        else
        {
            if (ammoCratePrompt != null)
                ammoCratePrompt.gameObject.SetActive(false);
        }
    }

    bool IsLookingAtCrate()
    {
        Ray ray = new Ray(playerTransform.position, playerTransform.forward);
        RaycastHit hit;
        return Physics.Raycast(ray, out hit, interactionDistance) && hit.transform == transform;
    }

    bool ShouldShowPrompt(WeaponController weaponController)
    {
        return weaponController.GetGunMagCapacity() > weaponController.magCapacity || weaponController.GetReloadBulletAmount() > weaponController.bulletAmount;
    }

    void RefillPlayerAmmo(WeaponController weaponController)
    {
        if(weaponController.GetGunMagCapacity() > weaponController.magCapacity || weaponController.GetReloadBulletAmount() > weaponController.bulletAmount)
        {
            weaponController.RefillAmmo();
            PlayRefillSound();
        }

    }

    void PlayRefillSound()
    {
        if (refillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(refillSound);
        }
    }
}
