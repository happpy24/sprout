using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    [Header("Player Settings")]
    public Transform target;

    [Header("Camera Settings")]
    public float smoothSpeed = 1f;
    public Vector3 offset;

    [Header("Clamp Settings")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = new Vector3(smoothedPosition.x, smoothedPosition.y, transform.position.z);
    }
}
