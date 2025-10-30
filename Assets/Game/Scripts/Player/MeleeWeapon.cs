using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    [SerializeField]
    private int damageAmount = 20;
    [SerializeField]
    private float wallKnockbackForce = 3f;
    
    private PlayerMovement playerMovement;
    private MeleeAttackManager meleeAttackManager;
    private Vector2 direction;
    private bool collided;
    private bool downwardStrike;

    private void Start()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
        meleeAttackManager = GetComponentInParent<MeleeAttackManager>();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Check for enemy collision first
        EnemyBase enemy = collision.GetComponent<EnemyBase>();
        if (enemy == null)
        {
            enemy = collision.GetComponentInParent<EnemyBase>();
        }

        if (enemy != null)
        {
            HandleCollision(enemy);
            return;
        }

        // Check for wall collision using LayerMask
        if (((1 << collision.gameObject.layer) & playerMovement.tilemapLayer) != 0)
        {
            HandleWallCollision();
        }
    }

    private void HandleCollision(EnemyBase objHealth)
    {
        if (objHealth.giveUpwardForce && Input.GetAxis("Vertical") < 0 && !playerMovement.isGrounded)
        {
            direction = Vector2.up;
            downwardStrike = true;
            collided = true;
        }
        else if (Input.GetAxis("Vertical") > 0 && !playerMovement.isGrounded)
        {
            direction = Vector2.down;
            collided = true;
        }
        else if ((Input.GetAxis("Vertical") <= 0 && playerMovement.isGrounded) || Input.GetAxis("Vertical") == 0)
        {
            if (playerMovement.isFacingLeft)
            {
                direction = Vector2.left;
            }
            else
            {
                direction = Vector2.right;
            }
            collided = true;
        }

        objHealth.Damage(damageAmount, transform.position);
        StartCoroutine(NoLongerColliding());
    }

    private void HandleWallCollision()
    {
        // Knockback away from wall based on facing direction
        Vector2 knockbackDirection;
        
        if (playerMovement.isFacingLeft)
        {
            knockbackDirection = Vector2.right;
        }
        else
        {
            knockbackDirection = Vector2.left;
        }
                
        // Use ApplyExternalForce for wall knockback
        playerMovement.ApplyExternalForce(knockbackDirection * wallKnockbackForce, meleeAttackManager.movementTime);
    }

    private void HandleMovement()
    {
        if (collided)
        {
            if (downwardStrike)
            {
                playerMovement.velocity = (direction * meleeAttackManager.upwardsForce);
            }
            else
            {
                playerMovement.velocity = (direction * meleeAttackManager.defaultForce);
            }
        }
    }

    IEnumerator NoLongerColliding()
    {
        yield return new WaitForSeconds(meleeAttackManager.movementTime);
        collided = false;
        downwardStrike = false;
    }
}
