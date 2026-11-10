using System.Collections;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow instance;

    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;

    Vector3 shakeOffset;

    void Awake()
    {
        instance = this;
    }

    void LateUpdate()
    {
        if (target == null) return;
        Vector3 goal = new Vector3(target.position.x, target.position.y, -10);
        Vector3 smooth = Vector3.Lerp(transform.position - shakeOffset, goal, smoothSpeed * Time.deltaTime);
        transform.position = smooth + shakeOffset;
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
            shakeOffset = Random.insideUnitCircle * 0.15f;
            yield return null;
        }
        shakeOffset = Vector3.zero;
    }
}
