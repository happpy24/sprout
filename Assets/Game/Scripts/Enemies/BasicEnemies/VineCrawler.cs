using UnityEngine;
public class VineCrawler : EnemyBase
{
    [Header("Movement")]
    public float walkSpeed = 1f; // regular enemy speed
    public float chaseSpeed = 2.5f; // enemy speed when player is in radius
    public float detectionRadius = 1.7f; // player detection radius

    public Transform groundCheck;
    public Transform wallCheck;
    public LayerMask collisionLayer;

    private Rigidbody2D rb;
    private bool facingRight = true;
    private Transform player;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void FixedUpdate()
    {
        if (player != null && Vector2.Distance(transform.position, player.position) <= detectionRadius)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        float moveDir = facingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(moveDir * walkSpeed, rb.linearVelocity.y);

        // check for ground
        RaycastHit2D groundHit = Physics2D.Raycast(groundCheck.position, Vector2.down, 0.2f, collisionLayer);
        // check for wall
        Vector2 wallDir = facingRight ? Vector2.right : Vector2.left;
        RaycastHit2D wallHit = Physics2D.Raycast(wallCheck.position, wallDir, 0.2f, collisionLayer);

        if (groundHit.collider == null || wallHit.collider != null)
            Flip();
    }

    private void ChasePlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        rb.linearVelocity = new Vector2(dir * chaseSpeed, rb.linearVelocity.y);

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
            Flip();
    }

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 s = transform.localScale;
        s.x = Mathf.Abs(s.x) * (facingRight ? 1 : -1);
        transform.localScale = s;

        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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

        if (player != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}
