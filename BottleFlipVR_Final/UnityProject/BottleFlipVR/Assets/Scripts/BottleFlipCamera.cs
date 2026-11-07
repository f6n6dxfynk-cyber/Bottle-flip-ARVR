using UnityEngine;

/// <summary>Desktop spectator camera that tracks the course while leaving room for an XR camera to replace it later.</summary>
public sealed class BottleFlipCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float followDistance = 8.2f;
    [SerializeField] private float height = 2.7f;
    [SerializeField] private float lookAhead = 2.4f;
    [SerializeField] private float followSharpness = 4f;

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = new Vector3(0f, height, target.position.z - followDistance);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
        Vector3 lookPoint = target.position + Vector3.up * 0.45f + Vector3.forward * lookAhead;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookPoint - transform.position, Vector3.up), 1f - Mathf.Exp(-followSharpness * Time.deltaTime));
    }
}
