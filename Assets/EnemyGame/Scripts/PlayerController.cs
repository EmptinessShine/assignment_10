using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 4.5f;
    [SerializeField] VirtualJoystick joystick;

    [Header("Attack")]
    [SerializeField] float attackRadius = 1.3f;
    [SerializeField] int attackDamage = 1;
    [SerializeField] float attackCooldown = 0.35f;
    [SerializeField] int backstabDamage = 10; // hit from behind = instant kill

    [Header("Visuals")]
    [SerializeField] CharacterVisual visual;
    [SerializeField] SpriteRenderer body;
    [SerializeField] GameObject slashEffect;
    [SerializeField] FloatingPopup popupPrefab;
    [SerializeField] AudioClip slashSound;
    [SerializeField] AudioClip hitSound;
    [SerializeField] AudioClip backstabSound;

    public Health health;

    Rigidbody2D rb;
    Vector2 lookDirection = Vector2.right;
    float attackTimer;

    void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (IsDead() || GameManager.instance.levelFinished)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 input = joystick.value + KeyboardInput();
        input = Vector2.ClampMagnitude(input, 1f);
        rb.linearVelocity = input * moveSpeed;

        if (input != Vector2.zero)
        {
            lookDirection = input.normalized;
            visual.LookAt(lookDirection);
        }

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            Attack();
    }

    Vector2 KeyboardInput()
    {
        Vector2 v = Vector2.zero;
        if (Keyboard.current == null) return v;
        if (Keyboard.current.aKey.isPressed) v.x -= 1;
        if (Keyboard.current.dKey.isPressed) v.x += 1;
        if (Keyboard.current.sKey.isPressed) v.y -= 1;
        if (Keyboard.current.wKey.isPressed) v.y += 1;
        return v;
    }

    public void Attack()
    {
        if (IsDead() || attackTimer > 0) return;
        attackTimer = attackCooldown;

        float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;
        Instantiate(slashEffect, (Vector2)transform.position + lookDirection * 0.6f, Quaternion.Euler(0, 0, angle));
        visual.Lunge(lookDirection);
        Sound.Play(slashSound, 0.8f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRadius);
        foreach (Collider2D hit in hits)
        {
            EnemyBase enemy = hit.GetComponent<EnemyBase>();
            if (enemy == null) continue;

            // backstab: enemy is calm and looks away from us
            if (!enemy.IsAlerted() && enemy.IsLookingAwayFrom(transform.position))
            {
                enemy.GetComponent<Health>().TakeDamage(backstabDamage);
                Sound.Play(backstabSound);
                Instantiate(popupPrefab, enemy.transform.position + Vector3.up, Quaternion.identity);
                CameraFollow.instance.Shake(0.2f);
            }
            else
            {
                enemy.GetComponent<Health>().TakeDamage(attackDamage);
                Sound.Play(hitSound, 0.8f);
            }
        }
    }

    public bool IsDead()
    {
        return health.currentHealth <= 0;
    }

    void OnDamaged()
    {
        CameraFollow.instance.Shake(0.15f);
    }

    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        body.color = Color.gray;
        GameManager.instance.PlayerDied();
    }
}
