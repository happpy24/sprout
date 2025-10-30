using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Persistent component responsible for handling all scene loading, unloading,
/// transition effects, and managing global scene-related state.
/// </summary>
public class SceneController : MonoBehaviour
{
    public enum ExitAngle
    {
        None,
        Left,
        Right,
        Upwards,
        Downwards,
    }

    [Header("Configuration")]
    [Tooltip("The tag of the GameObject that holds the TransitionEffect UI (e.g., a Canvas with a black Image).")]
    public string transitionEffectTag = "TransitionEffect";
    [Tooltip("The total duration (in seconds) the screen is held black during transition.")]
    public float blackScreenHoldDuration = 2f;
    [Tooltip("The duration (in seconds) of the fade to/from black.")]
    public float fadeDuration = 0.25f;

    public static HashSet<string> preloadedScenes = new HashSet<string>();
    private static bool hasEnteredVineWoodsSecondScreen = false;
    private static string currentSceneName = "";
    private static bool isTransitioning = false;
    private static bool isTitleCardPlaying = false;

    private GameObject transitionEffectObject;
    private Image fadeImage;
    private AudioManager audioManager;

    public static bool IsScenePreloadedOrLoading(string sceneName)
    {
        return preloadedScenes.Contains(sceneName);
    }

    void Awake()
    {
        transitionEffectObject = GameObject.FindGameObjectWithTag(transitionEffectTag);
        if (transitionEffectObject == null)
        {
            Debug.LogError($"SceneController: Transition Effect GameObject with tag '{transitionEffectTag}' not found! Transitions will not fade.");
        }
        else
        {
            fadeImage = transitionEffectObject.GetComponentInChildren<Image>();

            if (fadeImage != null)
            {
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
            }
        }
    }

    void Start()
    {
        audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("SceneController: AudioManager not found! Music transitions will fail.");
        }

