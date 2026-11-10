using System.Collections;
using UnityEngine;

// turns the sprite where the character looks + simple walk animation
public class CharacterVisual : MonoBehaviour
{
    [SerializeField] Transform body;
    [SerializeField] SpriteRenderer bodyRenderer;
    [SerializeField] Sprite[] walkFrames; // frame 0 = standing
    [SerializeField] float walkFps = 8f;

    Rigidbody2D rb;
    float walkTimer;
    int frame;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (rb.linearVelocity.magnitude > 0.2f)
        {
            walkTimer += Time.deltaTime;
            if (walkTimer > 1f / walkFps)
            {
                walkTimer = 0;
                frame = (frame + 1) % walkFrames.Length;
                bodyRenderer.sprite = walkFrames[frame];
            }
        }
        else
        {
            bodyRenderer.sprite = walkFrames[0];
        }
    }

    public void LookAt(Vector2 direction)
    {
        if (direction == Vector2.zero) return;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        body.rotation = Quaternion.Euler(0, 0, angle);
    }

    // quick move forward and back (melee hit, or backwards for a shot)
    public void Lunge(Vector2 direction)
    {
        StartCoroutine(LungeRoutine(direction));
    }

    IEnumerator LungeRoutine(Vector2 direction)
    {
        body.localPosition = direction.normalized * 0.3f;
        yield return new WaitForSeconds(0.1f);
        body.localPosition = Vector3.zero;
    }

    public void Shake(float time)
    {
        StartCoroutine(ShakeRoutine(time));
    }

    IEnumerator ShakeRoutine(float time)
    {
        float endTime = Time.time + time;
        while (Time.time < endTime)
        {
            body.localPosition = Random.insideUnitCircle * 0.07f;
            yield return null;
        }
        body.localPosition = Vector3.zero;
    }
}
