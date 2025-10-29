using System.Collections;
using UnityEngine;

public class DirectedArcSwing : MonoBehaviour
{
    [Header("Arc")]
    public float arcDegrees = 110f;              // total sweep
    public float duration = 0.20f;               // seconds
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public bool clockwise = true;                // flip direction if needed

    [Header("Swing Plane")]
    public Vector3 planeNormal = Vector3.up;

    bool busy;

    /// <summary>
    /// Swing centered toward a target world position.
    /// </summary>
    public void SwingAt(Vector3 targetWorld)
    {
        if (busy) return;

        // Aim direction projected onto swing plane
        Vector3 aimDir = targetWorld - transform.position;
        aimDir = Vector3.ProjectOnPlane(aimDir, planeNormal);
        if (aimDir.sqrMagnitude < 1e-6f) return;

        StartCoroutine(SwingRoutine(aimDir.normalized));
    }

    /// <summary>
    /// Swing centered toward a world direction (already normalized or not).
    /// </summary>
    public void SwingTowards(Vector3 worldDir)
    {
        if (busy) return;

        worldDir = Vector3.ProjectOnPlane(worldDir, planeNormal);
        if (worldDir.sqrMagnitude < 1e-6f) return;

        StartCoroutine(SwingRoutine(worldDir.normalized));
    }

    IEnumerator SwingRoutine(Vector3 aimDirOnPlane)
    {
        busy = true;

        // Base facing: look along aim direction with the plane's normal as "up"
        Quaternion baseFacing = Quaternion.LookRotation(aimDirOnPlane, planeNormal.normalized);

        // Rotation axis is the plane normal (world space)
        Vector3 axis = planeNormal.normalized;

        // Centered arc: -half → +half around axis
        float sign = clockwise ? -1f : 1f;
        Quaternion qStart = Quaternion.AngleAxis(sign * ( arcDegrees * 0.5f), axis);
        Quaternion qEnd   = Quaternion.AngleAxis(sign * (-arcDegrees * 0.5f), axis);

        float t = 0f;
        while (t < duration)
        {
            t = Mathf.Min(t + Time.deltaTime, duration);
            float f = ease.Evaluate(t / duration);

            // Interpolate the offset around the axis, applied about the centered facing
            Quaternion offset = Quaternion.Slerp(qStart, qEnd, f);
            transform.rotation = offset * baseFacing;

            yield return null;
        }

        busy = false;
    }
}