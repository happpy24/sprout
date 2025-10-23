using UnityEngine;

[DefaultExecutionOrder(100)]
public class ParallaxBackground : MonoBehaviour
{
    [Header("Pixel Perfect Settings")]
    public float pixelsPerUnit = 16f;

    [Header("Parallax Settings")]
    public Transform cameraTransform;

    [Range(0f, 1f)]
    public float parallaxX = 0.5f;

    [Range(0f, 1f)]
    public float parallaxY = 0.5f;

    [Header("Offset")]
    public Vector2 offset = Vector2.zero;

    private Vector3 startCameraPosition;

    void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
                cameraTransform = Camera.main.transform;
            else
                Debug.LogError("ParallaxBackground: No camera found! Please assign a camera.");
        }

        if (cameraTransform != null)
            startCameraPosition = cameraTransform.position;

        transform.position = new Vector3(offset.x, offset.y, transform.position.z);
        SnapToPixelGrid();
    }

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 cameraDelta = cameraTransform.position - startCameraPosition;

        Vector3 targetPosition = new Vector3(
            offset.x + (cameraDelta.x * parallaxX),
            offset.y + (cameraDelta.y * parallaxY),
            transform.position.z
        );

        transform.position = targetPosition;

        SnapToPixelGrid();
    }

    void SnapToPixelGrid()
    {
        Vector3 pos = transform.position;
        float pixelSize = 1f / pixelsPerUnit;

        pos.x = Mathf.Round(pos.x / pixelSize) * pixelSize;
        pos.y = Mathf.Round(pos.y / pixelSize) * pixelSize;

        transform.position = pos;
    }

    public void ResetPosition()
    {
        if (cameraTransform != null)
        {
            startCameraPosition = cameraTransform.position;
        }
    }

    void OnValidate()
    {
        if (Application.isPlaying && cameraTransform != null)
        {
            startCameraPosition = cameraTransform.position;
        }
    }
}