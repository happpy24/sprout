using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("General Movement")]
    public float acceleration = 20f;
    public float deceleration = 25f;
    public float maxSpeed = 5f;
    public float velocityPower = 0.9f;

    [Header("Jumping")]
    public float jumpForce = 6.5f;
    public float variableJumpTime = 0.3f;
    public float gravity = 30f;
    public float fallGravityMultiplier = 1.5f;
    public float jumpGravityMultiplier = 0.85f;
    public float maxFallSpeed = 20f;
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.1f;

    [Header("Double Jump")]
    public bool allowDoubleJump = true;
    private bool canDoubleJump = false;

    [Header("Wall Jump & Slide")]
    public bool allowWallJump = true;
    public float wallSlideSpeed = 3f;
    public float wallJumpHorizontalForce = 5f;
    public float wallJumpVerticalForce = 6.5f;
    public float wallJumpControlDelay = 0.15f;
    private bool isTouchingWall;
    private bool isWallSliding;
    private int wallDir;

    [Header("Dash")]
    public bool allowDash = true;
    public float dashSpeed = 15f;
    public float dashDuration = 0.15f;
    public float dashCooldown = 0.3f;

    [Header("Collision Settings")]
    [Tooltip("Your existing ground/wall check transforms")]
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Vector2 groundBoxSize = new Vector2(0.6f, 0.075f);
    public Vector2 wallBoxSize = new Vector2(0.1f, 0.75f);

    [Tooltip("Distance to check for collisions (should be small, like 0.05)")]
    public float collisionCheckDistance = 0.05f;

    public LayerMask tilemapLayer;

    [Header("Debug Gizmos")]
    public bool showDebugGizmos = true;

    // Components
    private BoxCollider2D boxCollider;

    // Velocity
    private Vector2 velocity;

    // Input
    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool dashPressed;

    // State
    private bool isGrounded;
    private bool isJumping;
    private bool canMove = true;
    private bool isDashing = false;
    private bool canDash = true;

    // Timers
    private float jumpTimeCounter;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    // Collision info
    private RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    private ContactFilter2D contactFilter;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        contactFilter = new ContactFilter2D();
        contactFilter.layerMask = tilemapLayer;
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        jumpHeld = Input.GetKey(KeyCode.Space);
        dashPressed = Input.GetKeyDown(KeyCode.LeftShift);

        CheckCollisions();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            canDoubleJump = true;
            canDash = true;
        }

        if (jumpPressed) lastJumpPressedTime = Time.time;

        HandleJump();
        HandleDash();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            MoveDash();
            return;
        }

        HandleMovement();
        ApplyGravity();
        MoveCharacter(velocity * Time.fixedDeltaTime);
    }

    // ---------------- COLLISIONS ----------------

    void CheckCollisions()
    {
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundBoxSize, 0, tilemapLayer);

        float scaleX = transform.localScale.x;
        bool wallLeft = Physics2D.OverlapBox(wallCheckLeft.position, wallBoxSize, 0, tilemapLayer);
        bool wallRight = Physics2D.OverlapBox(wallCheckRight.position, wallBoxSize, 0, tilemapLayer);

        bool actualWallLeft = scaleX > 0 ? wallLeft : wallRight;
        bool actualWallRight = scaleX > 0 ? wallRight : wallLeft;

        isTouchingWall = wallLeft || wallRight;

        if (actualWallLeft && !actualWallRight)
            wallDir = -1;
        else if (actualWallRight && !actualWallLeft)
            wallDir = 1;
        else if (actualWallLeft && actualWallRight)
            wallDir = (int)Mathf.Sign(moveInput) * -1;
        else
            wallDir = 0;

        // Wall sliding
        isWallSliding = allowWallJump && isTouchingWall && !isGrounded && velocity.y < 0;
    }

    // ---------------- MOVEMENT ----------------

    void HandleMovement()
    {
        if (!canMove) return;

        float targetSpeed = moveInput * maxSpeed;
        float speedDiff = targetSpeed - velocity.x;
        float accelRate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
        float movement = Mathf.Pow(Mathf.Abs(speedDiff) * accelRate, velocityPower) * Mathf.Sign(speedDiff);

        velocity.x += movement * Time.fixedDeltaTime;

        // Clamp to max speed when grounded
        if (isGrounded && Mathf.Abs(velocity.x) > maxSpeed)
            velocity.x = Mathf.Sign(velocity.x) * maxSpeed;

        // Flip sprite
        if (moveInput != 0)
            transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
    }

    // ---------------- GRAVITY ----------------

    void ApplyGravity()
    {
        float gravityToApply = gravity;

        // Jump gravity for slight floatiness
        if (velocity.y > 0 && jumpHeld)
        {
            gravityToApply = gravity * jumpGravityMultiplier;
        }
        // Quick cut when releasing jump
        else if (velocity.y > 0 && !jumpHeld)
        {
            gravityToApply = gravity * (fallGravityMultiplier * 1.1f);
        }
        // Fast fall
        else if (velocity.y < 0)
        {
            gravityToApply = gravity * fallGravityMultiplier;
        }

        velocity.y -= gravityToApply * Time.fixedDeltaTime;

        // Clamp fall speed
        if (velocity.y < -maxFallSpeed)
            velocity.y = -maxFallSpeed;

        // Wall sliding
        if (isWallSliding)
        {
            if (velocity.y < -wallSlideSpeed)
                velocity.y = -wallSlideSpeed;
        }
    }

    // ---------------- CHARACTER MOVEMENT WITH COLLISION ----------------

    void MoveCharacter(Vector2 moveAmount)
    {
        // Move horizontally
        if (Mathf.Abs(moveAmount.x) > 0.0001f)
        {
            int hitCount = boxCollider.Cast(new Vector2(Mathf.Sign(moveAmount.x), 0), contactFilter, hitBuffer, Mathf.Abs(moveAmount.x) + collisionCheckDistance);

            if (hitCount > 0)
            {
                // Hit something, move to contact point
                moveAmount.x = (hitBuffer[0].distance - collisionCheckDistance) * Mathf.Sign(moveAmount.x);
                velocity.x = 0;
            }
        }

        // Apply horizontal movement
        transform.Translate(new Vector2(moveAmount.x, 0));

        // Move vertically
        if (Mathf.Abs(moveAmount.y) > 0.0001f)
        {
            int hitCount = boxCollider.Cast(new Vector2(0, Mathf.Sign(moveAmount.y)), contactFilter, hitBuffer, Mathf.Abs(moveAmount.y) + collisionCheckDistance);

            if (hitCount > 0)
            {
                // Hit something, move to contact point
                moveAmount.y = (hitBuffer[0].distance - collisionCheckDistance) * Mathf.Sign(moveAmount.y);
                velocity.y = 0;
            }
        }

        // Apply vertical movement
        transform.Translate(new Vector2(0, moveAmount.y));
    }

    // ---------------- JUMP ----------------

    void HandleJump()
    {
        bool canCoyoteJump = Time.time - lastGroundedTime <= coyoteTime;
        bool canBufferJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (canBufferJump && (canCoyoteJump || (allowWallJump && isWallSliding)))
        {
            Jump();
            lastJumpPressedTime = -999f;
        }
        else if (canBufferJump && allowDoubleJump && canDoubleJump && !isGrounded && !isWallSliding)
        {
            DoubleJump();
            lastJumpPressedTime = -999f;
        }

        // Variable jump height
        if (isJumping && jumpHeld && jumpTimeCounter > 0)
        {
            jumpTimeCounter -= Time.deltaTime;
            velocity.y = jumpForce;
        }
        else if (isJumping && (!jumpHeld || jumpTimeCounter <= 0))
        {
            isJumping = false;
        }
    }

    void Jump()
    {
        if (isWallSliding)
        {
            // Wall jump
            velocity.x = -wallDir * wallJumpHorizontalForce;
            velocity.y = wallJumpVerticalForce;

            // Face away from wall
            transform.localScale = new Vector3(-wallDir, 1, 1);

            StartCoroutine(WallJumpControlDelay());
        }
        else
        {
            // Normal jump
            velocity.y = jumpForce;
        }

        isJumping = true;
        jumpTimeCounter = variableJumpTime;
    }

    void DoubleJump()
    {
        canDoubleJump = false;
        velocity.y = jumpForce;
        isJumping = true;
        jumpTimeCounter = variableJumpTime;
    }

    IEnumerator WallJumpControlDelay()
    {
        canMove = false;
        yield return new WaitForSeconds(wallJumpControlDelay);
        canMove = true;
    }

    // ---------------- DASH ----------------

    void HandleDash()
    {
        if (!allowDash) return;
        if (dashPressed && canDash && !isDashing)
            StartCoroutine(DashCoroutine());
    }

    IEnumerator DashCoroutine()
    {
        canDash = false;
        isDashing = true;
        canMove = false;

        // Determine dash direction
        float dir = moveInput != 0 ? Mathf.Sign(moveInput) : transform.localScale.x;
        velocity = new Vector2(dir * dashSpeed, 0);

        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            dashTimer += Time.deltaTime;
            yield return null;
        }

        // End dash - keep some momentum
        velocity.x = dir * maxSpeed * 1.5f;
        isDashing = false;
        canMove = true;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    void MoveDash()
    {
        // Dash movement - horizontal only, no gravity
        Vector2 moveAmount = new Vector2(velocity.x * Time.fixedDeltaTime, 0);

        if (Mathf.Abs(moveAmount.x) > 0.0001f)
        {
            int hitCount = boxCollider.Cast(new Vector2(Mathf.Sign(moveAmount.x), 0), contactFilter, hitBuffer, Mathf.Abs(moveAmount.x) + collisionCheckDistance);

            if (hitCount > 0)
            {
                moveAmount.x = (hitBuffer[0].distance - collisionCheckDistance) * Mathf.Sign(moveAmount.x);
                velocity.x = 0;
            }
        }

        transform.Translate(moveAmount);
    }

    // ---------------- DEBUG ----------------

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        // Draw box collider
        if (boxCollider != null)
        {
            Gizmos.color = Color.cyan;
            Vector2 center = (Vector2)transform.position + boxCollider.offset;
            Gizmos.DrawWireCube(center, boxCollider.size);
        }

        // Ground check
        Gizmos.color = Color.green;
        if (groundCheck)
            Gizmos.DrawWireCube(groundCheck.position, groundBoxSize);

        // Wall checks
        Gizmos.color = Color.red;
        if (wallCheckLeft)
            Gizmos.DrawWireCube(wallCheckLeft.position, wallBoxSize);
        if (wallCheckRight)
            Gizmos.DrawWireCube(wallCheckRight.position, wallBoxSize);

        // State indicators
        if (Application.isPlaying)
        {
            // Coyote time
            if (Time.time - lastGroundedTime <= coyoteTime)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(transform.position + Vector3.up * 1.2f, 0.1f);
            }

            // Jump buffer
            if (Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(transform.position + Vector3.up * 1.4f, 0.1f);
            }

            // Wall sliding
            if (isWallSliding)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawSphere(transform.position + Vector3.right * wallDir * 0.6f, 0.15f);
            }
        }
    }
}