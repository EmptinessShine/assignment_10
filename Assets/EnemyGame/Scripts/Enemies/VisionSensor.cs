using UnityEngine;

// distance + angle + raycast (walls block the view)
public class VisionSensor : MonoBehaviour
{
    [SerializeField] float viewDistance = 6f;
    [SerializeField] float viewAngle = 90f;
    [SerializeField] LayerMask wallMask;
    [SerializeField] LineRenderer sightLine; // yellow "V" that shows where the enemy looks

    public Vector2 facing = Vector2.right;
    public Vector2 lastSeenPosition;

    public bool CanSee(Transform target)
    {
        Vector2 toTarget = target.position - transform.position;

        if (toTarget.magnitude > viewDistance) return false;
        if (Vector2.Angle(facing, toTarget) > viewAngle / 2f) return false;
        if (Physics2D.Raycast(transform.position, toTarget.normalized, toTarget.magnitude, wallMask)) return false;

        lastSeenPosition = target.position;
        return true;
    }

    void Update()
    {
        Vector2 left = Quaternion.Euler(0, 0, viewAngle / 2f) * facing;
        Vector2 right = Quaternion.Euler(0, 0, -viewAngle / 2f) * facing;

        sightLine.SetPosition(0, RayEnd(left));
        sightLine.SetPosition(1, transform.position);
        sightLine.SetPosition(2, RayEnd(right));
    }

    Vector2 RayEnd(Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, viewDistance, wallMask);
        if (hit.collider != null) return hit.point;
        return (Vector2)transform.position + direction * viewDistance;
    }
}
