using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways] // temp
public class NormalSword : MonoBehaviour
{
    [Header("Editor Preview")]
    public bool autoAimInEditor = true;
    
    [Header("Refs")]
    public Transform target;
    
    [Header("Swing")]
    public Vector3 planeNormal = Vector3.up;
    [Range(0f, 360f)] public float arcLength = 150f;
    public float duration = .35f;
    public float MIN_DURATION = .001f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public float variationOffset = 45f;
    private float swingVariation = 0f; // Between -variationOffset and +variationOffset

    [Header("Weapon Type")] public string weaponType = "katana";

    public readonly Quaternion weaponAdjustment = Quaternion.Euler(-90, -90, 0);
    private Action attackFunc;

    private void Update()
    {
        if (autoAimInEditor)
        {
            Reorient();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Swing();
        }
    }
    
    private (Vector3 normal, Vector3 targetDir) Reorient()
    {
        if (!target) return (Vector3.up, Vector3.forward);

        Vector3 normal = planeNormal.sqrMagnitude > 0f ? planeNormal.normalized : Vector3.up;
        Vector3 direction = target.position - transform.position;
        Vector3 directionOnPlane = Vector3.ProjectOnPlane(direction, normal);

        if (directionOnPlane.sqrMagnitude < 1e-8f) return (normal, Vector3.forward);
        
        transform.rotation = PlanarRotation(directionOnPlane, normal);
        
        return (normal, directionOnPlane);
    }

    private Quaternion PlanarRotation(Vector3 planarDirection, Vector3 normal, bool useCorrection = true)
    {
        planarDirection.Normalize();
        if (useCorrection) return Quaternion.LookRotation(planarDirection, normal) * weaponAdjustment;
        return Quaternion.LookRotation(planarDirection, normal);
    }

    IEnumerator SwingArc(Quaternion start, Quaternion end)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(MIN_DURATION, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            transform.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        transform.rotation = end;
    }
    
    public void Swing()
    {
        if (!target) return;
    
        var (normal, targetDirection) = Reorient(); // Orient and get both values
    
        // Use the actual target direction, not transform.forward
        Vector3 baseSwingAxis = Vector3.Cross(normal, targetDirection).normalized;
    
        // Add variation using the target direction
        swingVariation = Random.Range(-variationOffset, variationOffset);
        Quaternion offsetRotation = Quaternion.AngleAxis(swingVariation, targetDirection);
        Vector3 variedSwingAxis = offsetRotation * baseSwingAxis;
        
        // Debug
        // DrawSwingPlane(variedSwingAxis, targetDirection);
        
        // Swing from current rotation
        Quaternion swordPositioning = (variedSwingAxis.x < 0f) ? Quaternion.Euler(180f, 0f, -90f) : Quaternion.Euler(0f, 0f, -90f);
        Quaternion start = PlanarRotation(variedSwingAxis, normal) * swordPositioning;
        Quaternion end = Quaternion.AngleAxis(arcLength, variedSwingAxis) * start;

        StopAllCoroutines();
        StartCoroutine(SwingArc(start, end));
    }

    public void Swing(float duration)
    {
        this.duration = Mathf.Max(MIN_DURATION, duration);
        Swing();
    }

    public void TestFullSwingVariations(float step = 5f)
    {
        if (!target)
        {
            Debug.LogWarning("No target assigned.");
            return;
        }
    
        var (normal, targetDirection) = Reorient();
        Vector3 baseSwingAxis = Vector3.Cross(normal, targetDirection).normalized;
    
        var planeList = new System.Collections.Generic.List<PlaneBuilder>();
    
        for (float angle = 0f; angle < 360f; angle += step)
        {
            Quaternion rot = Quaternion.AngleAxis(angle, targetDirection);
            Debug.Log($"Angle step: {angle} / 360");
            var variedSwingAxis = rot * baseSwingAxis;
            planeList.Add(new PlaneBuilder(variedSwingAxis));
        }
    
        var planes = planeList.ToArray();
        var set = new PlaneSet(normal, planes);
        string path = "Assets/planes.bin";
        PlaneSet.Save(path, set);
    
        Debug.Log($"Saved {planes.Length} plane variations to {path} with normal {normal}.");
    }
    
    private GameObject debugPlane;
    private void DrawSwingPlane(Vector3 swingAxis, Vector3 targetDir)
    {
        // Clean up old plane
        if (debugPlane) Destroy(debugPlane);

        debugPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debugPlane.name = "SwingPlane";

        debugPlane.transform.localScale = new Vector3(1f, 1f, 0.001f);
        debugPlane.transform.position = transform.position;

        // Orient the plane - swing axis is the plane's normal
        // Vector3 planeRight = targetDir;
        Vector3 planeUp = Vector3.Cross(swingAxis, targetDir).normalized;
        debugPlane.transform.rotation = Quaternion.LookRotation(swingAxis, planeUp);

        // Make semi-transparent
        var renderer = debugPlane.GetComponent<Renderer>();
        renderer.material.color = new Color(0, 1, 0, 0.15f); // Green, semi-transparent
    }
}
