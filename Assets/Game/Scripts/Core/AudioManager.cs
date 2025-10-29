using System.Collections;
using UnityEngine;

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

    private AudioSource tempFadeSource;

    private void Start()
    {
        if (musicSource == null)
        {
            Debug.LogError("AudioManager is missing its musicSource!");
            return;
        }

        musicSource.loop = true;
        musicSource.volume = musicVolume;
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

    // ----------------------------------------------------
    // PUBLIC MUSIC CONTROL FUNCTIONS
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
        DontDestroyOnLoad(tempObj);
        tempFadeSource = tempObj.AddComponent<AudioSource>();
        tempFadeSource.clip = musicSource.clip;
        tempFadeSource.volume = musicSource.volume;
        tempFadeSource.time = musicSource.time;
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

    public IEnumerator FadeInMusicForPrefix(string prefix)
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

    public IEnumerator FadeInVineWoodsIntro()
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

    public void PlayVineWoodsLoop()
    {
        musicSource.clip = vineWoods;
        musicSource.volume = musicVolume;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ----------------------------------------------------
    // SFX & Heartbeat
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