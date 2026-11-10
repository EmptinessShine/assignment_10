using UnityEngine;

// red blinking glow + beep before an attack, so the player can react
public class Telegraph : MonoBehaviour
{
    [SerializeField] SpriteRenderer glow;
    [SerializeField] AudioClip windUpSound;
    [SerializeField] float blinkSpeed = 15f;

    void Awake()
    {
        glow.enabled = false;
    }

    public void Show()
    {
        glow.enabled = true;
        Sound.Play(windUpSound, 0.6f);
    }

    public void Hide()
    {
        glow.enabled = false;
    }

    void Update()
    {
        if (!glow.enabled) return;
        Color c = glow.color;
        c.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        glow.color = c;
    }
}
