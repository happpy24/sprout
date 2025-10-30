using System.Collections;
using UnityEngine;

public class EnemyBase : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField]
    private bool damageable = true;
    
    [SerializeField]
    private int healthAmount = 100;
    
    [SerializeField]
    private float invulnerabilityTime = 0.2f;

    [SerializeField]
    public float knockbackForce = 5f;
    
    public bool giveUpwardForce = true;
    
    public bool hit;
    public bool isDead = false;
    
    private int currentHealth;

    private Animator anim;

    private Rigidbody2D rb;

    private AudioManager audioManager;

    // Generic movement script reference
    private IEnemyMovement movementScript;

    // Particle effect for damage flash
    private ParticleSystem damageFlashParticles;

    private void Start()
    {
        if (GetComponent<Animator>() != null)
            anim = GetComponent<Animator>();

        if (GetComponent<Rigidbody2D>() != null)
            rb = GetComponent<Rigidbody2D>();

        currentHealth = healthAmount;

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();

        // Find any movement script that implements IEnemyMovement
        movementScript = GetComponent<IEnemyMovement>();

        // Create damage flash particle system
        CreateDamageFlashParticles();
    }

    private void CreateDamageFlashParticles()
    {
        // Create a child GameObject for the particle system
        GameObject particleObj = new GameObject("DamageFlashParticles");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = Vector3.zero;

        // Add and configure particle system
        damageFlashParticles = particleObj.AddComponent<ParticleSystem>();
        
        var main = damageFlashParticles.main;
        main.startLifetime = 0.1f;
        main.startSpeed = 0f;
        main.startSize = 0.1f; // Adjust based on enemy size
        main.startColor = Color.white;
        main.maxParticles = 100;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        var emission = damageFlashParticles.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 3) });

        var shape = damageFlashParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sprite;
        // Try to get sprite from SpriteRenderer
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            shape.sprite = sr.sprite;
        }

        var renderer = damageFlashParticles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 100; // Render on top
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

        // Set material to additive for better flash effect
        Material particleMat = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        particleMat.color = Color.white;
        renderer.material = particleMat;
    }

    public void Damage(int amount, Vector2 playerPosition)
    {
        if (damageable && !hit && currentHealth > 0 && !isDead)
        {
            hit = true;
            currentHealth -= amount;

            // Play enemy hit sound
            if (audioManager != null)
            {
                audioManager.PlaySFX(audioManager.enemyHit);
            }

            // Play particle flash effect
            PlayDamageFlash();

            // Apply knockback and notify movement script
            ApplyKnockback(playerPosition);

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                StartCoroutine(Die());
            }
            else
            {
                StartCoroutine(TurnOffHit());
            }
        }
    }

    private void PlayDamageFlash()
    {
        if (damageFlashParticles != null)
        {
            // Play the particle burst 3 times with delays
            StartCoroutine(FlashParticles());
        }
    }

    private IEnumerator FlashParticles()
    {
        for (int i = 0; i < 3; i++)
        {
            damageFlashParticles.Play();
            yield return new WaitForSeconds(0.05f);
        }
    }

    private void ApplyKnockback(Vector2 playerPosition)
    {
        if (rb != null && !isDead)
        {
            // Calculate direction from player to enemy
            Vector2 knockbackDirection = ((Vector2)transform.position - playerPosition).normalized;

            // Apply knockback force
            rb.linearVelocity = new Vector2(knockbackDirection.x * knockbackForce, rb.linearVelocity.y);

            // Notify any movement script about knockback (generic approach)
            if (movementScript != null)
            {
                movementScript.OnKnockbackStart();
            }
        }
    }

    IEnumerator TurnOffHit()
    {
        yield return new WaitForSeconds(invulnerabilityTime);
        hit = false;
    }

    public virtual void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth <= 0)
            StartCoroutine(Die());
    }

    IEnumerator Die()
    {
        isDead = true;
        
        // Stop all movement immediately
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Prevent further physics interactions
        }
        
        if (anim != null)
        {
            anim.SetTrigger("Death");
        }
        
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
