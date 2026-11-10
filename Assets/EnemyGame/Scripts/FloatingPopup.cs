using UnityEngine;

// text that flies up and disappears ("В спину!")
public class FloatingPopup : MonoBehaviour
{
    [SerializeField] TextMesh text;
    [SerializeField] float speed = 1.2f;
    [SerializeField] float lifetime = 0.9f;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;
        transform.position += Vector3.up * speed * Time.deltaTime;

        Color color = text.color;
        color.a = 1f - timer / lifetime;
        text.color = color;

        if (timer >= lifetime) Destroy(gameObject);
    }
}
