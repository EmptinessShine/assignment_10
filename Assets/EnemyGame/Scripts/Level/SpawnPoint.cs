using UnityEngine;

// put this in a room prefab: an enemy can appear here.
// the red arrow (transform.right) is where a shooter will look.
public class SpawnPoint : MonoBehaviour
{
    public Transform[] route; // patrol points for patroller / charger

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
        Gizmos.DrawLine(transform.position, transform.position + transform.right);
    }
}
