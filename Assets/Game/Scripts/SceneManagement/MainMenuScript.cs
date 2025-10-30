using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuScript : MonoBehaviour
{
    [Header("UI References")]
    public GameObject gameLogo;
    public GameObject spaceToStart;

    [Header("Animation Settings")]
    [Tooltip("How fast the logo sways up and down")]
    public float logoSwaySpeed = 1f;
    [Tooltip("How far the logo moves up/down")]
    public float logoSwayAmount = 10f;
    [Tooltip("Flash interval for 'Press Space' text")]
    public float flashInterval = 1f;

    [Header("Transition Settings")]
    [Tooltip("Target scene to load (HB_4)")]
    public string targetScene = "HB_4";
    [Tooltip("Fade to black duration")]
    public float fadeDuration = 1f;
    [Tooltip("Music fade out duration")]
    public float musicFadeOutDuration = 1.5f;
    [Tooltip("Tag for transition effect")]
    public string transitionEffectTag = "TransitionEffect";

    private Image spaceToStartImage;
    private RectTransform logoRectTransform;
    private Vector3 logoStartPosition;
    private AudioManager audioManager;
    private GameObject transitionEffectObject;
    private Image fadeImage;
    private bool isTransitioning = false;
    private GameObject hudManager;

    void Awake()
    {
        // IMMEDIATELY hide HUD on main menu
        hudManager = GameObject.FindGameObjectWithTag("HudManager");
        if (hudManager != null)
        {
            hudManager.SetActive(false);
        }
    }

    void Start()
    {
        // Get references
        if (gameLogo != null)
        {
            logoRectTransform = gameLogo.GetComponent<RectTransform>();
            if (logoRectTransform != null)
            {
                logoStartPosition = logoRectTransform.anchoredPosition;
            }
        }

        if (spaceToStart != null)
        {
            spaceToStartImage = spaceToStart.GetComponent<Image>();
        }

        // Get AudioManager
        audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogError("MainMenuScript: AudioManager not found!");
        }

        // Get transition effect
        transitionEffectObject = GameObject.FindGameObjectWithTag(transitionEffectTag);
        if (transitionEffectObject != null)
        {
            fadeImage = transitionEffectObject.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                // Ensure we start with no fade
                Color c = fadeImage.color;
                c.a = 0f;
                fadeImage.color = c;
            }
        }

        // Start animations
        StartCoroutine(AnimateLogo());
        StartCoroutine(FlashSpaceToStart());
    }

    void Update()
    {
        // Check for space bar press
        if (Input.GetKeyDown(KeyCode.Space) && !isTransitioning)
        {
            StartCoroutine(StartGame());
        }
    }

    private IEnumerator AnimateLogo()
    {
        if (logoRectTransform == null) yield break;

        float time = 0f;

        while (true)
        {
            time += Time.deltaTime * logoSwaySpeed;

            // Smooth sine wave for sway
            float yOffset = Mathf.Sin(time) * logoSwayAmount;
            Vector3 newPos = logoStartPosition;
            newPos.y += yOffset;

            logoRectTransform.anchoredPosition = newPos;

            yield return null;
        }
    }

    private IEnumerator FlashSpaceToStart()
    {
        if (spaceToStartImage == null) yield break;

        while (true)
        {
            // Fade out
            yield return StartCoroutine(FadeSpaceToStart(1f, 0f, flashInterval / 2f));

            // Fade in
            yield return StartCoroutine(FadeSpaceToStart(0f, 1f, flashInterval / 2f));
        }
    }

    private IEnumerator FadeSpaceToStart(float startAlpha, float endAlpha, float duration)
    {
        if (spaceToStartImage == null) yield break;

        float elapsed = 0f;
        Color color = spaceToStartImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            spaceToStartImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        spaceToStartImage.color = color;
    }

    private IEnumerator StartGame()
    {
        isTransitioning = true;

        // Show HUD for the game
        if (hudManager != null)
        {
            hudManager.SetActive(true);
        }

        // Start fading out music IMMEDIATELY
        if (audioManager != null)
        {
            StartCoroutine(audioManager.FadeOutMusic(musicFadeOutDuration));
        }

        // Fade to black COMPLETELY before loading scene
        if (fadeImage != null)
        {
            float elapsed = 0f;
            Color c = fadeImage.color;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                fadeImage.color = c;
                yield return null;
            }

            c.a = 1f;
            fadeImage.color = c;
        }

        // Wait for music to finish fading if it takes longer
        if (musicFadeOutDuration > fadeDuration)
        {
            yield return new WaitForSeconds(musicFadeOutDuration - fadeDuration);
        }

        // Small extra delay while fully black
        yield return new WaitForSeconds(0.2f);

        // NOW load the scene (screen is already black)
        SceneManager.LoadScene(targetScene, LoadSceneMode.Single);
    }
}
