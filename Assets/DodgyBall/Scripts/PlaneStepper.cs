using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

// Super-simple plane stepper: load (or compute) a list of swing axes, draw one plane, step with arrow keys.
public class PlaneStepper : MonoBehaviour
{
    [Header("Refs")] public Transform target;

    [Header("Source")]
    public bool loadFromFile = true;
    public string binPath = "Assets/planes.bin"; // expects PlaneSet: set.normal + planes[i].origin (axis)
    public Vector3 planeNormal = Vector3.up;       // used only when not loading from file
    [Range(1f, 90f)] public float stepDegrees = 10f;

    [Header("Draw")]
    public Vector3 planeScale = new Vector3(5f, 5f, 0.1f); // give it thickness so it stays visible
    public Color planeColor = new Color(0f, 1f, 0f, 0.35f);

    private readonly List<Vector3> axes = new List<Vector3>();
    private int index;
    private GameObject plane;
    private Vector3 sharedNormal = Vector3.up;

    private void OnEnable()
    {
        Rebuild();
        Draw();
    }

    private void OnDisable()
    {
        if (plane) DestroyImmediate(plane);
    }

    private void Update()
    {
        if (axes.Count == 0) return;
        if (Input.GetKeyDown(KeyCode.RightArrow)) { index = (index + 1) % axes.Count; Draw(); }
        if (Input.GetKeyDown(KeyCode.LeftArrow))  { index = (index - 1 + axes.Count) % axes.Count; Draw(); }
    }

    [ContextMenu("Rebuild")] public void Rebuild()
    {
        axes.Clear();
        index = 0;

        if (loadFromFile && File.Exists(binPath))
        {
            try
            {
                var set = PlaneSet.Load(binPath);
                sharedNormal = set.normal.sqrMagnitude > 0f ? set.normal.normalized : Vector3.up;
                foreach (var p in set.planes) axes.Add(p.origin.normalized);
                if (axes.Count > 0) return;
            }
            catch (Exception) { /* fall back to compute */ }
        }

        // Compute a simple 360° set around the current aim
        sharedNormal = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.up;
        Vector3 aim = GetAimOnPlane(sharedNormal);
        if (aim.sqrMagnitude < 1e-8f) aim = Vector3.forward;
        Vector3 baseAxis = Vector3.Cross(sharedNormal, aim).normalized;
        float step = Mathf.Max(1f, stepDegrees);
        for (float a = 0f; a < 360f; a += step)
        {
            axes.Add((Quaternion.AngleAxis(a, aim) * baseAxis).normalized);
        }
    }

    private Vector3 GetAimOnPlane(Vector3 normal)
    {
        Vector3 dir = target ? (target.position - transform.position) : transform.forward;
        return Vector3.ProjectOnPlane(dir, normal).normalized;
    }

    private void Draw()
    {
        if (axes.Count == 0) return;
        if (!plane)
        {
            plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plane.name = "SwingPlane";
            var r = plane.GetComponent<Renderer>();
            var s = Shader.Find("Unlit/Color");
            if (s == null) s = Shader.Find("Sprites/Default");
            r.material = new Material(s);
            if (r.material.HasProperty("_Color")) r.material.SetColor("_Color", planeColor);
            plane.layer = 0;
        }

        Vector3 aim = GetAimOnPlane(sharedNormal);
        if (aim.sqrMagnitude < 1e-8f) aim = Vector3.forward;
        Vector3 axis = axes[index];
        // Ensure axis is perpendicular to aim (robust against loaded data)
        axis = Vector3.ProjectOnPlane(axis, aim).normalized;
        if (axis.sqrMagnitude < 1e-8f) axis = Vector3.Cross(sharedNormal, aim).normalized;

        plane.transform.position = transform.position;
        plane.transform.localScale = planeScale; // has thickness already

        // Orientation: forward = axis (plane normal), up = cross(axis, aim)
        Vector3 up = Vector3.Cross(axis, aim).normalized;
        if (up.sqrMagnitude < 1e-8f) up = sharedNormal; // fallback
        plane.transform.rotation = Quaternion.LookRotation(axis, up);
    }
}