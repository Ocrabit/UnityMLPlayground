using System;
using System.Collections;
using Google.Protobuf.WellKnownTypes;
using UnityEngine;
using Random = UnityEngine.Random;

public class editSword : MonoBehaviour
{
    public float AttackRange = .5f;
    public float hitAngle = 75f;
    [Range(0f, 1f)] public float pointOfContact = 0.25f;
    public float arcLength = 150f;
    public float Duration = 1.5f;
    
    public GameObject target;

    private Rigidbody _rb;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (!_rb) Debug.LogWarning("No rigidbody found");
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!target) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(PeformVelocitySwing(Duration, target.transform.localPosition));
        }
    }
    
    // Position Based
    // private IEnumerator PerformNonRigidBodySwing(float duration, Vector3 targetPosition)
    // {
    //     float timeToMove = (HitAngle / ArcLength) * duration;
    //     float distance = Vector3.Distance(transform.localPosition, targetPosition) - AttackRange;
    //     Vector3 direction = (targetPosition - transform.localPosition).normalized;
    //     
    //     Vector3 positionalVelocity = direction * (distance / timeToMove);  // Handle local
    //     float offsetVelocity = (Random.value * 360) / timeToMove;  // Handle world
    //     float swingVelocity = ArcLength / duration;  // Handle local
    //     
    //     float elapsedTime = 0f;
    //     while (elapsedTime < duration)
    //     {
    //         if (elapsedTime < timeToMove)
    //         {
    //             transform.localPosition += positionalVelocity * Time.deltaTime;
    //             transform.Rotate(Vector3.right, offsetVelocity * Time.deltaTime, Space.Self);
    //         }
    //         transform.Rotate(Vector3.forward, swingVelocity * Time.deltaTime, Space.Self);
    //         
    //         elapsedTime += Time.deltaTime;
    //         yield return null;
    //     }
    // }
    
    // Quaternion based
    // private IEnumerator PerformRigidBodySwing(float duration, Vector3 targetPosition)
    // {
    //     // Time by which the weapon needs to be at hit point along arc
    //     Debug.Log($"duration is {duration}");
    //     float timeToContact = pointOfContact * duration;
    //     Debug.Log($"making contact at {timeToContact} seconds");
    //     float timeFromContact = duration - timeToContact;
    //     Debug.Log($"Following through for {timeFromContact} seconds");
    //     
    //     // Linear Position
    //     float distance = Vector3.Distance(transform.localPosition, targetPosition) - AttackRange;
    //     Vector3 direction = (targetPosition - transform.localPosition).normalized;
    //     Debug.DrawRay(transform.localPosition, direction * distance, Color.red, 5f);
    //     Vector3 linearVelocity = direction * (distance / timeToContact);
    //     
    //     // Rotations
    //     Quaternion startRot = _rb.rotation;
    //     Quaternion alignRot = Quaternion.FromToRotation(transform.right, -direction);
    //     // Debug.Log($"Dot {Mathf.Abs(Vector3.Dot(transform.forward, direction))} and {Mathf.Abs(Vector3.Dot(transform.forward, direction)) < 0.001f}");
    //     // if (Mathf.Abs(Vector3.Dot(transform.forward, direction)) < 0.001f) alignRot = Quaternion.AngleAxis(180f, -direction) * alignRot;
    //     
    //     Quaternion hitRot = alignRot * startRot;
    //     Quaternion followThroughRot = Quaternion.AngleAxis(arcContinuation, hitRot * Vector3.forward) * hitRot;
    //     Quaternion rollRot = Quaternion.AngleAxis(Random.value * 45f, transform.right);
    //     
    //     float elapsed = 0f;
    //     while (elapsed < duration)
    //     {
    //         Quaternion targetRotation;
    //         
    //         if (elapsed < timeToContact)
    //         {
    //             float t = Mathf.Clamp01(elapsed / timeToContact);
    //             targetRotation = Quaternion.Slerp(startRot, hitRot, t);
    //         }
    //         else
    //         {
    //             float t = Mathf.Clamp01((elapsed - timeToContact) / timeFromContact);
    //             targetRotation = Quaternion.Slerp(hitRot, followThroughRot, t);
    //         }
    //         // Build Quaternions
    //         // Quaternion qRoll =  Quaternion.Slerp(Quaternion.identity, rollRot, alignT);
    //         
    //         Quaternion delta = targetRotation * Quaternion.Inverse(_rb.rotation);
    //         delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
    //         if (angleDeg > 180f) angleDeg -= 360f;
    //         
    //         if (Mathf.Abs(angleDeg) < 0.001f) _rb.angularVelocity = Vector3.zero;
    //         else _rb.angularVelocity = axis.normalized * (angleDeg * Mathf.Deg2Rad / Time.fixedDeltaTime);
    //         
    //         // linear movement
    //         _rb.linearVelocity = elapsed < timeToContact ? linearVelocity : Vector3.zero;
    //
    //         elapsed += Time.fixedDeltaTime;
    //         yield return new WaitForFixedUpdate();
    //     }
    //     
    //     // Stop swing
    //     _rb.linearVelocity = Vector3.zero;
    //     _rb.angularVelocity = Vector3.zero;
    // }
    
       
    private float timer = 0f;
    private float expectedHitTime = 0f;
    
    private IEnumerator PeformVelocitySwing(float duration, Vector3 targetPosition)
    {
        timer = 0f; 
        
        // Linear Position
        float distance = Vector3.Distance(transform.localPosition, targetPosition) - AttackRange; 
        Vector3 direction = (targetPosition - transform.localPosition).normalized; 
        
        // Solve for contactAngle
        Vector3 swingNormal = Vector3.forward;
        Vector3 bladePlane  = Vector3.ProjectOnPlane(transform.right,  swingNormal).normalized;
        Vector3 targetPlane = Vector3.ProjectOnPlane(direction, swingNormal).normalized;
        float totalContactAngle  = Vector3.SignedAngle(bladePlane, targetPlane, swingNormal);
        
        // Rotations
        Quaternion startRot = _rb.rotation;
        Quaternion alignRot = Quaternion.FromToRotation(transform.right, -direction);
        // Debug.Log($"Dot {Mathf.Abs(Vector3.Dot(transform.forward, direction))} and {Mathf.Abs(Vector3.Dot(transform.forward, direction)) < 0.001f}");
        // if (Mathf.Abs(Vector3.Dot(transform.forward, direction)) < 0.001f) alignRot = Quaternion.AngleAxis(180f, -direction) * alignRot;

        // Get alignment angle contribution
        Vector3 alignedPlaneDir = Vector3.ProjectOnPlane(alignRot * transform.right, swingNormal).normalized;
        float alignmentAngle = Vector3.SignedAngle(bladePlane, alignedPlaneDir, swingNormal);
        
        float contactAngle = totalContactAngle - alignmentAngle;
        
        // Time needed to reach contact point in the swing arc
        float timeToContact = (contactAngle / arcLength) * duration; 
        expectedHitTime = timeToContact;
        
        Debug.Log($"Total: {totalContactAngle}°, Align: {alignmentAngle}°, Swing: {contactAngle}°");
        Debug.Log($"Expected contact at {timeToContact}s");
        
        // Linear velocity to get into range
        Vector3 linearVelocity = direction * (distance / timeToContact);
        
        // Quaternion rollRot = Quaternion.AngleAxis(Random.value * 45f, transform.right);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float alignT = Mathf.Clamp01(elapsed / timeToContact);
            float swingT = Mathf.Clamp01(elapsed / duration);
            
            Quaternion qAlign = Quaternion.Slerp(Quaternion.identity, alignRot, alignT);
            Quaternion qSwing = Quaternion.AngleAxis(arcLength * swingT, Vector3.forward);
            // Quaternion qRoll =  Quaternion.Slerp(Quaternion.identity, rollRot, alignT);
            
            Quaternion targetRotation = qAlign * startRot * qSwing;
            
            Quaternion delta = targetRotation * Quaternion.Inverse(_rb.rotation);
            delta.ToAngleAxis(out float angleDeg, out Vector3 axis);
            if (angleDeg > 180f) angleDeg -= 360f;
            
            if (Mathf.Abs(angleDeg) < 0.001f) _rb.angularVelocity = Vector3.zero;
            else _rb.angularVelocity = axis.normalized * (angleDeg * Mathf.Deg2Rad / Time.fixedDeltaTime);
            
            // linear movement
            _rb.linearVelocity = elapsed < timeToContact ? linearVelocity : Vector3.zero;

            elapsed += Time.fixedDeltaTime;
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        
        // Stop swing
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("making contact");
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Mathf.Abs(timer - expectedHitTime) < 0.02f)
            {
                Debug.Log($"Accurate Expected Hit Time");
            }
            Debug.Log($"Timer and expectedHitTime: {timer} / {expectedHitTime}");
        }
    }
    
}
