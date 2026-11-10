using UnityEngine;

public class ExitZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && !PlayerController.instance.IsDead())
            GameManager.instance.PlayerReachedExit();
    }
}
