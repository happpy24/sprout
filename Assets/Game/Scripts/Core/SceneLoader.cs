using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

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

    // Static tracking for preloaded scenes and VW state
    private static HashSet<string> preloadedScenes = new HashSet<string>();
    private static HashSet<string> activeSceneHistory = new HashSet<string>();
    private static bool hasEnteredVineWoodsSecondScreen = false;
    private static bool isCurrentlyInVineWoods = false;
    private static string currentSceneName = "";

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

        // Track current scene and mark it as preloaded
        if (string.IsNullOrEmpty(currentSceneName))
        {
            currentSceneName = SceneManager.GetActiveScene().name;
            preloadedScenes.Add(currentSceneName); // Add initial scene to prevent duplicates
            activeSceneHistory.Add(currentSceneName);
            isCurrentlyInVineWoods = GetScenePrefix(currentSceneName) == "VW";

            // Start initial music
            StartCoroutine(StartInitialMusic());
        }

        // Only preload if this SceneLoader's GameObject is active
        // (inactive SceneLoaders are in unloaded/inactive scenes and shouldn't preload yet)
        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PreloadSceneInBackground());
        }
    }

    private IEnumerator StartInitialMusic()
    {
        yield return null; // Wait one frame for AudioManager to initialize

        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null)
        {
            string prefix = GetScenePrefix(currentSceneName);
            StartCoroutine(audioManager.FadeInMusicForPrefix(prefix));
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

    private IEnumerator PreloadSceneInBackground()
    {
        // Check if already preloaded or currently being loaded
        if (preloadedScenes.Contains(sceneName))
            yield break;

        // Mark as being loaded to prevent other loaders from starting
        preloadedScenes.Add(sceneName);

        // Use allowSceneActivation = false to load without activating
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;

        // Wait until scene is almost ready (90%)
        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        // Now allow activation
        loadOperation.allowSceneActivation = true;
        yield return loadOperation;

        Scene preloadedScene = SceneManager.GetSceneByName(sceneName);
        if (preloadedScene.isLoaded)
        {
            // Immediately deactivate all root objects to prevent any systems from running
            GameObject[] rootObjects = preloadedScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
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

        // Get prefix info
        string oldSceneName = SceneManager.GetActiveScene().name;
        string oldPrefix = GetScenePrefix(oldSceneName);
        string newPrefix = GetScenePrefix(sceneName);
        bool prefixChanged = oldPrefix != newPrefix;
        bool enteringVWFromVW = oldPrefix == "VW" && newPrefix == "VW";
        bool leavingVW = oldPrefix == "VW" && newPrefix != "VW";
        bool enteringVWFromOther = oldPrefix != "VW" && newPrefix == "VW";

        // Start fade to black (0.25s)
        yield return StartCoroutine(FadeToBlack(0.25f));

        // Start music fade out if needed
        AudioManager audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager != null && prefixChanged)
        {
            // If leaving VW or changing areas, fade out
            if (leavingVW || !enteringVWFromVW)
            {
                StartCoroutine(audioManager.FadeOutMusic(1.5f));
            }
        }

        // Hold on black screen while music fades (1.5s total black time)
        yield return new WaitForSeconds(1.25f);

        // Save player state
        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        Vector2 savedVelocity = Vector2.zero;
        Vector3 savedScale = player.transform.localScale;

        if (playerMovement != null)
        {
            savedVelocity = playerMovement.velocity;
            playerMovement.velocity = Vector2.zero;
        }

        // Load or activate the new scene
        Scene newScene;
        if (preloadedScenes.Contains(sceneName))
        {
            newScene = SceneManager.GetSceneByName(sceneName);

            // Reactivate all objects
            GameObject[] rootObjects = newScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                obj.SetActive(true);
            }
        }
        else
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            yield return loadOperation;
            newScene = SceneManager.GetSceneByName(sceneName);
            preloadedScenes.Add(sceneName);
        }

        SceneManager.SetActiveScene(newScene);

        // Deactivate old scene objects instead of unloading
        Scene oldScene = SceneManager.GetSceneByName(oldSceneName);
        if (oldScene.isLoaded)
        {
            GameObject[] oldRootObjects = oldScene.GetRootGameObjects();
            foreach (GameObject obj in oldRootObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                }
            }
        }

        yield return null;

        // Find and position new player
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

        // Update current scene tracking
        currentSceneName = sceneName;
        activeSceneHistory.Add(sceneName);

        // Clean up old scenes not in any SceneLoader from current scene
        yield return StartCoroutine(CleanupUnusedScenes());

        // Track if we JUST set the VW flag (meaning this is the transition we need to handle)
        bool justSetVWFlag = false;

        // Handle music transitions
        if (audioManager != null)
        {
            if (enteringVWFromVW && !hasEnteredVineWoodsSecondScreen)
            {
                // First time entering second VW screen - set flag
                hasEnteredVineWoodsSecondScreen = true;
                justSetVWFlag = true;
                // Don't start any music yet - will handle after fade in
            }
            else if (prefixChanged)
            {
                // Normal area transition with music change
                if (enteringVWFromOther && !hasEnteredVineWoodsSecondScreen)
                {
                    // Entering VW for first time - play intro
                    StartCoroutine(audioManager.FadeInVineWoodsIntro());
                }
                else
                {
                    // Normal music fade in for new area
                    StartCoroutine(audioManager.FadeInMusicForPrefix(newPrefix));
                }
            }
            // If same prefix and not the VW special case, music continues playing
        }

        // Update VW tracking
        isCurrentlyInVineWoods = newPrefix == "VW";

        // Fade from black (0.25s)
        yield return StartCoroutine(FadeFromBlack(0.25f));

        // NOW handle VineWoods transition after screen is visible
        if (justSetVWFlag && audioManager != null)
        {
            yield return StartCoroutine(HandleVineWoodsIntroToLoop(audioManager));
        }

        Destroy(transitionEffect);
        Destroy(gameObject);
    }

    private IEnumerator CleanupUnusedScenes()
    {
        // Get all SceneLoaders in the current active scene
        SceneLoader[] currentSceneLoaders = FindObjectsByType<SceneLoader>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        HashSet<string> referencedScenes = new HashSet<string>();

        // Add current scene
        referencedScenes.Add(currentSceneName);

        // Add all scenes referenced by SceneLoaders in current scene
        foreach (SceneLoader loader in currentSceneLoaders)
        {
            if (!string.IsNullOrEmpty(loader.sceneName))
            {
                referencedScenes.Add(loader.sceneName);
            }
        }

        // Unload scenes that are not referenced
        List<string> scenesToUnload = new List<string>();

        // Check all loaded scenes
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            string sceneName = scene.name;

            // Skip DontDestroyOnLoad pseudo-scene
            if (string.IsNullOrEmpty(sceneName))
                continue;

            if (!referencedScenes.Contains(sceneName) && sceneName != currentSceneName)
            {
                scenesToUnload.Add(sceneName);
            }
        }

        foreach (string sceneToUnload in scenesToUnload)
        {
            Scene scene = SceneManager.GetSceneByName(sceneToUnload);
            if (scene.isLoaded)
            {
                AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(scene);
                yield return unloadOp;
                preloadedScenes.Remove(sceneToUnload);
                activeSceneHistory.Remove(sceneToUnload);
            }
        }
    }

    private IEnumerator HandleVineWoodsIntroToLoop(AudioManager audioManager)
    {
        // Fade out intro over 1 second
        yield return StartCoroutine(audioManager.FadeOutMusic(1f));

        // Wait briefly
        yield return new WaitForSeconds(0.1f);

        // Start vineWoods loop instantly
        audioManager.PlayVineWoodsLoop();

        // Show title card
        yield return StartCoroutine(ShowVineWoodsTitleCard());
    }

    private IEnumerator ShowVineWoodsTitleCard()
    {
        GameObject titleCardsObj = GameObject.FindGameObjectWithTag("TitleCards");
        if (titleCardsObj == null)
        {
            Debug.LogWarning("TitleCards object not found!");
            yield break;
        }

        Transform vineWoodsTransform = titleCardsObj.transform.Find("Vinewoods");
        if (vineWoodsTransform == null)
        {
            Debug.LogWarning("Vinewoods title card not found!");
            yield break;
        }

        Image vineWoodsImage = vineWoodsTransform.GetComponent<Image>();
        if (vineWoodsImage == null)
        {
            Debug.LogWarning("Vinewoods Image component not found!");
            yield break;
        }

        // Ensure starting from transparent
        Color color = vineWoodsImage.color;
        color.a = 0f;
        vineWoodsImage.color = color;

        yield return null;

        float elapsed = 0f;
        float fadeDuration = 1f;

        // Fade in over 1 second
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            vineWoodsImage.color = color;
            yield return null;
        }

        // Ensure fully visible
        color = vineWoodsImage.color;
        color.a = 1f;
        vineWoodsImage.color = color;

        // Hold for 5 seconds
        yield return new WaitForSeconds(5f);

        // Fade out over 1 second
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            vineWoodsImage.color = color;
            yield return null;
        }

        // Ensure fully transparent
        color = vineWoodsImage.color;
        color.a = 0f;
        vineWoodsImage.color = color;
    }

    private string GetScenePrefix(string sceneName)
    {
        if (sceneName.Length >= 2)
            return sceneName.Substring(0, 2);
        return sceneName;
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