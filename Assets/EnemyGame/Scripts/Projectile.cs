using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float lifetime = 3f;
    [SerializeField] LayerMask wallMask;
    [SerializeField] GameObject impactEffect;

    int damage;

    public void Launch(Vector2 direction, float speed, int bulletDamage)
    {
        damage = bulletDamage;
        GetComponent<Rigidbody2D>().linearVelocity = direction * speed;
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            PlayerController.instance.health.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (((1 << other.gameObject.layer) & wallMask) != 0)
        {
            Instantiate(impactEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
