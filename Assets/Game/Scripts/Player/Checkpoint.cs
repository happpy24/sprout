using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    public float checkpointID = 0;

    public Vector2 boxSize = new Vector2(1f, 1f);
    public Vector2 boxSizeOffset = new Vector2(0f, 0f);
    public LayerMask playerLayer;

    private TextMeshProUGUI checkpointText;

    private string currentScene;
    private bool hasTriggered;
    private Animator animator;

    void Start()
    {
        checkpointText = GameObject.FindGameObjectWithTag("CheckpointIndicator").GetComponent<TextMeshProUGUI>();
        currentScene = SceneManager.GetActiveScene().name;
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        Collider2D hit = Physics2D.OverlapBox(transform.position + new Vector3(boxSizeOffset.x, boxSizeOffset.y, 0), boxSize, 0f, playerLayer);
        if (hit && !hasTriggered)
        {
            hasTriggered = true;
            StartCoroutine(InitiateCheckpoint());
        }
    }

    private IEnumerator InitiateCheckpoint()
    {
        animator.SetTrigger("Checkpoint");

        Player player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        if (player != null)
        {
            player.checkpointRoom = currentScene;
            player.checkpointID = checkpointID;
        }

        yield return FadeCheckpointText(0f, 1f, 0.5f);
        yield return new WaitForSeconds(2f);
        yield return FadeCheckpointText(1f, 0f, 0.5f);
    }

    private IEnumerator FadeCheckpointText(float startAlpha, float endAlpha, float duration)
    {
        Color color = checkpointText.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            checkpointText.color = color;
            yield return null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(boxSizeOffset.x, boxSizeOffset.y, 0), boxSize);
    }
}
