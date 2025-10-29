using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class CustomSwing : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;
    [Header("Swing")]
    public Vector3 planeNormal = Vector3.up;
    [Range(0f, 360f)] public float arcLength = 150f;
    public float duration = .35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public float variationOffset = 45f;
    private float swingVariation = 0f; // Between -1 and 1
    
    private Quaternion baseRotation;

    void Start()
    {
        baseRotation = transform.rotation; // meh
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            swingVariation = Random.Range(-variationOffset, variationOffset);
            Swing();
        }
    }

    IEnumerator SwingArc(Quaternion start, Quaternion end)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            transform.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        transform.rotation = end;
    }
    
    void Swing()
    {
        if (!target) return;
        
        Vector3 normal = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.up;
        
        Vector3 direction = target.position - transform.position;
        Vector3 directionOnPlane = Vector3.ProjectOnPlane(direction, normal);
        if (directionOnPlane.sqrMagnitude < 1e-8f) return;

        directionOnPlane.Normalize();
        
        // Aim it at normalized plane direction
        Vector3 baseForward = Vector3.ProjectOnPlane(baseRotation * Vector3.forward, normal).normalized;
        float angleToTarget = Vector3.SignedAngle(baseForward, directionOnPlane, normal);

        Quaternion aimRotation = Quaternion.AngleAxis(angleToTarget, normal) * baseRotation;
        Vector3 verticalSwingAxis = Vector3.Cross(normal, directionOnPlane).normalized;
        
        // Add variation angle
        Quaternion offsetRotation = Quaternion.AngleAxis(swingVariation, directionOnPlane);
        
        // Build the swing at aim
        Quaternion start = offsetRotation * baseRotation;;
        Quaternion end = Quaternion.AngleAxis(arcLength, verticalSwingAxis) * aimRotation * offsetRotation;
    
        StopAllCoroutines();
        StartCoroutine(SwingArc(start, end));
    }
    
    void OnDrawGizmos()
    {
        if (!target) return;

        Vector3 n = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.up;
        Vector3 dir = target.position - transform.position;
        Vector3 dirOnPlane = Vector3.ProjectOnPlane(dir, n);

        float len = 1.5f, thick = 0.08f;

        Gizmos.color = Color.red;
        if (dir.sqrMagnitude > 1e-8f) DrawThickLine(transform.position, dir.normalized * len, thick);

        Gizmos.color = Color.green;
        if (dirOnPlane.sqrMagnitude > 1e-8f) DrawThickLine(transform.position, dirOnPlane.normalized * len, thick);
    }

    void DrawThickLine(Vector3 start, Vector3 dir, float thickness)
    {
        Vector3 mid = start + dir * 0.5f;
        float length = dir.magnitude;
        if (length < 1e-6f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        Gizmos.matrix = Matrix4x4.TRS(mid, rot, new Vector3(thickness, thickness, length));
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }
}
