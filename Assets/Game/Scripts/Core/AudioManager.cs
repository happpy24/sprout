using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource SFXSource;
    [SerializeField] private AudioSource heartbeat;

    [Header("Music Clips")]
    public AudioClip mainMenu;
    public AudioClip heartrootBasin;
    public AudioClip vineWoodsIntro;
    public AudioClip vineWoods;
    public AudioClip fountainRestored;
    public AudioClip bossFight;
    public AudioClip hollowgroveRidge;

    [Header("SFX Clips")]
    public AudioClip walking1, walking2, walking3, walking4;
    public AudioClip attack, jump, dash, land, enemyHit, playerHit, itemReceived;

    [Header("Settings")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.5f;
    public float fadeDuration = 2f;

    private string currentScenePrefix = "";
    private AudioSource tempFadeSource;
    private bool hasCompletedVineWoodsIntro = false;
    private bool isFirstVineWoodsScreen = true;
    private Image vineWoodsImage;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            DestroyImmediate(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (musicSource == null)
        {
            Debug.LogError("AudioManager is missing its musicSource!");
            return;
        }

        musicSource.loop = true;
        musicSource.volume = musicVolume;

        // Start music on the first scene load only
        if (currentScenePrefix == "")
        {
            UpdateMusicForCurrentScene();
        }
    }

    private void Update()
    {
        // Check if vineWoodsIntro is playing and handle looping
        if (musicSource.clip == vineWoodsIntro && musicSource.isPlaying)
        {
            // When the intro reaches the end, loop back to halfway point
            if (!musicSource.loop && musicSource.time >= vineWoodsIntro.length - 0.1f)
            {
                musicSource.time = vineWoodsIntro.length / 2f;
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string prefix = GetScenePrefix(scene.name);

        if (prefix == currentScenePrefix)
        {
            // Moving between screens within the same area
            if (prefix == "VW" && isFirstVineWoodsScreen && !hasCompletedVineWoodsIntro)
            {
                isFirstVineWoodsScreen = false;
                hasCompletedVineWoodsIntro = true;
                StartCoroutine(TransitionToVineWoodsLoop());
            }
            return;
        }

        currentScenePrefix = prefix;

        // Reset the VW screen tracker when entering VW area
        if (prefix == "VW")
        {
            isFirstVineWoodsScreen = true;
        }

        StartCoroutine(SceneMusicTransition(prefix));
    }

    private IEnumerator TransitionToVineWoodsLoop()
    {
        // Start fading out the intro music
        StartCoroutine(FadeOutMusic(0.5f));

        // Wait for fade out to complete
        yield return new WaitForSeconds(0.5f);

        // Instantly start the main vineWoods loop at full volume
        musicSource.clip = vineWoods;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();

        // Show the title card
        yield return StartCoroutine(OnSecondVineWoodsScreen());
    }

    private IEnumerator SceneMusicTransition(string prefix)
    {
        // Special handling for VW entry
        if (prefix == "VW")
        {
            if (!hasCompletedVineWoodsIntro)
            {
                // Haven't completed the intro yet - play intro
                yield return StartCoroutine(FadeOutMusic());
                yield return StartCoroutine(FadeInVineWoodsIntro());
            }
            else
            {
                // Already completed intro - normal fade to vineWoods loop
                yield return StartCoroutine(FadeOutMusic());
                yield return StartCoroutine(FadeInMusic("VW"));
            }
        }
        else
        {
            // Normal transition for other scenes
            yield return StartCoroutine(FadeOutMusic());
            yield return StartCoroutine(FadeInMusic(prefix));
        }
    }

    private IEnumerator OnSecondVineWoodsScreen()
    {
        // Cache the image reference if not already cached
        if (vineWoodsImage == null)
        {
            GameObject titleCards = GameObject.FindGameObjectWithTag("TitleCards");
            if (titleCards != null)
            {
                Transform vineWoodsTransform = titleCards.transform.Find("Vinewoods");
                if (vineWoodsTransform != null)
                {
                    vineWoodsImage = vineWoodsTransform.GetComponent<Image>();
                }
            }
        }

        if (vineWoodsImage == null)
        {
            Debug.LogWarning("VineWoods title card image not found!");
            yield break;
        }

        // Ensure we start from invisible
        Color color = vineWoodsImage.color;
        color.a = 0f;
        vineWoodsImage.color = color;

        // Small delay to ensure the color change is applied
        yield return null;

        float elapsed = 0f;

        // Fade in
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(0f, 1f, elapsed / 0.3f);
            vineWoodsImage.color = color;
            yield return null;
        }

        // Ensure fully visible
        color = vineWoodsImage.color;
        color.a = 1f;
        vineWoodsImage.color = color;

        // Hold
        yield return new WaitForSeconds(3f);

        // Fade out
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            color = vineWoodsImage.color;
            color.a = Mathf.Lerp(1f, 0f, elapsed / 0.3f);
            vineWoodsImage.color = color;
            yield return null;
        }

        // Ensure fully invisible
        color = vineWoodsImage.color;
        color.a = 0f;
        vineWoodsImage.color = color;
    }

    private string GetScenePrefix(string sceneName)
    {
        return sceneName.Substring(0, 2);
    }

    private AudioClip GetClipForPrefix(string prefix)
    {
        return prefix switch
        {
            "HB" => heartrootBasin,
            "VW" => vineWoods,
            "HR" => hollowgroveRidge,
            "MM" => mainMenu,
            "BF" => bossFight,
            _ => mainMenu
        };
    }

    private void UpdateMusicForCurrentScene()
    {
        string prefix = GetScenePrefix(SceneManager.GetActiveScene().name);
        currentScenePrefix = prefix;

        // Check if this is VW and intro hasn't been completed yet
        if (prefix == "VW" && !hasCompletedVineWoodsIntro)
        {
            musicSource.clip = vineWoodsIntro;
            musicSource.loop = false; // Don't use standard loop for intro
            musicSource.volume = musicVolume;
            musicSource.Play();
            return;
        }

        AudioClip clip = GetClipForPrefix(prefix);
        if (clip == null) return;

        musicSource.clip = clip;
        musicSource.volume = musicVolume;
        musicSource.Play();
    }

    // ----------------------------------------------------
    // PUBLIC FADE FUNCTIONS
    // ----------------------------------------------------

    public IEnumerator FadeOutMusic(float duration = -1f)
    {
        if (duration < 0) duration = fadeDuration;

        if (musicSource == null || !musicSource.isPlaying)
            yield break;

        if (tempFadeSource != null)
        {
            Destroy(tempFadeSource.gameObject);
        }

        GameObject tempObj = new GameObject("TempFadeSource");
        tempFadeSource = tempObj.AddComponent<AudioSource>();
        tempFadeSource.clip = musicSource.clip;
        tempFadeSource.volume = musicSource.volume;
        tempFadeSource.loop = false;
        tempFadeSource.Play();

        musicSource.Stop();
        musicSource.clip = null;

        float startVol = tempFadeSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (tempFadeSource != null)
            {
                tempFadeSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            }
            yield return null;
        }

        if (tempFadeSource != null)
        {
            tempFadeSource.Stop();
            Destroy(tempFadeSource.gameObject);
            tempFadeSource = null;
        }
    }

    public IEnumerator FadeInMusic()
    {
        string prefix = GetScenePrefix(SceneManager.GetActiveScene().name);
        yield return StartCoroutine(FadeInMusic(prefix));
    }

    public IEnumerator FadeInMusic(string prefix)
    {
        AudioClip newClip = GetClipForPrefix(prefix);
        if (newClip == null)
            yield break;

        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    private IEnumerator FadeInVineWoodsIntro()
    {
        musicSource.clip = vineWoodsIntro;
        musicSource.volume = 0f;
        musicSource.loop = false; // Custom looping handled in Update
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, musicVolume, elapsed / fadeDuration);
            yield return null;
        }

        musicSource.volume = musicVolume;
    }

    // ----------------------------------------------------
    // SFX & Heartbeat (unchanged)
    // ----------------------------------------------------

    public void PlaySFX(AudioClip clip, bool pitchChange = true)
    {
        if (SFXSource == null || clip == null) return;

        SFXSource.volume = sfxVolume;
        if (pitchChange)
            SFXSource.pitch = Random.Range(0.8f, 1.2f);

        SFXSource.PlayOneShot(clip);
    }

    public IEnumerator ActivateHeartbeat()
    {
        float startMusic = musicSource.volume;
        float startSfx = SFXSource.volume;
        float elapsed = 0f;

        heartbeat.Play();

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            musicSource.volume = Mathf.Lerp(startMusic, musicVolume * 0.4f, t);
            SFXSource.volume = Mathf.Lerp(startSfx, sfxVolume * 0.4f, t);
            heartbeat.volume = Mathf.Lerp(0f, musicVolume * 2.5f, t);

            yield return null;
        }
    }

    public IEnumerator DeactivateHeartbeat()
    {
        float startMusic = musicSource.volume;
        float startSfx = SFXSource.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            musicSource.volume = Mathf.Lerp(startMusic, musicVolume, t);
            SFXSource.volume = Mathf.Lerp(startSfx, sfxVolume, t);
            heartbeat.volume = Mathf.Lerp(heartbeat.volume, 0f, t);

            yield return null;
        }

        heartbeat.Stop();
    }
}