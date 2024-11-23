using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    [Header("Crosshair Elements")]
    public RectTransform centerDot;
    public RectTransform topLine;
    public RectTransform bottomLine;
    public RectTransform leftLine;
    public RectTransform rightLine;

    [Header("Crosshair Settings")]
    public float maxLineDistance = 50f;
    public float bounceSpeed = 10f;
    public float bounceDecay = 5f;

    private float currentBounceDistance = 0f;
    private bool isShooting = false;

    private Vector3 topLineInitialPos;
    private Vector3 bottomLineInitialPos;
    private Vector3 leftLineInitialPos;
    private Vector3 rightLineInitialPos;

    void Start()
    {
        topLineInitialPos = topLine.localPosition;
        bottomLineInitialPos = bottomLine.localPosition;
        leftLineInitialPos = leftLine.localPosition;
        rightLineInitialPos = rightLine.localPosition;
    }

    void Update()
    {
        if (isShooting)
        {
            currentBounceDistance += bounceSpeed * Time.deltaTime;
            currentBounceDistance = Mathf.Clamp(currentBounceDistance, 0f, maxLineDistance);
        }
        else
        {
            currentBounceDistance -= bounceDecay * Time.deltaTime;
            currentBounceDistance = Mathf.Clamp(currentBounceDistance, 0f, maxLineDistance);
        }
        UpdateCrosshairPosition();
    }

    void UpdateCrosshairPosition()
    {
        topLine.localPosition = topLineInitialPos + new Vector3(0, currentBounceDistance, 0);
        bottomLine.localPosition = bottomLineInitialPos + new Vector3(0, -currentBounceDistance, 0);
        leftLine.localPosition = leftLineInitialPos + new Vector3(-currentBounceDistance, 0, 0);
        rightLine.localPosition = rightLineInitialPos + new Vector3(currentBounceDistance, 0, 0);
    }

    public void StartShooting()
    {
        isShooting = true;
    }

    public void StopShooting()
    {
        isShooting = false;
    }
}
