using UnityEngine;

public class SwingDriver : MonoBehaviour
{
    public Camera cam;
    public Transform origin;            // usually player/root
    public DirectedArcSwing sword;

    public LayerMask groundMask = ~0;   // set to your ground

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Raycast mouse to ground, swing at that XYZ
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 1000f, groundMask))
                sword.SwingAt(hit.point);
            else
                sword.SwingTowards(origin.forward);
        }
    }
}