using UnityEngine;
using System.Collections;

public class VineCrawler : EnemyBase
{
    [Header("Movement")]
    public float walkSpeed = 1f;
    public float chaseSpeed = 2.5f;
    public float detectionRadius = 1.7f;
    public float loseSightRadius = 3.5f;
    public float chaseStartDelay = 0.25f;
    public float loseSightTime = 1f;
    public float proximityRadius = 1.0f;

    [Header("Checks")]
    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask collisionLayer;
    public LayerMask playerLayer;

    private Rigidbody2D rb;
    private bool facingRight = true;
    private Transform player;
    private Animator animator;
    private bool isChasing = false;
    private bool isStartingChase = false;
    private float loseSightTimer = 0f;
    private readonly Vector2 rayOriginOffset = Vector2.down * 0.25f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj) player = pObj.transform;
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (player == null) return;

        if (isChasing)
        {
            bool playerIsRight = player.position.x > transform.position.x;
            if (playerIsRight != facingRight) Flip();

            if (PlayerInSightFacing() || PlayerInProximity())
            {
                loseSightTimer = 0f;
                FollowPlayer();
            }
            else
            {
                loseSightTimer += Time.deltaTime;
                if (loseSightTimer >= loseSightTime) StopChase();
            }
        }
        else if (!isStartingChase)
        {
            if (PlayerInSightFacing() || PlayerInProximity())
                StartCoroutine(BeginChase());
            else
                Patrol();
        }
    }

    private bool PlayerInSightFacing()
    {
        Vector2 origin = (Vector2)transform.position + rayOriginOffset;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;
        int mask = playerLayer | collisionLayer;
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, detectionRadius, mask);
        Debug.DrawRay(origin, direction * detectionRadius, hit.collider ? Color.red : Color.green);
        if (!hit.collider) return false;
        if (((1 << hit.collider.gameObject.layer) & collisionLayer) != 0) return false;
        if (hit.collider.CompareTag("Player")) return true;
        return false;
    }

    private bool PlayerInProximity()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        return dist <= proximityRadius;
    }

    private IEnumerator BeginChase()
    {
        isStartingChase = true;
        rb.linearVelocity = Vector2.zero;
        if (animator) animator.SetTrigger("StartChase");
        yield return new WaitForSeconds(chaseStartDelay);
        isChasing = true;
        isStartingChase = false;
        loseSightTimer = 0f;
    }

    private void StopChase()
    {
        isChasing = false;
        loseSightTimer = 0f;
        if (animator) animator.SetTrigger("StopChase");
        rb.linearVelocity = Vector2.zero;
    }

    private void Patrol()
    {
        if (isStartingChase || isChasing) return;
        float moveDir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.2f, collisionLayer);
        Vector2 wallDir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, wallDir, 0.2f, collisionLayer);
        if (groundHit.collider == null || wallHit.collider != null) Flip();
    }

    private void FollowPlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > loseSightRadius)
        {
            loseSightTimer += Time.deltaTime;
            if (loseSightTimer >= loseSightTime) StopChase();
        }
        else loseSightTimer = 0f;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1);
        transform.localScale = s;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(groundCheck.position, groundCheck.position + Vector3.down * 0.2f);
        }

        if (wallCheck)
        {
            Gizmos.color = Color.red;
            Vector3 dir = facingRight ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + dir * 0.2f);
        }

        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + Vector3.down * 0.25f;
        Vector3 facingDir = facingRight ? Vector3.right : Vector3.left;
        Gizmos.DrawLine(origin, origin + facingDir * detectionRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, loseSightRadius);
    }
}
