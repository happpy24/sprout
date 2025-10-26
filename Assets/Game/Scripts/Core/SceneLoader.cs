using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public enum ExitAngle
    {
        None,
        Left,
        Right,
        Upwards,
        Downwards,
    }

    [Header("Setup")]
    public ExitAngle exitAngle;
    public string sceneName;
    public int linkID;
    public GameObject player;

    [Header("Transition Effect for Fade")]
    public GameObject transitionEffect;

    private bool isTransitioning = false;
    private Image fadeImage;
    private BoxCollider2D boxCollider;

    private void Start()
    {
        if (transitionEffect != null)
        {
            fadeImage = transitionEffect.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
            }
        }

        boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            Debug.LogError("SceneLoader requires a BoxCollider2D component!");
        }

        if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }
    }

    private void Update()
    {
        if (!isTransitioning && player != null && boxCollider != null)
        {
            BoxCollider2D playerCollider = player.GetComponent<BoxCollider2D>();
            if (playerCollider != null)
            {
                if (boxCollider.bounds.Intersects(playerCollider.bounds))
                {
                    isTransitioning = true;
                    StartCoroutine(TransitionToScene());
                }
            }
        }
    }

    private IEnumerator TransitionToScene()
    {
        DontDestroyOnLoad(transitionEffect);
        DontDestroyOnLoad(gameObject);

        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            Destroy(eventSystem.gameObject);
        }

        yield return StartCoroutine(FadeToBlack(0.25f));

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        Vector2 savedVelocity = Vector2.zero;
        Vector3 savedScale = player.transform.localScale;

        if (playerMovement != null)
        {
            savedVelocity = playerMovement.velocity;
            playerMovement.velocity = Vector2.zero;
        }

        string oldSceneName = SceneManager.GetActiveScene().name;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        yield return loadOperation;

        Scene newScene = SceneManager.GetSceneByName(sceneName);
        SceneManager.SetActiveScene(newScene);

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(oldSceneName);
        yield return unloadOperation;

        yield return null;

        Player findNewPlayer = FindFirstObjectByType<Player>(0);
        GameObject newPlayer = findNewPlayer.gameObject;

        if (newPlayer != null)
        {
            SceneLoader[] sceneLoaders = FindObjectsByType<SceneLoader>(0);
            SceneLoader targetLoader = null;

            foreach (SceneLoader loader in sceneLoaders)
            {
                if (loader.linkID == linkID && loader != this)
                {
                    targetLoader = loader;
                    break;
                }
            }

            if (targetLoader != null)
            {
                Vector3 newPosition = targetLoader.transform.position;

                switch (exitAngle)
                {
                    case ExitAngle.Left:
                        newPosition.x -= 1f;
                        newPosition.y -= 1f;
                        break;
                    case ExitAngle.Right:
                        newPosition.x += 1f;
                        newPosition.y -= 1;
                        break;
                    case ExitAngle.Upwards:
                        newPosition.y += 1f;
                        break;
                    case ExitAngle.Downwards:
                        newPosition.y -= 1f;
                        break;
                }

                newPlayer.transform.position = newPosition;
            }

            newPlayer.transform.localScale = savedScale;

            PlayerMovement newPlayerMovement = newPlayer.GetComponent<PlayerMovement>();
            if (newPlayerMovement != null)
            {
                newPlayerMovement.velocity = savedVelocity;
            }
        }

        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(FadeFromBlack(0.25f));

        Destroy(transitionEffect);
        Destroy(gameObject);
    }

    private IEnumerator FadeToBlack(float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        if (fadeImage == null) yield break;

        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, elapsed / duration);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }
}