using UnityEngine;
using UnityEngine.UI;

public class AmmoCrate : MonoBehaviour
{
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.F;
    public Text ammoCratePrompt;
    public AudioClip refillSound;
    public Transform target;
    private AudioSource audioSource;
    private PlayerHealth playerHealth;
    private int ammoCost=2000;
    void Start()
    {
        target = GameObject.FindWithTag("Player")?.transform;
        playerHealth = target.GetComponent<PlayerHealth>();
        if (ammoCratePrompt != null)
            ammoCratePrompt.gameObject.SetActive(false);

        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        Transform playerTransform = Camera.main.transform;
        WeaponController weaponController = playerTransform.GetComponentInChildren<WeaponController>();

        if (weaponController != null && ShouldShowPrompt(weaponController))
        {
            bool isLookingAtCrate = IsLookingAtCrate(playerTransform);
            float distance = Vector3.Distance(transform.position, playerTransform.position);

            if (isLookingAtCrate && distance <= interactionDistance)
            {
                if (ammoCratePrompt != null)
                    ammoCratePrompt.gameObject.SetActive(true);

                
                if (Input.GetKeyDown(interactionKey)&& playerHealth.GetMoney()>=ammoCost)
                {
                    playerHealth.RemoveMoney(ammoCost);
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

    bool IsLookingAtCrate(Transform playerTransform)
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
        weaponController.RefillAmmo();
        PlayRefillSound();
    }

    void PlayRefillSound()
    {
        if (refillSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(refillSound);
        }
    }
}
