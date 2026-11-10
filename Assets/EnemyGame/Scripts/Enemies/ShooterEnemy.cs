using UnityEngine;

// Guard -> Aim -> Shoot -> Cooldown -> Aim / Guard ; alarm: Guard -> Investigate
public class ShooterEnemy : EnemyBase
{
    enum State { Guard, Investigate, Aim, Shoot, Cooldown }

    [Header("Shooter")]
    [SerializeField] float scanSpeed = 50f;     // degrees per second
    [SerializeField] float scanTime = 2f;       // turn one way for this long, then the other way
    [SerializeField] float aimTime = 0.6f;      // red laser before the shot
    [SerializeField] float aimLockTime = 0.2f;  // last part of aiming: stops following you
    [SerializeField] float cooldownTime = 1.2f;
    [SerializeField] float bulletSpeed = 7f;
    [SerializeField] int damage = 1;
    [SerializeField] float investigateTime = 6f;
    [SerializeField] Projectile bulletPrefab;
    [SerializeField] LineRenderer aimLine;
    [SerializeField] LayerMask wallMask;
    [SerializeField] GameObject muzzleFlash;
    [SerializeField] AudioClip shotSound;

    State state;
    string[] stateNames = { "Охрана", "Проверяет", "Целится", "Выстрел", "Перезарядка" };
    Vector2 aimDirection;
    float scanTimer;
    int scanDirection = 1;

    protected override void Start()
    {
        base.Start();
        ChangeState(State.Guard);
    }

    public override bool IsAlerted()
    {
        return state != State.Guard;
    }

    public override void HearAlarm(Vector2 position)
    {
        vision.lastSeenPosition = position;
        ChangeState(State.Investigate);
    }

    void Update()
    {
        stateTimer -= Time.deltaTime;

        switch (state)
        {
            case State.Guard:
                Scan();
                if (CanSeePlayer())
                {
                    RaiseAlarm();
                    ChangeState(State.Aim);
                }
                break;

            case State.Investigate:
                if (CanSeePlayer())
                    ChangeState(State.Aim);
                else if (!Reached(vision.lastSeenPosition))
                    MoveTo(vision.lastSeenPosition, moveSpeed);
                else
                    LookAround();

                if (stateTimer <= 0) ChangeState(State.Guard);
                break;

            case State.Aim:
                StopMoving();
                if (stateTimer > aimLockTime)
                {
                    if (!CanSeePlayer())
                    {
                        ChangeState(State.Guard); // player hid behind a wall
                        break;
                    }
                    aimDirection = DirectionToPlayer();
                    LookAt(aimDirection);
                }
                DrawAimLine();
                if (stateTimer <= 0) ChangeState(State.Shoot);
                break;

            case State.Shoot:
                Shoot();
                ChangeState(State.Cooldown);
                break;

            case State.Cooldown:
                if (stateTimer <= 0)
                {
                    if (CanSeePlayer()) ChangeState(State.Aim);
                    else ChangeState(State.Guard);
                }
                break;
        }
    }

    void Scan()
    {
        StopMoving();
        scanTimer += Time.deltaTime;
        if (scanTimer > scanTime)
        {
            scanTimer = 0;
            scanDirection = -scanDirection;
        }
        LookAt(Quaternion.Euler(0, 0, scanSpeed * scanDirection * Time.deltaTime) * vision.facing);
    }

    void Shoot()
    {
        Vector2 gunPosition = (Vector2)transform.position + aimDirection * 0.6f;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        Instantiate(muzzleFlash, gunPosition, Quaternion.Euler(0, 0, angle));
        Projectile bullet = Instantiate(bulletPrefab, gunPosition, Quaternion.identity);
        bullet.Launch(aimDirection, bulletSpeed, damage);

        visual.Lunge(-aimDirection); // recoil
        Sound.Play(shotSound, 0.7f);
    }

    void DrawAimLine()
    {
        Vector2 start = transform.position;
        Vector2 end = start + aimDirection * 15f;
        RaycastHit2D hit = Physics2D.Raycast(start, aimDirection, 15f, wallMask);
        if (hit.collider != null) end = hit.point;

        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    void ChangeState(State newState)
    {
        state = newState;
        SetLabel(stateNames[(int)newState]);
        aimLine.enabled = false;
        telegraph.Hide();
        ShowIcon(null);

        if (newState == State.Investigate)
        {
            stateTimer = investigateTime;
            ShowIcon(searchIcon);
        }
        if (newState == State.Aim)
        {
            stateTimer = aimTime;
            aimDirection = DirectionToPlayer();
            aimLine.enabled = true;
            telegraph.Show();
            ShowIcon(alertIcon);
        }
        if (newState == State.Cooldown)
        {
            stateTimer = cooldownTime;
            ShowIcon(alertIcon);
        }
    }
}
