using UnityEngine;
using System.Collections;

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
    [Tooltip("Only snap final position to pixels, keep smooth movement")]
    public bool pixelPerfectFinalPosition = true;

    private Vector3 baseCameraPos;
    private float downHoldTimer = 0f;
    private float currentLookOffset = 0f;
    private float targetLookOffset = 0f;
    private bool lookingDown = false;
    private Camera cam;
    public Transform target;

    // --- Screen Shake ---
    private float shakeDuration = 0f;
    private float shakeAmount = 0f;
    private Vector3 shakeOffset = Vector3.zero;

    // Cached camera height
    private float cameraHalfHeight;
    private float cameraHalfWidth;

    // Cinematic mode (controlled by OpeningSequence)
    public bool cinematicMode = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("PlayerCamera: No Camera component found!");
            return;
        }

        // Calculate camera dimensions
        cameraHalfHeight = cam.orthographicSize;
        cameraHalfWidth = cameraHalfHeight * cam.aspect;

        if (FindObjectsByType<Player>(0).Length == 1)
            target = FindFirstObjectByType<Player>().transform;
    }

    void Update()
    {
        if (!target && (FindObjectsByType<Player>(0).Length == 1))
        {
            target = FindFirstObjectByType<Player>().transform;
        }

        // Handle shake timing
        if (shakeDuration > 0)
        {
            shakeDuration -= Time.unscaledDeltaTime;
            shakeOffset = Random.insideUnitCircle * shakeAmount;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }
    }

    void LateUpdate()
    {
        // Skip normal camera logic if in cinematic mode
        if (cinematicMode || !target) return;

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
            float wouldBeY = baseCameraPos.y - lookDownDistance;
            float minAllowedY = bottomBound + cameraHalfHeight;

            if (wouldBeY < minAllowedY)
            {
                // Limit look down to stay within bounds
                maxAllowedLookDown = -(baseCameraPos.y - minAllowedY);
            }
        }

        targetLookOffset = lookingDown ? maxAllowedLookDown : 0f;

        // Smooth look offset
        currentLookOffset = Mathf.Lerp(currentLookOffset, targetLookOffset, Time.deltaTime * lookDownSpeed);

        // --- Dead Zone Camera Follow (NO SMOOTHING - Instant snap) ---
        Vector2 playerPos = target.position;
        baseCameraPos = transform.position;

        float minX = baseCameraPos.x - boundX;
        float maxX = baseCameraPos.x + boundX;
        float minY = baseCameraPos.y - boundY;
        float maxY = baseCameraPos.y + boundY;

        // Horizontal follow - instant snap when outside dead zone
        if (playerPos.x < minX)
            baseCameraPos.x = playerPos.x + boundX;
        else if (playerPos.x > maxX)
            baseCameraPos.x = playerPos.x - boundX;

        // Vertical follow - instant snap when outside dead zone
        if (playerPos.y < minY)
            baseCameraPos.y = playerPos.y + boundY;
        else if (playerPos.y > maxY)
            baseCameraPos.y = playerPos.y - boundY;

        // --- Apply Look Offset ---
        Vector3 finalPos = baseCameraPos + new Vector3(0f, currentLookOffset, 0f);

        // --- Apply Camera Bounds BEFORE shake ---
        if (useCameraBounds)
        {
            finalPos.x = Mathf.Clamp(finalPos.x, leftBound + cameraHalfWidth, rightBound - cameraHalfWidth);
            finalPos.y = Mathf.Clamp(finalPos.y, bottomBound + cameraHalfHeight, topBound - cameraHalfHeight);
        }

        // --- Apply Shake Offset ---
        finalPos += shakeOffset;

        // --- Apply Pixel Perfect to FINAL position only ---
        if (pixelPerfectFinalPosition)
        {
            finalPos.x = Mathf.Round(finalPos.x * pixelsPerUnit) / pixelsPerUnit;
            finalPos.y = Mathf.Round(finalPos.y * pixelsPerUnit) / pixelsPerUnit;
        }

        transform.position = new Vector3(finalPos.x, finalPos.y, transform.position.z);
    }

    /// <summary>
    /// Public method to trigger camera shake from other scripts.
    /// </summary>
    /// <param name="amount">The strength of the shake.</param>
    /// <param name="duration">How long the shake lasts.</param>
    public void Shake(float amount, float duration)
    {
        shakeAmount = amount;
        shakeDuration = duration;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            // Update camera dimensions in editor
            Camera editorCam = GetComponent<Camera>();
            if (editorCam != null)
            {
                cameraHalfHeight = editorCam.orthographicSize;
                cameraHalfWidth = cameraHalfHeight * editorCam.aspect;
            }
        }

        // Draw dead zone bounds
        Gizmos.color = Color.yellow;
        Vector3 deadZoneCenter = transform.position;
        Vector3 deadZoneSize = new Vector3(boundX * 2, boundY * 2, 0);
        Gizmos.DrawWireCube(deadZoneCenter, deadZoneSize);

        // Draw camera bounds
        if (useCameraBounds)
        {
            Gizmos.color = Color.red;
            Vector3 boundsCenter = new Vector3((leftBound + rightBound) / 2f, (bottomBound + topBound) / 2f, 0f);
            Vector3 boundsSize = new Vector3(rightBound - leftBound, topBound - bottomBound, 0f);
            Gizmos.DrawWireCube(boundsCenter, boundsSize);

            // Draw actual camera view bounds
            Gizmos.color = Color.cyan;
            Vector3 cameraViewCenter = transform.position;
            Vector3 cameraViewSize = new Vector3(cameraHalfWidth * 2, cameraHalfHeight * 2, 0);
            Gizmos.DrawWireCube(cameraViewCenter, cameraViewSize);
        }

        // Draw player position indicator
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(target.position, 0.3f);
        }
    }
}
