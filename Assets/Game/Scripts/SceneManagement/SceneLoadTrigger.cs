using UnityEngine;

/// <summary>
/// Scene-specific component that acts as a trigger to initiate a scene change.
/// This script contains the collision logic and passes required data to the SceneController.
/// </summary>
public class SceneLoadTrigger : MonoBehaviour
{
    [Header("Scene Link Configuration")]
    [Tooltip("The direction the player exits the current scene.")]
    public SceneController.ExitAngle exitAngle;

    [Tooltip("The name of the scene to load or activate.")]
    public string sceneName;
    [Tooltip("The ID of the SceneLoadTrigger in the target scene where the player will spawn (must match).")]
    public int linkID;

    private GameObject player;
    private BoxCollider2D boxCollider;
    private bool isTransitioning = false;

    private void OnEnable()
    {
        // Reset state when re-enabled
        isTransitioning = false;

        // Find player if not cached
        if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("SceneLoadTrigger: Player object not found with tag 'Player'.");
            }
        }
    }

    private void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError("SceneLoadTrigger requires a BoxCollider2D component!");
        }
    }

    private void Update()
    {
        if (!isTransitioning && player != null && boxCollider != null && isActiveAndEnabled)
        {
            BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
            if (playerCollider != null)
            {
                if (boxCollider.bounds.Intersects(playerCollider.bounds))
                {
                    SceneController sceneController = FindFirstObjectByType<SceneController>();
                    if (sceneController != null)
                    {
                        isTransitioning = true;
                        gameObject.SetActive(false);

                        sceneController.StartSceneTransition(sceneName, linkID, exitAngle, player, this);
                    }
                    else
                    {
                        Debug.LogError("SceneController not found! Cannot start scene transition.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Public method to reset the trigger state (called by SceneController when re-enabling)
    /// </summary>
    public void ResetTrigger()
    {
        isTransitioning = false;
    }
}