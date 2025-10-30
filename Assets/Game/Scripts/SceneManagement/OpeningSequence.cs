using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class OpeningSequence : MonoBehaviour
{
    [Header("Camera Movement")]
    [Tooltip("Starting camera Y offset above player")]
    public float startCameraYOffset = 20.5f;
    [Tooltip("Duration of camera descent")]
    public float cameraMoveTime = 5f;
    [Tooltip("Smoothing curve for camera movement")]
    public AnimationCurve cameraMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Fade Settings")]
    [Tooltip("How long to hold black screen before starting")]
    public float initialBlackDuration = 1f;
    [Tooltip("Fade in duration")]
    public float fadeInDuration = 2f;
    [Tooltip("Music fade in duration")]
    public float musicFadeInDuration = 2f;

    [Header("Tutorial Cards")]
    [Tooltip("Fade in/out duration for tutorial cards")]
    public float cardFadeDuration = 0.5f;
    [Tooltip("Tag for the parent object containing tutorial cards")]
    public string introCardsTag = "IntroCards";
    [Tooltip("Walk card stays visible until player moves away")]
    public float walkCardHideDistance = 3f;

    [Header("References")]
    [Tooltip("Tag for transition effect (black screen)")]
    public string transitionEffectTag = "TransitionEffect";

    private Camera mainCamera;
    private PlayerCamera playerCamera;
    private PlayerMovement playerMovement;
    private Player player;
    private GameObject transitionEffectObject;
    private Image fadeImage;
    private GameObject introCardsParent;
    private AudioManager audioManager;

    // Tutorial card references
    private Image walkCard;
    private Image jumpCard;
    private Image attackCard;

    // Card fade coroutines
    private Coroutine walkCardCoroutine;
    private Coroutine jumpCardCoroutine;
    private Coroutine attackCardCoroutine;

    // Trigger references
    private BoxCollider2D trigger2;
    private BoxCollider2D trigger3;

    private bool sequenceComplete = false;
    private bool walkCardShown = false;
    private Vector3 playerStartPosition;

    // Track if sequence has already run
    private static bool hasSequenceRun = false;

    void Awake()
    {
        // CRITICAL: Ensure HB_4 is added to preloaded scenes when loaded from main menu
        UnityEngine.SceneManagement.Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (currentScene.name == "HB_4" && !SceneController.IsScenePreloadedOrLoading("HB_4"))
        {
            SceneController.SetCurrentSceneName("HB_4");
        }

        // Check if this sequence has already been played
        if (hasSequenceRun)
        {
            SkipSequence();
            return;
        }

        hasSequenceRun = true;

        // Get references
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        playerCamera = mainCamera.GetComponent<PlayerCamera>();
        player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerStartPosition = player.transform.position;
        }

        audioManager = FindFirstObjectByType<AudioManager>();

        // Disable everything
        if (playerCamera != null)
        {
            playerCamera.cinematicMode = true;
        }

        if (playerMovement != null)
        {
            playerMovement.playerDead = true;
            playerMovement.enabled = false;
            playerMovement.velocity = Vector2.zero;
        }

        // Position camera above player
        if (player != null)
        {
            Vector3 playerPos = player.transform.position;
            mainCamera.transform.position = new Vector3(playerPos.x, playerPos.y + startCameraYOffset, mainCamera.transform.position.z);
        }

        // Get transition effect
        transitionEffectObject = GameObject.FindGameObjectWithTag(transitionEffectTag);
        if (transitionEffectObject != null)
        {
            fadeImage = transitionEffectObject.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                SetImageAlpha(fadeImage, 1f);
            }
        }

    }

    void Start()
    {
        if (sequenceComplete) return;
        StartCoroutine(InitializeSequence());
    }

    void Update()
    {
        // Check if player moved away from start position to hide Walk card
        if (sequenceComplete && walkCardShown && player != null)
        {
            float distance = Vector3.Distance(player.transform.position, playerStartPosition);
            if (distance > walkCardHideDistance)
            {
                walkCardShown = false;
                if (walkCard != null)
                {
                    ShowCard(ref walkCardCoroutine, walkCard, false);
                }
            }
        }
    }

    private void SkipSequence()
    {
        sequenceComplete = true;

        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            playerCamera = mainCamera.GetComponent<PlayerCamera>();
        }

        player = FindFirstObjectByType<Player>();
        if (player != null)
        {
            playerMovement = player.GetComponent<PlayerMovement>();
            playerStartPosition = player.transform.position;
        }

        if (playerCamera != null)
        {
            playerCamera.cinematicMode = false;
        }

        if (playerMovement != null)
        {
            playerMovement.playerDead = false;
            playerMovement.enabled = true;
            playerMovement.velocity = Vector2.zero;
        }

        transitionEffectObject = GameObject.FindGameObjectWithTag(transitionEffectTag);
        if (transitionEffectObject != null)
        {
            fadeImage = transitionEffectObject.GetComponentInChildren<Image>();
            if (fadeImage != null)
            {
                SetImageAlpha(fadeImage, 0f);
            }
        }

        StartCoroutine(InitializeTutorialCards());
    }

    private IEnumerator InitializeTutorialCards()
    {
        float waitTime = 0f;
        float maxWaitTime = 2f;
        while (introCardsParent == null && waitTime < maxWaitTime)
        {
            introCardsParent = GameObject.FindGameObjectWithTag(introCardsTag);
            if (introCardsParent != null) break;
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
        }

        if (introCardsParent != null)
        {
            Transform walkTransform = introCardsParent.transform.Find("Walk");
            Transform jumpTransform = introCardsParent.transform.Find("Jump");
            Transform attackTransform = introCardsParent.transform.Find("Attack");

            if (walkTransform != null) walkCard = walkTransform.GetComponent<Image>();
            if (jumpTransform != null) jumpCard = jumpTransform.GetComponent<Image>();
            if (attackTransform != null) attackCard = attackTransform.GetComponent<Image>();

            if (walkCard != null) SetImageAlpha(walkCard, 0f);
            if (jumpCard != null) SetImageAlpha(jumpCard, 0f);
            if (attackCard != null) SetImageAlpha(attackCard, 0f);
        }

        Transform trigger2Transform = transform.Find("Trigger2");
        Transform trigger3Transform = transform.Find("Trigger3");

        if (trigger2Transform != null)
        {
            trigger2 = trigger2Transform.GetComponent<BoxCollider2D>();
        }
        if (trigger3Transform != null)
        {
            trigger3 = trigger3Transform.GetComponent<BoxCollider2D>();
        }
    }

    private IEnumerator InitializeSequence()
    {
        if (playerMovement != null)
        {
            playerMovement.velocity = Vector2.zero;
        }

        // Wait for intro cards
        yield return StartCoroutine(InitializeTutorialCards());

        // Start the opening sequence
        yield return StartCoroutine(PlayOpeningSequence());
    }

    private IEnumerator PlayOpeningSequence()
    {
        // Hold black screen
        yield return new WaitForSeconds(initialBlackDuration);

        // Start fading in music
        if (audioManager != null)
        {
            StartCoroutine(audioManager.FadeInMusicForPrefix("HB"));
        }

        // Calculate positions
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(player.transform.position.x, player.transform.position.y, mainCamera.transform.position.z);

        float elapsed = 0f;

        while (elapsed < cameraMoveTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraMoveTime;
            float curveT = cameraMoveCurve.Evaluate(t);

            // Move camera to player position
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, curveT);

            // Fade in screen
            if (t < (fadeInDuration / cameraMoveTime))
            {
                float fadeT = t / (fadeInDuration / cameraMoveTime);
                if (fadeImage != null)
                {
                    SetImageAlpha(fadeImage, Mathf.Lerp(1f, 0f, fadeT));
                }
            }

            yield return null;
        }

        // Ensure final position
        mainCamera.transform.position = endPos;

        if (fadeImage != null)
        {
            SetImageAlpha(fadeImage, 0f);
        }

        // Disable cinematic mode
        if (playerCamera != null)
        {
            playerCamera.cinematicMode = false;
        }

        // Re-enable player
        if (playerMovement != null)
        {
            playerMovement.velocity = Vector2.zero;
            playerMovement.playerDead = false;
            playerMovement.enabled = true;
        }

        sequenceComplete = true;

        // Show Walk card immediately
        if (walkCard != null)
        {
            walkCardShown = true;
            ShowCard(ref walkCardCoroutine, walkCard, true);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!sequenceComplete) return;
        
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // The player is entering OUR triggers (Trigger2, Trigger3)
        // Check which of OUR child triggers the player's collider is inside
        
        if (trigger2 != null && trigger2.bounds.Intersects(other.bounds))
        {
            if (jumpCard != null)
            {
                ShowCard(ref jumpCardCoroutine, jumpCard, true);
            }
        }
        
        if (trigger3 != null && trigger3.bounds.Intersects(other.bounds))
        {
            if (attackCard != null)
            {
                ShowCard(ref attackCardCoroutine, attackCard, true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!sequenceComplete || !other.CompareTag("Player")) return;

        // Check if player has exited the bounds of our triggers
        
        if (trigger2 != null && !trigger2.bounds.Intersects(other.bounds))
        {
            if (jumpCard != null)
            {
                ShowCard(ref jumpCardCoroutine, jumpCard, false);
            }
        }
        
        if (trigger3 != null && !trigger3.bounds.Intersects(other.bounds))
        {
            if (attackCard != null)
            {
                ShowCard(ref attackCardCoroutine, attackCard, false);
            }
        }
    }

    private void ShowCard(ref Coroutine cardCoroutine, Image cardImage, bool show)
    {
        if (cardImage == null) return;

        if (cardCoroutine != null)
        {
            StopCoroutine(cardCoroutine);
        }

        cardCoroutine = StartCoroutine(FadeCard(cardImage, show));
    }

    private IEnumerator FadeCard(Image cardImage, bool fadeIn)
    {
        float startAlpha = cardImage.color.a;
        float targetAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < cardFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cardFadeDuration;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetImageAlpha(cardImage, alpha);
            yield return null;
        }

        SetImageAlpha(cardImage, targetAlpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }

    // Public method to reset the sequence flag (call this when returning to main menu)
    public static void ResetSequence()
    {
        hasSequenceRun = false;
    }
}
