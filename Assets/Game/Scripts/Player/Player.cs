using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    public int maxHp = 5;
    public int hp = 5;
    public int hpRestoreProgress = 0;
    public int damage = 20;

    [Header("Knockback Settings")]
    public float knockbackForce = 10f;
    public float knockbackUpwardForce = 5f;
    public float knockbackDuration = 0.2f;
    public float impactFreezeDuration = 0.15f;

    [Header("Impact Effects")]
    public float screenShakeIntensity = 0.3f;
    public float screenShakeDuration = 0.2f;
    public float chromaticAberrationAmount = 0.5f;
    public Color damageFlashColor = Color.red;
    public float damageFlashIntensity = 0.7f;

    [Header("Invincibility Settings")]
    public float invincibilityDuration = 1f;
    public float flashInterval = 0.1f;

    [Header("Systems")]
    public LayerMask damageableLayer;

    // Flags
    public bool doDamage = false;
    public bool giveHpRstore = false;
    public bool increaseHP = false;
    private bool playerDeathActivate = true;
    private bool isKnockedBack = false;
    private bool isInvincible = false;

    // Checkpoint Data
    public string checkpointRoom = "HB_1";
    public float checkpointID = 0;

    // Cached Components
    private AudioManager audioManager;
    private Animator animator;
    private HudManager hudManager;
    private GameObject deathScreen;
    private PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Material spriteMaterial;

    // Knockback Data
    private Vector2 lastDamageSourcePosition;
    private Vector2 knockbackVelocity;
    private float originalTimeScale;
    private Color originalSpriteColor;

    void Start()
    {
        CacheComponents();
    }

    void Update()
    {
        if (!audioManager || !animator || !hudManager || !deathScreen || !playerMovement || !mainCamera)
        {
            CacheComponents();
        }

        HandleInput();
        HandleHealthRestore();
        HandleDamage();
        HandleHPRestore();
        HandleDeath();

        if (increaseHP)
        {
            increaseHP = false;
            IncreaseHP();
        }
    }

    private void CacheComponents()
    {
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        animator = GetComponent<Animator>();
        hudManager = GameObject.FindGameObjectWithTag("HudManager").GetComponent<HudManager>();
        deathScreen = GameObject.FindGameObjectWithTag("DeathScreen");
        playerMovement = GetComponent<PlayerMovement>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
        originalTimeScale = Time.timeScale;
        originalSpriteColor = spriteRenderer.color;

        spriteMaterial = new Material(spriteRenderer.material);
        spriteRenderer.material = spriteMaterial;
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            Attack();
        }
    }

    private void HandleHealthRestore()
    {
        if (hpRestoreProgress >= 5 && hp < maxHp)
        {
            hpRestoreProgress = 0;
            hp++;
            hudManager.EmptyRefillBar();
            hudManager.UpdateHealthUI(hp);
        }
    }

    private void HandleDamage()
    {
        if (doDamage && !isInvincible)
        {
            doDamage = false;
            hp--;
            animator.SetTrigger("Hit");
            hudManager.UpdateHealthUI(hp);
            StartCoroutine(DamageSequence());
        }
        else if (doDamage && isInvincible)
        {
            doDamage = false;
        }
    }

    private void HandleHPRestore()
    {
        if (giveHpRstore)
        {
            giveHpRstore = false;
            GiveHPRestore();
        }
    }

    private void HandleDeath()
    {
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
            hpRestoreProgress++;
            hudManager.FillRefillBar(hpRestoreProgress);
        }
    }

    private void IncreaseHP()
    {
        if (maxHp < 7)
        {
            maxHp++;
            hp = maxHp;
            hudManager.AddLife();
            hpRestoreProgress = 5;
            hudManager.FillRefillBar(5);
        }
    }

    private IEnumerator DamageSequence()
    {
        // Play Hit SFX
        audioManager.PlaySFX(audioManager.playerHit);

        // Start visual effects
        mainCamera.GetComponent<PlayerCamera>().Shake(screenShakeIntensity, screenShakeDuration);
        StartCoroutine(DamageFlash());

        // Impact freeze frame
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(impactFreezeDuration);
        Time.timeScale = originalTimeScale;

        // Apply knockback
        ApplyKnockback();

        // Start invincibility with flashing
        StartCoroutine(InvincibilityFlash());
    }

    private void ApplyKnockback()
    {
        if (playerMovement == null) return;

        Vector2 horizontalDirection = ((Vector2)transform.position - lastDamageSourcePosition).normalized;
        Vector2 knockbackDirection = new Vector2(horizontalDirection.x, 0).normalized;

        isKnockedBack = true;
        knockbackVelocity = new Vector2(knockbackDirection.x * knockbackForce, knockbackUpwardForce);

        StartCoroutine(KnockbackCoroutine());
    }

    private IEnumerator KnockbackCoroutine()
    {
        yield return new WaitForSeconds(knockbackDuration);

        isKnockedBack = false;
        knockbackVelocity = Vector2.zero;
    }

    private IEnumerator InvincibilityFlash()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(flashInterval);
            elapsed += flashInterval;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }

    private IEnumerator DamageFlash()
    {
        // Flash the sprite to damage color
        spriteRenderer.color = damageFlashColor;

        yield return new WaitForSecondsRealtime(impactFreezeDuration);

        // Fade back to original color
        float elapsed = 0f;
        float fadeDuration = 0.1f;

        while (elapsed < fadeDuration)
        {
            spriteRenderer.color = Color.Lerp(damageFlashColor, originalSpriteColor, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = originalSpriteColor;
    }

    public Vector2 GetKnockbackVelocity()
    {
        return isKnockedBack ? knockbackVelocity : Vector2.zero;
    }

    public bool IsKnockedBack()
    {
        return isKnockedBack;
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    IEnumerator PlayerDeath()
    {
        playerMovement.playerDead = true;
        animator.SetTrigger("Death");

        yield return new WaitForSeconds(1f);

        yield return FadeDeathScreen(0f, 0.7f, 1.0f);

        yield return FadeDeathText(0f, 1f, 0.5f);

        yield return new WaitForSeconds(1f);

        yield return FadeDeathScreenAndText(0.7f, 1.0f, 1f, 0f, 0.5f);

        yield return LoadCheckpointScene();

        ResetPlayerState();

        yield return new WaitForSeconds(2f);

        yield return FadeDeathScreen(1f, 0f, 2.5f);

        // Reset animations and movement
        animator.SetTrigger("Falling");
        animator.SetTrigger("Idle");
        playerMovement.playerDead = false;
    }

    private IEnumerator FadeDeathScreen(float startAlpha, float endAlpha, float duration)
    {
        Image deathImage = deathScreen.GetComponent<Image>();
        Color color = deathImage.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            deathImage.color = color;
            yield return null;
        }
    }

    private IEnumerator FadeDeathText(float startAlpha, float endAlpha, float duration)
    {
        TextMeshProUGUI deathText = deathScreen.GetComponentInChildren<TextMeshProUGUI>();
        Color color = deathText.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
            deathText.color = color;
            yield return null;
        }
    }

    private IEnumerator FadeDeathScreenAndText(float screenStart, float screenEnd, float textStart, float textEnd, float duration)
    {
        Image deathImage = deathScreen.GetComponent<Image>();
        TextMeshProUGUI deathText = deathScreen.GetComponentInChildren<TextMeshProUGUI>();
        Color screenColor = deathImage.color;
        Color textColor = deathText.color;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            screenColor.a = Mathf.Lerp(screenStart, screenEnd, elapsed / duration);
            textColor.a = Mathf.Lerp(textStart, textEnd, elapsed / duration);
            deathImage.color = screenColor;
            deathText.color = textColor;
            yield return null;
        }
    }

    private IEnumerator LoadCheckpointScene()
    {
        string oldSceneName = SceneManager.GetActiveScene().name;

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(checkpointRoom, LoadSceneMode.Additive);
        yield return loadOperation;

        Scene newScene = SceneManager.GetSceneByName(checkpointRoom);
        SceneManager.SetActiveScene(newScene);

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(oldSceneName);
        yield return unloadOperation;

        TeleportToCheckpoint();
    }

    private void TeleportToCheckpoint()
    {
        Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint.checkpointID == checkpointID)
            {
                transform.position = checkpoint.transform.position - new Vector3(1, 0, 0);
                break;
            }
        }
    }

    private void ResetPlayerState()
    {
        hp = maxHp;
        hpRestoreProgress = 0;
        playerDeathActivate = true;
        hudManager.UpdateHealthUI(hp);
        hudManager.EmptyRefillBar();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckDamageableCollision(collision.collider, collision.GetContact(0).point);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CheckDamageableCollision(other, other.ClosestPoint(transform.position));
    }

    private void CheckDamageableCollision(Collider2D col, Vector2 contactPoint)
    {
        if (((1 << col.gameObject.layer) & damageableLayer) != 0)
        {
            lastDamageSourcePosition = contactPoint;
            doDamage = true;
        }
    }
}