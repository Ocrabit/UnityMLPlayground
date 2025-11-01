using System.Collections;
using UnityEngine;

public class EfficientSword : MonoBehaviour
{
    [Header("Refs")]
    public Transform target;
    public string binPath = "Assets/DodgyBall/data/sword_planes.bin";
    private Vector3 planeNormal;
    private Vector3[] planes;
    
    [Header("Swing")]
    [Range(0f, 360f)] public float arcLength = 150f;
    public float duration = .35f;
    public float MIN_DURATION = .001f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    
    public readonly Quaternion weaponAdjustment = Quaternion.Euler(-90, -90, 0);
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Precomp orientation
        Reorient();
        
        // Load sword planes
        var planeSet = PlaneSet.Load(binPath);
        planeNormal = planeSet.normal;
        planes = new Vector3[planeSet.planes.Length];
        for (int i = 0; i < planeSet.planes.Length; i++)
            planes[i] = planeSet.planes[i].origin;
        
        Debug.Log($"[PlaneStepper] Loaded {planes.Length} planes from '{binPath}' | Normal: {planeNormal}");
    }
    
    private Quaternion PlanarRotation(Vector3 planarDirection, Vector3 normal, bool useCorrection = true)
    {
        planarDirection.Normalize();
        if (useCorrection) return Quaternion.LookRotation(planarDirection, normal) * weaponAdjustment;
        return Quaternion.LookRotation(planarDirection, normal);
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
        
        // int randIndex = Random.Range(0, planes.Length);
        // Vector3 randomPlane = planes[randIndex];
        Vector3 randomPlane = planes[Random.Range(0, planes.Length)];
        // Debug.Log($"Swing {randomPlane.x} | Normal: {randomPlane}");
        
        // DrawSwingPlane(randomPlane, target.position);

        Quaternion swordPositioning = (randomPlane.x < 0f) ? Quaternion.Euler(180f, 0f, -90f) : Quaternion.Euler(0f, 0f, -90f);
        Quaternion start = PlanarRotation(randomPlane, planeNormal) * swordPositioning;
        Quaternion end = Quaternion.AngleAxis(arcLength, randomPlane) * start;
        
        StopAllCoroutines();
        StartCoroutine(SwingArc(start, end));
    }
    
    public void Swing(float duration)
    {
        this.duration = Mathf.Max(MIN_DURATION, duration);
        Swing();
    }
    
    // Old debug funcs, Quaternion Directions are annoying haha
    private GameObject startSword;
    private GameObject endSword;
    void CreateDebugCopy(Quaternion start, Quaternion end)
    {
        // Clean Up
        if (startSword) Destroy(startSword);
        if (endSword) Destroy(endSword);
    
        startSword = Instantiate(gameObject, transform.position, start);
        endSword = Instantiate(gameObject, transform.position, end);
        startSword.name = $"DebugStartSword";
        endSword.name = $"DebugEndSword";

        // DISABLE the script on the copy to prevent infinite loop
        var scriptComponent = startSword.GetComponent<EfficientSword>();
        if (scriptComponent) scriptComponent.enabled = false;
    
        var scriptComponent2 = endSword.GetComponent<EfficientSword>();
        if (scriptComponent2) scriptComponent2.enabled = false;

        // Make translucent
        SetTranslucentColor(startSword, new Color(0, 1, 0, 0.3f));
        SetTranslucentColor(endSword, new Color(1, 0, 0, 0.3f));
    }

    void SetTranslucentColor(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                mat.color = color;
                mat.SetFloat("_Mode", 3); // Transparent
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
            }
        }
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
