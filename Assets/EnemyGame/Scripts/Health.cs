using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public int currentHealth;

    [SerializeField] float invincibleTime = 0.5f;
    [SerializeField] SpriteRenderer body;
    [SerializeField] GameObject hitEffect;
    [SerializeField] GameObject deathEffect;
    [SerializeField] AudioClip hurtSound;
    [SerializeField] AudioClip deathSound;

    public event Action Damaged;
    public event Action Died;

    float invincibleTimer;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        invincibleTimer -= Time.deltaTime;
    }

    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0 || invincibleTimer > 0) return;

        currentHealth -= damage;
        invincibleTimer = invincibleTime;

        if (hitEffect != null) Instantiate(hitEffect, transform.position, Quaternion.identity);
        if (body != null) StartCoroutine(Flash());

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            if (deathEffect != null) Instantiate(deathEffect, transform.position, Quaternion.identity);
            Sound.Play(deathSound);
            if (Died != null) Died();
        }
        else
        {
            Sound.Play(hurtSound);
            if (Damaged != null) Damaged();
        }
    }

    IEnumerator Flash()
    {
        body.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (currentHealth > 0) body.color = Color.white;
    }
}
