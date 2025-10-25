using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Pixel Perfect Settings")]
    public float pixelsPerUnit = 16f;

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
    public Transform groundCheck;
    public Transform wallCheckLeft;
    public Transform wallCheckRight;
    public Vector2 groundBoxSize = new Vector2(0.6f, 0.075f);
    public Vector2 wallBoxSize = new Vector2(0.1f, 0.75f);

    public float collisionCheckDistance = 0.05f;

    public LayerMask tilemapLayer;

    [Header("Debug Gizmos")]
    public bool showDebugGizmos = true;

    // Components
    private BoxCollider2D boxCollider;

    // Velocity
    public Vector2 velocity;

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
    private float fallTime;
    private float jumpTimeCounter;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    // Collision info
    private RaycastHit2D[] hitBuffer = new RaycastHit2D[16];
    private ContactFilter2D contactFilter;

    // Audio Manager
    private AudioManager audioManager;

    // Animator
    private Animator animator;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
        animator = GetComponent<Animator>();

        contactFilter = new ContactFilter2D();
        contactFilter.layerMask = tilemapLayer;
        contactFilter.useLayerMask = true;
        contactFilter.useTriggers = false;

        SnapToPixelGrid();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetKeyDown(KeyCode.Space);
        jumpHeld = Input.GetKey(KeyCode.Space);
        dashPressed = Input.GetKeyDown(KeyCode.LeftShift);

        if (moveInput == 0 && !jumpHeld && !jumpPressed && !dashPressed && Mathf.Round(velocity.x) == 0 && Mathf.Round(velocity.y) == 0)
        {
            animator.ResetTrigger("Walking");
            animator.SetTrigger("Idle");
        }

        CheckCollisions();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            canDoubleJump = true;
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
            SnapToPixelGrid();
            return;
        }

        HandleMovement();
        ApplyGravity();
        MoveCharacter(velocity * Time.fixedDeltaTime);
        SnapToPixelGrid();
    }

    // ---------------- PIXEL PERFECT ----------------

    void SnapToPixelGrid()
    {
        Vector3 pos = transform.position;
        float pixelSize = 1f / pixelsPerUnit;

        pos.x = Mathf.Round(pos.x / pixelSize) * pixelSize;
        pos.y = Mathf.Round(pos.y / pixelSize) * pixelSize;

        transform.position = pos;
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

        if (Mathf.Abs(velocity.x) != 0 && isGrounded && !jumpHeld && !isDashing)
            animator.SetTrigger("Walking");

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
            if (!isWallSliding)
            {
                fallTime += Time.deltaTime;
                animator.SetTrigger("Falling");
            }
        }
        
        if (velocity.y >= 0)
        {
            if (fallTime > 0.5)
            {
                animator.SetTrigger("Land");
            }
            fallTime = 0;
        }
            

        Debug.Log(fallTime);
        velocity.y -= gravityToApply * Time.fixedDeltaTime;

        // Clamp fall speed
        if (velocity.y < -maxFallSpeed)
            velocity.y = -maxFallSpeed;

        // Wall sliding
        if (isWallSliding)
        {
            canDoubleJump = true;
            animator.SetTrigger("WallSlide");

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
                // Hit something
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
                // Hit something
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
        else if (canBufferJump && allowDoubleJump && canDoubleJump)
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
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Walking");
            animator.SetTrigger("WallJump");
            velocity.x = -wallDir * wallJumpHorizontalForce;
            velocity.y = wallJumpVerticalForce;

            // Face away from wall
            transform.localScale = new Vector3(-wallDir, 1, 1);

            StartCoroutine(WallJumpControlDelay());
        }
        else
        {
            // Normal jump
            animator.ResetTrigger("Idle");
            animator.ResetTrigger("Walking");
            animator.SetTrigger("Jump");
            velocity.y = jumpForce;
        }

        isJumping = true;
        jumpTimeCounter = variableJumpTime;
    }

    void DoubleJump()
    {
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Walking");
        animator.SetTrigger("Jump");
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

        audioManager.PlaySFX(audioManager.dash);
        animator.SetTrigger("Dash");

        float dashTimer = 0f;
        while (dashTimer < dashDuration)
        {
            dashTimer += Time.deltaTime;
            yield return null;
        }

        // End dash
        velocity.x = dir * maxSpeed * 1.5f;
        isDashing = false;
        canMove = true;

        // Wait for cooldown, then restore dash if conditions met
        yield return new WaitForSeconds(dashCooldown);

        yield return new WaitUntil(() => isGrounded || isTouchingWall);

        canDash = true;
    }

    void MoveDash()
    {
        // Dash movement
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