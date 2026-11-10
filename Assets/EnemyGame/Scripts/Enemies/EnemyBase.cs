using System.Collections.Generic;
using UnityEngine;

// common things for all enemies. every enemy type has its own state machine.
public abstract class EnemyBase : MonoBehaviour
{
    public static List<EnemyBase> allEnemies = new List<EnemyBase>();

    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected Transform[] waypoints;
    [SerializeField] float backstabAngle = 110f;

    [Header("Visuals")]
    [SerializeField] protected CharacterVisual visual;
    [SerializeField] protected SpriteRenderer icon;
    [SerializeField] protected Sprite alertIcon;
    [SerializeField] protected Sprite searchIcon;
    [SerializeField] protected Sprite stunIcon;
    [SerializeField] protected TextMesh stateLabel;

    protected Rigidbody2D rb;
    protected VisionSensor vision;
    protected Telegraph telegraph;
    protected Transform player;
    protected float stateTimer;

    int waypointIndex;

    // true when the enemy knows about the player (fighting or searching)
    public abstract bool IsAlerted();

    // another enemy saw the player at this position
    public abstract void HearAlarm(Vector2 position);

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        vision = GetComponent<VisionSensor>();
        telegraph = GetComponent<Telegraph>();
        GetComponent<Health>().Died += Die;
        allEnemies.Add(this);
    }

    protected virtual void Start()
    {
        player = PlayerController.instance.transform;
    }

    void OnDestroy()
    {
        allEnemies.Remove(this);
    }

    // used by the random level
    public void Setup(Transform[] route, Vector2 lookDirection)
    {
        waypoints = route;
        LookAt(lookDirection);
    }

    protected bool CanSeePlayer()
    {
        if (PlayerController.instance.IsDead()) return false;
        if (!IsOnScreen()) return false; // no attacks from off-screen
        return vision.CanSee(player);
    }

    bool IsOnScreen()
    {
        Vector3 p = Camera.main.WorldToViewportPoint(transform.position);
        return p.x > 0 && p.x < 1 && p.y > 0 && p.y < 1;
    }

    protected float DistanceToPlayer()
    {
        return Vector2.Distance(transform.position, player.position);
    }

    protected Vector2 DirectionToPlayer()
    {
        return (player.position - transform.position).normalized;
    }

    protected void MoveTo(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * speed;
        LookAt(direction);
    }

    protected void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
    }

    protected void LookAt(Vector2 direction)
    {
        if (direction == Vector2.zero) return;
        vision.facing = direction.normalized;
        visual.LookAt(direction);
    }

    protected void LookAround()
    {
        StopMoving();
        LookAt(Quaternion.Euler(0, 0, 150f * Time.deltaTime) * vision.facing);
    }

    protected bool Reached(Vector2 target)
    {
        return Vector2.Distance(transform.position, target) < 0.4f;
    }

    protected void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            StopMoving();
            return;
        }

        Vector2 target = waypoints[waypointIndex].position;
        if (Reached(target))
            waypointIndex = (waypointIndex + 1) % waypoints.Length;

        MoveTo(target, moveSpeed);
    }

    protected void RaiseAlarm()
    {
        AlertSystem.instance.RaiseAlarm(this, player.position);
    }

    protected void ShowIcon(Sprite sprite)
    {
        icon.sprite = sprite;
        icon.enabled = sprite != null;
    }

    protected void SetLabel(string text)
    {
        stateLabel.text = text;
    }

    // for backstab: is the attacker behind this enemy?
    public bool IsLookingAwayFrom(Vector2 position)
    {
        Vector2 toAttacker = position - (Vector2)transform.position;
        return Vector2.Angle(vision.facing, toAttacker) > backstabAngle;
    }

    void Die()
    {
        Destroy(gameObject);
    }
}
