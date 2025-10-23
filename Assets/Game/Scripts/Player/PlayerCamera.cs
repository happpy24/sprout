using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Dead Zone Bounds (in world units)")]
    public float boundX = 2f;
    public float boundY = 1.5f;

    [Header("Look Down Settings")]
    public float lookDownDelay = 1f;
    public float lookDownDistance = 2f;
    public float lookDownSpeed = 3f;

    [Header("Camera Bounds")]
    public bool useCameraBounds = false;
    public float leftBound = -10f;
    public float rightBound = 10f;
    public float bottomBound = -10f;
    public float topBound = 10f;

    [Header("Pixel Settings")]
    public float pixelsPerUnit = 16f;

    private Vector3 baseCameraPos;
    private float downHoldTimer = 0f;
    private float currentLookOffset = 0f;
    private float targetLookOffset = 0f;
    private bool lookingDown = false;
    private Camera cam;
    public Transform target;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("PlayerCamera: No Camera component found!");
        }
        
        if (FindObjectsByType<Player>(0).Length == 1)
            target = FindFirstObjectByType<Player>().transform;
    }

    void Update()
    {
        if (!target && (FindObjectsByType<Player>(0).Length == 1))
        {
            target = FindFirstObjectByType<Player>().transform;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        // --- Look Down Logic ---
        bool holdingDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (holdingDown)
        {
            downHoldTimer += Time.deltaTime;
            if (downHoldTimer >= lookDownDelay)
                lookingDown = true;
        }
        else
        {
            downHoldTimer = 0f;
            lookingDown = false;
        }

        float maxAllowedLookDown = -lookDownDistance;

        if (useCameraBounds)
        {
            float halfHeight = 5f;
            float wouldBeY = baseCameraPos.y - lookDownDistance;
            float minAllowedY = bottomBound + halfHeight;

            if (wouldBeY < minAllowedY)
            {
                // Limit look down to stay within bounds
                maxAllowedLookDown = -(baseCameraPos.y - minAllowedY);
            }
        }

        targetLookOffset = lookingDown ? maxAllowedLookDown : 0f;

        // Smooth look offset
        currentLookOffset = Mathf.Lerp(currentLookOffset, targetLookOffset, Time.deltaTime * lookDownSpeed);

        // --- Dead Zone Camera Follow ---
        baseCameraPos = transform.position - new Vector3(0f, currentLookOffset, 0f);

        float minX = baseCameraPos.x - boundX;
        float maxX = baseCameraPos.x + boundX;
        float minY = baseCameraPos.y - boundY;
        float maxY = baseCameraPos.y + boundY;

        Vector2 playerPos = target.position;

        if (playerPos.x < minX)
            baseCameraPos.x = playerPos.x + boundX;
        else if (playerPos.x > maxX)
            baseCameraPos.x = playerPos.x - boundX;

        if (playerPos.y < minY)
            baseCameraPos.y = playerPos.y + boundY;
        else if (playerPos.y > maxY)
            baseCameraPos.y = playerPos.y - boundY;

        // --- Apply Look Offset ---
        Vector3 finalPos = baseCameraPos + new Vector3(0f, currentLookOffset, 0f);

        // --- Apply Camera Bounds ---
        if (useCameraBounds)
        {
            // Calculate camera viewport size
            float halfHeight = 5f;
            float halfWidth = halfHeight * (16f / 9f);

            // Clamp camera center so edges don't go outside bounds
            finalPos.x = Mathf.Clamp(finalPos.x, leftBound + halfWidth, rightBound - halfWidth);
            finalPos.y = Mathf.Clamp(finalPos.y, bottomBound + halfHeight, topBound - halfHeight);
        }

        // --- Apply Pixel Perfect ---
        finalPos.x = Mathf.Round(finalPos.x * pixelsPerUnit) / pixelsPerUnit;
        finalPos.y = Mathf.Round(finalPos.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Draw dead zone
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(boundX * 2, boundY * 2, 0));

        // Draw camera bounds
        if (useCameraBounds)
        {
            Gizmos.color = Color.red;
            Vector3 center = new Vector3((leftBound + rightBound) / 2f, (bottomBound + topBound) / 2f, 0f);
            Vector3 size = new Vector3(rightBound - leftBound, topBound - bottomBound, 0f);
            Gizmos.DrawWireCube(center, size);
        }
    }
#endif
}