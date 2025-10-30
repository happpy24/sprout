using System.Collections;
using System.Collections.Generic;
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

    [Header("Death Settings")]
    public float deathMusicFadeOutDuration = 1.5f;
    public float deathMusicFadeInDuration = 2f;

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
    public PlayerMovement playerMovement;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;
    private Material spriteMaterial;
    private SceneController sceneController;

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
        if (!audioManager || !animator || !hudManager || !deathScreen || !playerMovement || !mainCamera || !sceneController)
        {
            CacheComponents();
        }

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
        sceneController = FindFirstObjectByType<SceneController>();
        originalTimeScale = Time.timeScale;
        originalSpriteColor = spriteRenderer.color;

        spriteMaterial = new Material(spriteRenderer.material);
        spriteRenderer.material = spriteMaterial;
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
        // Reset opening sequence flag in case we return to HB_4
        OpeningSequence.ResetSequence();

        // 1. Start fading out music
        if (audioManager != null)
        {
            StartCoroutine(audioManager.FadeOutMusic(deathMusicFadeOutDuration));
        }

        // 2. Block player input but allow physics to continue (player will fall to ground)
        playerMovement.playerDead = true;

        // 3. Wait until player is grounded
        yield return new WaitUntil(() => playerMovement != null && 
            (Physics2D.OverlapBox(playerMovement.groundCheck.position, playerMovement.groundBoxSize, 0, playerMovement.tilemapLayer) != null));

        // 4. Stop all movement and block animations
        playerMovement.enabled = false;

        // 5. Play death animation (reset all triggers first)
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Walking");
        animator.ResetTrigger("Falling");
        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Jump");
        animator.ResetTrigger("WallSlide");
        animator.ResetTrigger("WallJump");
        animator.ResetTrigger("Dash");
        animator.ResetTrigger("Land");
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(1f);

        // 6. Show death screen UI
        yield return FadeDeathScreen(0f, 0.7f, 1.0f);
        yield return FadeDeathText(0f, 1f, 0.5f);
        yield return new WaitForSeconds(2f);
        yield return FadeDeathScreenAndText(0.7f, 1.0f, 1f, 0f, 0.5f);

        // 7. Unload ALL scenes and load checkpoint scene fresh
        yield return UnloadAllScenesAndLoadCheckpoint();

        // 8. Reset player state
        ResetPlayerState();

        // 9. Teleport to checkpoint position
        TeleportToCheckpoint();

        // 10. Preload adjacent scenes from checkpoint room's SceneLoadTriggers
        yield return PreloadAdjacentScenes();

        // 11. Fade in music for the checkpoint room
        if (audioManager != null)
        {
            string checkpointPrefix = checkpointRoom.Length >= 2 ? checkpointRoom.Substring(0, 2) : checkpointRoom;
            StartCoroutine(audioManager.FadeInMusicForPrefix(checkpointPrefix));
        }

        // 12. Reset animator to Idle state (clear all animation state)
        animator.Rebind();
        animator.Update(0f);
        animator.SetTrigger("Idle");
        
        // 13. Re-enable player movement
        playerMovement.playerDead = false;
        playerMovement.enabled = true;

        yield return new WaitForSeconds(0.5f);

        // 14. Fade from black screen
        yield return FadeDeathScreen(1f, 0f, 2.5f);
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

        color.a = endAlpha;
        deathImage.color = color;
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

        color.a = endAlpha;
        deathText.color = color;
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

        screenColor.a = screenEnd;
        textColor.a = textEnd;
        deathImage.color = screenColor;
        deathText.color = textColor;
    }

    private IEnumerator UnloadAllScenesAndLoadCheckpoint()
    {
        // Clear the SceneController's preloaded scenes cache
        SceneController.ClearPreloadedScenes();
        
        // Get all currently loaded scenes BEFORE loading checkpoint
        List<Scene> scenesToUnload = new List<Scene>();
        
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.isLoaded && !string.IsNullOrEmpty(scene.name))
            {
                scenesToUnload.Add(scene);
            }
        }

        // Load the checkpoint scene FIRST (so there's always an active scene)
        Scene existingCheckpointScene = SceneManager.GetSceneByName(checkpointRoom);
        
        if (existingCheckpointScene.isLoaded)
        {
            // If checkpoint scene is already loaded, just activate it
            SceneManager.SetActiveScene(existingCheckpointScene);
        }
        else
        {
            // Load checkpoint scene fresh
            yield return SceneManager.LoadSceneAsync(checkpointRoom, LoadSceneMode.Additive);
            
            Scene checkpointScene = SceneManager.GetSceneByName(checkpointRoom);
            SceneManager.SetActiveScene(checkpointScene);
        }

        // Update SceneController's current scene tracking and add to preloaded scenes
        SceneController.SetCurrentSceneName(checkpointRoom);
        
        yield return null;

        // NOW unload all other scenes
        foreach (Scene scene in scenesToUnload)
        {
            // Don't unload the checkpoint scene we just loaded/activated
            if (scene.name != checkpointRoom && scene.isLoaded)
            {
                // Deactivate objects first
                GameObject[] rootObjects = scene.GetRootGameObjects();
                foreach (GameObject obj in rootObjects)
                {
                    if (obj != null)
                    {
                        obj.SetActive(false);
                    }
                }
                
                // Unload the scene
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        // Reactivate all objects in checkpoint scene
        Scene finalCheckpointScene = SceneManager.GetSceneByName(checkpointRoom);
        GameObject[] checkpointObjects = finalCheckpointScene.GetRootGameObjects();
        foreach (GameObject obj in checkpointObjects)
        {
            if (obj != null && !obj.activeSelf)
            {
                obj.SetActive(true);
            }
        }
        
        yield return null;
    }

    private IEnumerator PreloadAdjacentScenes()
    {
        if (sceneController == null) 
        {
            Debug.LogWarning("SceneController is null, cannot preload adjacent scenes.");
            yield break;
        }

        // Wait multiple frames to ensure all scene unloading is completely finished
        yield return null;
        yield return null;

        // Find all SceneLoadTriggers in the checkpoint scene
        SceneLoadTrigger[] triggers = FindObjectsByType<SceneLoadTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        List<string> scenesToPreload = new List<string>();
        
        foreach (SceneLoadTrigger trigger in triggers)
        {
            // Only preload triggers in the active (checkpoint) scene
            if (trigger.gameObject.scene.name == checkpointRoom && !string.IsNullOrEmpty(trigger.sceneName))
            {
                if (!scenesToPreload.Contains(trigger.sceneName))
                {
                    scenesToPreload.Add(trigger.sceneName);
                }
            }
        }

        Debug.Log($"Found {scenesToPreload.Count} scenes to preload from checkpoint room {checkpointRoom}");

        // Preload each unique scene
        foreach (string sceneName in scenesToPreload)
        {
            Debug.Log($"Preloading adjacent scene: {sceneName}");
            yield return sceneController.PreloadSceneInBackground(sceneName);
        }
        
        Debug.Log("Adjacent scene preloading complete.");
    }

    private void TeleportToCheckpoint()
    {
        Checkpoint[] checkpoints = FindObjectsByType<Checkpoint>(FindObjectsSortMode.None);

        foreach (Checkpoint checkpoint in checkpoints)
        {
            if (checkpoint.checkpointID == checkpointID)
            {
                // Position player slightly to the left of the checkpoint
                transform.position = checkpoint.transform.position + new Vector3(-1f, 0f, 0f);
                return;
            }
        }

        // Fallback: if no matching checkpoint found, just use the first one
        if (checkpoints.Length > 0)
        {
            transform.position = checkpoints[0].transform.position + new Vector3(-1f, 0f, 0f);
        }
    }

    private void ResetPlayerState()
    {
        hp = maxHp;
        hpRestoreProgress = 0;
        playerDeathActivate = true;
        isKnockedBack = false;
        isInvincible = false;
        knockbackVelocity = Vector2.zero;
        
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