using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    public float checkpointID = 0;

    public Vector2 boxSize = new Vector2(1f, 1f);
    public Vector2 boxSizeOffset = new Vector2(0f, 0f);
    public LayerMask playerLayer;

    private string currentScene;
    private bool hasTriggered;
    private Animator animator;

    void Start()
    {
        currentScene = SceneManager.GetActiveScene().name;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position + new Vector3(boxSizeOffset.x, boxSizeOffset.y, 0), boxSize, 0f, playerLayer);
        if (hit && !hasTriggered)
        {
            hasTriggered = true;

            animator.SetTrigger("Checkpoint");

            Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
            if (player != null)
            {
                player.checkpointRoom = currentScene;
                player.checkpointID = checkpointID;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(boxSizeOffset.x, boxSizeOffset.y, 0), boxSize);
    }
}
