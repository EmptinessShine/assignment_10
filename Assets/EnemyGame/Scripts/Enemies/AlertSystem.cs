using UnityEngine;

// when one enemy sees the player: quiet siren + the closest calm enemies run to that place
public class AlertSystem : MonoBehaviour
{
    public static AlertSystem instance;

    [SerializeField] AudioClip sirenSound;
    [SerializeField] float sirenVolume = 0.25f;
    [SerializeField] float sirenCooldown = 5f;
    [SerializeField] float alertRadius = 12f;
    [SerializeField] int helpersCount = 2;

    float sirenTimer;

    void Awake()
    {
        instance = this;
    }

    void Update()
    {
        sirenTimer -= Time.deltaTime;
    }

    public void RaiseAlarm(EnemyBase caller, Vector2 playerPosition)
    {
        if (sirenTimer <= 0)
        {
            Sound.Play(sirenSound, sirenVolume);
            sirenTimer = sirenCooldown;
        }

        for (int i = 0; i < helpersCount; i++)
        {
            EnemyBase closest = FindClosestCalmEnemy(caller);
            if (closest == null) break;
            closest.HearAlarm(playerPosition); // after this it is alerted, so next loop finds another one
        }
    }

    EnemyBase FindClosestCalmEnemy(EnemyBase caller)
    {
        EnemyBase closest = null;
        float closestDistance = alertRadius;

        foreach (EnemyBase enemy in EnemyBase.allEnemies)
        {
            if (enemy == caller || enemy.IsAlerted()) continue;

            float distance = Vector2.Distance(caller.transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }
}
