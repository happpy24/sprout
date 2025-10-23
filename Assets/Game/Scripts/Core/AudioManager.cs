using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.Layouts;
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

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        musicSource.Play();
        musicSource.loop = true;
    }

    private void Update()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name.Substring(0,2) == "HB")
        {
            musicSource.clip = heartrootBasin;
        }
    }
}
