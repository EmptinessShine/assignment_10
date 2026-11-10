using UnityEngine;

// plays sprites one after another, then destroys the object (slash, sparks, muzzle flash)
public class SpriteFrameAnimation : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 20f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        int frame = (int)(timer * fps);

        if (frame >= frames.Length)
        {
            Destroy(gameObject);
            return;
        }
        spriteRenderer.sprite = frames[frame];
    }
}
