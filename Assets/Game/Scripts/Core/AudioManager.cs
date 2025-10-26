using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField]
    private AudioSource musicSource;
    [SerializeField]
    private AudioSource SFXSource;

    [Header("Music")]
    public AudioClip mainMenu;
    public AudioClip heartrootBasin;
    public AudioClip vineWoods;
    public AudioClip fountainRestored;
    public AudioClip bossFight;
    public AudioClip hollowgroveRidge;

    [Header("SFX")]
    public AudioClip walking1;
    public AudioClip walking2;
    public AudioClip walking3;
    public AudioClip walking4;
    public AudioClip attack;
    public AudioClip jump;
    public AudioClip dash;
    public AudioClip land;
    public AudioClip enemyHit;
    public AudioClip playerHit;
    public AudioClip itemReceived;

    [Header("Fade Settings")]
    public float fadeDuration = 2f;
    [Range(0f, 1f)]
    public float musicVolume = 0.5f;
    [Range(0f, 1f)]
    public float sfxVolume = 0.5f;

    private string currentScenePrefix = "";
    private Coroutine fadeCoroutine;
    private AudioSource tempFadeSource;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        // Ensure music source has correct settings
        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.volume = musicVolume;
        }
        else
        {
            Debug.LogError("Music Source is not assigned in AudioManager!");
            return;
        }

        // Set initial music based on current scene
        UpdateMusicForCurrentScene();
    }

    void OnDestroy()
    {
        // Unsubscribe from scene loaded event
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateMusicForScene(scene);
    }

    private void UpdateMusicForCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        UpdateMusicForScene(currentScene);
    }

    private void UpdateMusicForScene(Scene scene)
    {
        string sceneName = scene.name;

        string scenePrefix = sceneName.Substring(0, 2);

        // If scene prefix hasn't changed, don't do anything
        if (scenePrefix == currentScenePrefix && musicSource.isPlaying)
        {
            return;
        }

        currentScenePrefix = scenePrefix;

        // Determine which music to play
        AudioClip newClip = GetMusicForScenePrefix(scenePrefix);

        if (newClip == null)
        {
            return;
        }

        if (newClip != musicSource.clip || !musicSource.isPlaying)
        {
            // Stop any existing fade and clean up temp source
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                if (tempFadeSource != null)
                {
                    Destroy(tempFadeSource);
                    tempFadeSource = null;
                }
            }

            // If no music is playing, start immediately without fade
            if (!musicSource.isPlaying)
            {
                musicSource.clip = newClip;
                musicSource.volume = musicVolume;
                musicSource.Play();
            }
            else
            {
                fadeCoroutine = StartCoroutine(CrossfadeMusic(newClip));
            }
        }
    }

    private AudioClip GetMusicForScenePrefix(string prefix)
    {
        switch (prefix)
        {
            case "HB":
                return heartrootBasin;
            case "VW":
                return vineWoods;
            case "HR":
                return hollowgroveRidge;
            case "MM":
                return mainMenu;
            case "FR":
                return fountainRestored;
            case "BF":
                return bossFight;
            default:
                Debug.LogWarning($"Unknown scene prefix: {prefix}");
                return null;
        }
    }

    private IEnumerator CrossfadeMusic(AudioClip newClip)
    {
        // Create a temporary AudioSource for the old music
        tempFadeSource = gameObject.AddComponent<AudioSource>();
        tempFadeSource.clip = musicSource.clip;
        tempFadeSource.volume = musicSource.volume;
        tempFadeSource.loop = false;
        tempFadeSource.Play();

        float startVolume = musicSource.volume;

        // Start new music immediately
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();
        musicSource.loop = true;

        // Crossfade: fade out old, fade in new simultaneously
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;

            tempFadeSource.volume = Mathf.Lerp(startVolume, 0f, t);
            musicSource.volume = Mathf.Lerp(0f, musicVolume, t);

            yield return null;
        }

        // Clean up
        musicSource.volume = musicVolume;
        Destroy(tempFadeSource);
        tempFadeSource = null;
        fadeCoroutine = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        SFXSource.volume = sfxVolume;
        SFXSource.PlayOneShot(clip);
    }
}