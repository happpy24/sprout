using System.Collections;
using TMPro;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    // Main Attributes
    public int maxHp = 5;
    public int hp = 5;
    public int hpRestoreProgress = 0;
    public int damage = 20;

    public bool doDamage = false;
    public bool giveHpRstore = false;

    private bool playerDeathActivate = true;

    public string checkpointRoom = "HB_1";
    public float checkpointID = 0;

    // Audio Manager
    private AudioManager audioManager;

    // Animator
    Animator animator;

    // Hud Manager
    private HudManager hudManager;

    // Death Screen
    private GameObject deathScreen;

    void Start()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        animator = GetComponent<Animator>();
        hudManager = GameObject.FindGameObjectWithTag("HudManager").GetComponent<HudManager>();
        deathScreen = GameObject.FindGameObjectWithTag("DeathScreen");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (Input.GetKeyDown(KeyCode.S))
            {

            }
            Attack();
        }

        if (hpRestoreProgress >= 5 && hp < maxHp)
        {
            hpRestoreProgress = 0;
            hp++;
            hudManager.EmptyRefillBar();
            hudManager.UpdateHealthUI(hp);
        }

        if (doDamage)
        {
            doDamage = false;
            hp--;
            animator.SetTrigger("Hit");
            hudManager.UpdateHealthUI(hp);
        }

        if (giveHpRstore)
        {
            giveHpRstore = false;
            GiveHPRestore();
        }

        if (hp <= 0 && playerDeathActivate)
        {
            playerDeathActivate = false;
            StartCoroutine(PlayerDeath());
        }
    }

    private void Attack()
    {

    }

    private void GiveHPRestore()
    {
        if (hpRestoreProgress < 5)
        {
            hudManager.FillRefillBar(hpRestoreProgress);
            hpRestoreProgress++;
        }
    }

    IEnumerator PlayerDeath()
    {
        PlayerMovement playerMovement = gameObject.GetComponent<PlayerMovement>();
        playerMovement.playerDead = true;

        animator.SetTrigger("Death");

        yield return new WaitForSeconds(1f);

        Color c1 = deathScreen.GetComponent<Image>().color;

        float elapsed = 0f;
        
        while (elapsed < 1.0f)
        {
            elapsed += Time.deltaTime;
            c1.a = Mathf.Lerp(0f, 0.7f, elapsed / 1.0f);
            deathScreen.GetComponent<Image>().color = c1;
            yield return null;
        }

        Color c2 = deathScreen.GetComponentInChildren<TextMeshProUGUI>().color;

        float elapsed2 = 0f;

        while (elapsed2 < 0.5f)
        {
            elapsed2 += Time.deltaTime;
            c2.a = Mathf.Lerp(0f, 1f, elapsed2 / 0.5f);
            deathScreen.GetComponentInChildren<TextMeshProUGUI>().color = c2;
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        elapsed = 0f;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            c1.a = Mathf.Lerp(0.7f, 1.0f, elapsed / 0.5f);
            c2.a = Mathf.Lerp(1f, 0f, elapsed2 / 0.5f);
            deathScreen.GetComponent<Image>().color = c1;
            deathScreen.GetComponentInChildren<TextMeshProUGUI>().color = c2;
            yield return null;
        }

        string oldSceneName = SceneManager.GetActiveScene().name;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(checkpointRoom, LoadSceneMode.Additive);
        yield return loadOperation;

        Scene newScene = SceneManager.GetSceneByName(checkpointRoom);
        SceneManager.SetActiveScene(newScene);

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(oldSceneName);
        yield return unloadOperation;

        Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(0);

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint.checkpointID == checkpointID)
            {
                transform.position = checkpoint.transform.position - new Vector3(1, 0, 0);
            }
        }

        hp = maxHp;
        hpRestoreProgress = 0;
        hudManager.UpdateHealthUI(hp);
        hudManager.EmptyRefillBar();

        elapsed = 0f;

        yield return new WaitForSeconds(2f);

        while (elapsed < 2.5f)
        {
            elapsed += Time.deltaTime;
            c1.a = Mathf.Lerp(1f, 0f, elapsed / 2.5f);
            deathScreen.GetComponent<Image>().color = c1;
            yield return null;
        }

        animator.SetTrigger("Falling");
        animator.SetTrigger("Idle");
        playerMovement.playerDead = false;

        yield return null;
    }
}
