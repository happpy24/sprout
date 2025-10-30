using UnityEngine;

public class MeleeAttackManager : MonoBehaviour
{
    public float defaultForce = 2f;
    public float upwardsForce = 6f;
    public float movementTime = .1f;
    public float attackCooldown = 0.2f;

    private bool meleeAttack;
    private Animator meleeAnimator;
    private Animator anim;
    private PlayerMovement playerMovement;
    private AudioManager audioManager;
    private float lastAttackTime = -999f;

    private void Start()
    {
        meleeAnimator = transform.Find("MeleeWeapon").GetComponent<Animator>();
        anim = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>();
        audioManager = GameObject.FindGameObjectWithTag("Audio").GetComponent<AudioManager>();
    }

    private void Update()
    {
        CheckInput();
    }

    private void CheckInput()
    {
        // Check if attack cooldown has elapsed
        bool canAttack = Time.time - lastAttackTime >= attackCooldown;

        if (Input.GetKeyDown(KeyCode.Backspace) && canAttack)
        {
            meleeAttack = true;
            lastAttackTime = Time.time;

            // Play attack sound once when attack is triggered
            if (audioManager != null)
            {
                audioManager.PlaySFX(audioManager.attack);
            }
        }
        else
        {
            meleeAttack = false;
        }

        if (meleeAttack && Input.GetAxis("Vertical") > 0)
        {
            anim.SetTrigger("AttackUp");
            meleeAnimator.SetTrigger("UpwardMeleeSwipe");
        }

        if (meleeAttack && Input.GetAxis("Vertical") < 0 && !playerMovement.isGrounded)
        {
            anim.SetTrigger("AttackDwn");
            meleeAnimator.SetTrigger("DownwardMeleeSwipe");
        }

        if ((meleeAttack && Input.GetAxis("Vertical") == 0) || meleeAttack && (Input.GetAxis("Vertical") < 0 && playerMovement.isGrounded))
        {
            anim.SetTrigger("AttackFwd");
            meleeAnimator.SetTrigger("ForwardMeleeSwipe");
        }
    }
}