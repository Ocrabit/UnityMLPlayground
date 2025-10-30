using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[ExecuteAlways] // temp
public class Attacker : MonoBehaviour
{
    [Header("Editor Preview")]
    public bool autoAimInEditor = true;
    
    [Header("Refs")]
    public Transform target;
    
    [Header("Swing")]
    public Vector3 planeNormal = Vector3.up;
    [Range(0f, 360f)] public float arcLength = 150f;
    public float duration = .35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    public float variationOffset = 45f;
    private float swingVariation = 0f; // Between -variationOffset and +variationOffset

    [Header("Weapon Type")] public string weaponType = "katana";

    public readonly Quaternion weaponAdjustment = Quaternion.Euler(-90, -90, 0);
    private Action attackFunc;

    private void Update()
    {
        if (!autoAimInEditor) return;
        Reorient();
    }

    void Start()
    {
        SetAttackFunction();
    }
    
    void OnValidate()
    {
        // Runs in editor when weaponType changes
        SetAttackFunction();
    }
    
    private void SetAttackFunction()
    {
        switch (weaponType.ToLower())
        {
            case "katana":
                attackFunc = Swing;
                break;
            // future weapon types:
            // case "spear": attackFunc = SpearAttack; break;
            // case "axe": attackFunc = AxeAttack; break;
            default:
                attackFunc = DefaultAttack;
                break;
        }
    }
    
    public void Attack()
    {
        attackFunc?.Invoke();
    }
    
    private void DefaultAttack()
    {
        Debug.LogWarning($"{name} has no valid attack for weapon type '{weaponType}'.");
        // Using Swing for default
        Swing();
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
            t += Time.deltaTime / Mathf.Max(0.0001f, duration);
            float k = ease.Evaluate(Mathf.Clamp01(t));
            transform.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        // transform.rotation = end;
        transform.rotation = start;
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
        DrawSwingPlane(variedSwingAxis, targetDirection);
        
        // Swing from current rotation
        Quaternion start = PlanarRotation(variedSwingAxis, normal) * Quaternion.Euler(0f, 0f, -90f);
        Quaternion end = Quaternion.AngleAxis(arcLength, variedSwingAxis) * start;

        StopAllCoroutines();
        StartCoroutine(SwingArc(start, end));
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