        if (string.IsNullOrEmpty(currentSceneName))
        {
            Scene activeScene = SceneManager.GetActiveScene();
            currentSceneName = activeScene.name;
            
            // IMPORTANT: Only add game scenes to preloaded scenes, not main menu
            // Main menu is loaded with LoadSceneMode.Single and doesn't need preloading
            if (!currentSceneName.StartsWith("MM"))
            {
                if (!preloadedScenes.Contains(currentSceneName))
                {
                    preloadedScenes.Add(currentSceneName);
                }
            }

            StartCoroutine(InitializeSceneTriggers(activeScene));
            
            // Don't start music here - let OpeningSequence handle it for HB scenes
            if (!currentSceneName.StartsWith("HB"))
            {
                StartCoroutine(StartInitialMusic());
            }
        }
    }

    public void StartSceneTransition(string targetSceneName, int linkID, ExitAngle exitAngle, GameObject playerObject, SceneLoadTrigger triggeringLoader)
    {
        if (isTransitioning)
        {
            if (triggeringLoader != null) triggeringLoader.gameObject.SetActive(false);
            return;
        }

        isTransitioning = true;

        if (!preloadedScenes.Contains(targetSceneName))
        {
            StartCoroutine(PreloadSceneInBackground(targetSceneName));
        }

        StartCoroutine(TransitionToScene(targetSceneName, linkID, exitAngle, playerObject, triggeringLoader));
    }

    private IEnumerator StartInitialMusic()
    {
        yield return null;
        if (audioManager != null)
        {
            string prefix = GetScenePrefix(currentSceneName);
            
            // If starting in VW, play the intro
            if (prefix == "VW")
            {
                StartCoroutine(audioManager.FadeInVineWoodsIntro());
            }
            else
            {
                StartCoroutine(audioManager.FadeInMusicForPrefix(prefix));
            }
        }
    }

    /// <summary>
    /// Loads the target scene additively in the background.
    /// EssentialObjectsLoader in the new scene will handle checking for existing EssentialObjects.
    /// </summary>
    public IEnumerator PreloadSceneInBackground(string sceneName)
    {
        if (preloadedScenes.Contains(sceneName) || string.IsNullOrEmpty(sceneName))
            yield break;

        preloadedScenes.Add(sceneName);

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        loadOperation.allowSceneActivation = false;

        // Wait until loading is almost complete
        while (loadOperation.progress < 0.9f)
        {
            yield return null;
        }

        // Allow activation
        loadOperation.allowSceneActivation = true;

        // Wait for completion
        yield return loadOperation;

        // Deactivate the preloaded scene immediately
        Scene preloadedScene = SceneManager.GetSceneByName(sceneName);
        if (preloadedScene.isLoaded && preloadedScene.name != SceneManager.GetActiveScene().name)
        {
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

    /// <summary>
    /// Finds and re-enables all SceneLoadTriggers in a scene, then preloads their targets.
    /// Only activates triggers if the scene is currently active.
    /// </summary>
    private IEnumerator InitializeSceneTriggers(Scene scene)
    {
        if (!scene.isLoaded) yield break;

        List<SceneLoadTrigger> sceneTriggers = new List<SceneLoadTrigger>();

        GameObject[] rootObjects = scene.GetRootGameObjects();
        foreach (GameObject rootObj in rootObjects)
        {
            sceneTriggers.AddRange(rootObj.GetComponentsInChildren<SceneLoadTrigger>(true));
        }

        // Check if this scene should have active triggers (i.e., is it the active scene or should remain inactive?)
        bool isActiveScene = scene == SceneManager.GetActiveScene();

        foreach (SceneLoadTrigger loader in sceneTriggers)
        {
            // Always reset the trigger state
            loader.ResetTrigger();
            
            // Only activate the trigger if the scene is active
            // If the scene is inactive, leave the triggers inactive too
            if (isActiveScene)
            {
                loader.gameObject.SetActive(true);
            }

            // Preload adjacent scene
            if (!SceneController.IsScenePreloadedOrLoading(loader.sceneName))
            {
                StartCoroutine(PreloadSceneInBackground(loader.sceneName));
            }
        }
        yield return null;
    }

    private IEnumerator TransitionToScene(string targetSceneName, int linkID, ExitAngle exitAngle, GameObject player, SceneLoadTrigger triggeringLoader)
    {
        string oldSceneName = currentSceneName;
        string oldPrefix = GetScenePrefix(oldSceneName);
        string newPrefix = GetScenePrefix(targetSceneName);

        bool prefixChanged = oldPrefix != newPrefix;
        bool isEnteringVW = newPrefix == "VW";
        bool isLeavingVW = oldPrefix == "VW";
        bool isVWtoVW = oldPrefix == "VW" && newPrefix == "VW";
        bool isFirstVWtoVWTransition = isVWtoVW && !hasEnteredVineWoodsSecondScreen;

        // --- 1. Save velocity and disable player input (keep player moving) ---

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        Vector2 savedVelocity = Vector2.zero;
        bool wasPlayerMovementEnabled = false;

        if (playerMovement != null)
        {
            savedVelocity = playerMovement.velocity;
            wasPlayerMovementEnabled = playerMovement.enabled;
            
            // Disable input handling but DON'T disable the component yet
            // This allows FixedUpdate to continue applying the saved velocity
            playerMovement.playerDead = true; // Stops input processing
        }

        // Start fade to black (player continues moving during this with saved velocity)
        yield return StartCoroutine(FadeToBlack(fadeDuration));

        // NOW fully disable player movement
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
            playerMovement.velocity = Vector2.zero;
        }

        // --- 2. Fade out music ONLY if prefix changes (during black screen hold) ---

        if (audioManager != null && prefixChanged)
        {
            StartCoroutine(audioManager.FadeOutMusic(1.5f));
        }

        yield return new WaitForSeconds(blackScreenHoldDuration - fadeDuration);

        if (isTitleCardPlaying)
        {
            StopCoroutine("ShowVineWoodsTitleCard");
            ResetTitleCardOpacity();
            isTitleCardPlaying = false;
        }

        // --- 3. Scene swap ---

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

        // Activate the new scene
        Scene newScene = SceneManager.GetSceneByName(targetSceneName);
        GameObject[] rootObjects = newScene.GetRootGameObjects();
        foreach (GameObject obj in rootObjects)
        {
            obj.SetActive(true);
        }

        SceneManager.SetActiveScene(newScene);
        currentSceneName = targetSceneName;
        yield return null;

        // Initialize triggers in the NEW scene only (it's now active)
        yield return StartCoroutine(InitializeSceneTriggers(newScene));

        // Reset triggers in the OLD scene but keep them INACTIVE
        if (oldScene.isLoaded)
        {
            yield return StartCoroutine(InitializeSceneTriggers(oldScene));
        }

        // --- 4. Reposition player ---

        if (player != null)
        {
            SceneLoadTrigger targetLoader = FindObjectsByType<SceneLoadTrigger>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                                      .FirstOrDefault(l => l.gameObject.scene == newScene && l.linkID == linkID);

            if (targetLoader != null)
            {
                Vector3 newPosition = targetLoader.transform.position + GetEntryOffset(exitAngle);
                player.transform.position = newPosition;
            }

            if (playerMovement != null)
            {
                playerMovement.velocity = Vector2.zero;
            }
        }

        StartCoroutine(CleanupUnusedScenes());

        // --- 5. Handle music based on prefix change and VW logic ---

        if (audioManager != null)
        {
            if (prefixChanged)
            {
                // Entering a new area
                if (isEnteringVW)
                {
                    // Entering VW from outside: play intro (first time) or loop (after sequence)
                    if (hasEnteredVineWoodsSecondScreen)
                    {
                        // Already did the sequence, just play the loop
                        StartCoroutine(audioManager.FadeInMusicForPrefix(newPrefix));
                    }
                    else
                    {
                        // First time entering VW, play intro
                        StartCoroutine(audioManager.FadeInVineWoodsIntro());
                    }
                }
                else
                {
                    // Entering any other area
                    StartCoroutine(audioManager.FadeInMusicForPrefix(newPrefix));
                }
            }
            // else: same prefix, music continues playing
        }

        // Fade from black
        yield return StartCoroutine(FadeFromBlack(fadeDuration));

        // Re-enable player
        if (playerMovement != null)
        {
            playerMovement.playerDead = false; // Re-enable input
            if (wasPlayerMovementEnabled)
            {
                playerMovement.enabled = true;
            }
        }

        // Mark transition as complete BEFORE title card
        isTransitioning = false;

        // --- 6. VW-to-VW special transition (AFTER fade in AND after transition is complete) ---

        if (isFirstVWtoVWTransition && audioManager != null)
        {
            hasEnteredVineWoodsSecondScreen = true;

            // Fade out the intro over 2 seconds
            yield return StartCoroutine(audioManager.FadeOutVineWoodsIntro(1f));
            
            // 2 seconds of silence
            yield return new WaitForSeconds(1f);
            
            // Instantly start the loop (no fade)
            audioManager.PlayVineWoodsLoop();
            
            // Show title card (use StartCoroutine with string name so it can be stopped individually)
            yield return StartCoroutine("ShowVineWoodsTitleCard");
        }
    }

    private Vector3 GetEntryOffset(ExitAngle exitAngle)
    {
        const float Offset = 1.0f;
        switch (exitAngle)
        {
            case ExitAngle.Left: return new Vector3(-Offset, -Offset, 0);
            case ExitAngle.Right: return new Vector3(Offset, -Offset, 0);
            case ExitAngle.Upwards: return new Vector3(0, Offset, 0);
            case ExitAngle.Downwards: return new Vector3(0, -Offset, 0);
            default: return Vector3.zero;
        }
    }

    private IEnumerator CleanupUnusedScenes()
    {
        SceneLoadTrigger[] currentSceneLoaders = FindObjectsByType<SceneLoadTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        HashSet<string> referencedScenes = new HashSet<string>();

        referencedScenes.Add(currentSceneName);

        foreach (SceneLoadTrigger loader in currentSceneLoaders)
        {
            if (loader.gameObject.scene.name == currentSceneName && !string.IsNullOrEmpty(loader.sceneName))
            {
                referencedScenes.Add(loader.sceneName);
            }
        }

        List<string> scenesToUnload = new List<string>();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            string sceneName = scene.name;

            if (string.IsNullOrEmpty(sceneName)) continue;

            if (scene.isLoaded && !referencedScenes.Contains(sceneName))
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
            }
        }
    }

    private IEnumerator ShowVineWoodsTitleCard()
    {
        isTitleCardPlaying = true;

        GameObject titleCardsObj = GameObject.FindGameObjectWithTag("TitleCards");
        if (titleCardsObj == null)
        {
            isTitleCardPlaying = false;
            yield break;
        }

        Transform vineWoodsTransform = titleCardsObj.transform.Find("Vinewoods");
        if (vineWoodsTransform == null)
        {
            isTitleCardPlaying = false;
            yield break;
        }

        vineWoodsTransform.gameObject.SetActive(true);
        Image vineWoodsImage = vineWoodsTransform.GetComponent<Image>();

        if (vineWoodsImage == null)
        {
            isTitleCardPlaying = false;
            yield break;
        }

        Color color = vineWoodsImage.color;
        color.a = 0f;
        vineWoodsImage.color = color;
        yield return null;

        float elapsed = 0f;
        float fadeInDuration = 2f;
        float holdDuration = 3f;
        float fadeOutDuration = 2f;

        // Fade in
        while (elapsed < fadeInDuration)
        {
            if (!isTitleCardPlaying) { yield break; }
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            vineWoodsImage.color = color;
            yield return null;
        }

        color = vineWoodsImage.color;
        color.a = 1f;
        vineWoodsImage.color = color;

        // Hold
        float holdElapsed = 0f;
        while (holdElapsed < holdDuration)
        {
            if (!isTitleCardPlaying) { yield break; }
            holdElapsed += Time.deltaTime;
            yield return null;
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            if (!isTitleCardPlaying) { yield break; }
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            vineWoodsImage.color = color;
            yield return null;
        }

        ResetTitleCardOpacity();
        isTitleCardPlaying = false;
    }

    private void ResetTitleCardOpacity()
    {
        GameObject titleCardsObj = GameObject.FindGameObjectWithTag("TitleCards");
        if (titleCardsObj != null)
        {
            Transform vineWoodsTransform = titleCardsObj.transform.Find("Vinewoods");
            if (vineWoodsTransform != null)
            {
                Image vineWoodsImage = vineWoodsTransform.GetComponent<Image>();
                if (vineWoodsImage != null)
                {
                    Color color = vineWoodsImage.color;
                    color.a = 0f;
                    vineWoodsImage.color = color;
                }
                vineWoodsTransform.gameObject.SetActive(false);
            }
        }
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

    /// <summary>
    /// Clears the preloaded scenes cache. Used when doing a full scene reset (e.g., player death).
    /// </summary>
    public static void ClearPreloadedScenes()
    {
        preloadedScenes.Clear();
    }

    /// <summary>
    /// Updates the current scene name. Used when manually changing scenes outside of normal transitions.
    /// </summary>
    public static void SetCurrentSceneName(string sceneName)
    {
        currentSceneName = sceneName;
        
        // Also add it to preloaded scenes if it's not already there
        if (!string.IsNullOrEmpty(sceneName) && !preloadedScenes.Contains(sceneName))
        {
            preloadedScenes.Add(sceneName);
        }
    }
}
