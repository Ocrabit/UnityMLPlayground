using System;
using UnityEngine;

public class AttackHandler : MonoBehaviour
{
    [Header("Weapon Type")] public string weaponType = "katana";
    
    private Action<float> attackFunc;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
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
                var tryEfficientSword = GetComponent<EfficientSword>();
                if (tryEfficientSword)
                {
                    attackFunc = tryEfficientSword.Swing;
                    break;
                }

                var tryNormalSword = GetComponent<NormalSword>();
                if (tryNormalSword)
                {
                    attackFunc = tryNormalSword.Swing;
                    break;
                }
                
                Debug.Log("Neither Efficient or Normal Sword Components exist on this object.");
                break;
            // future weapon types:
            // case "spear": attackFunc = SpearAttack; break;
            // case "axe": attackFunc = AxeAttack; break;
            default:
                attackFunc = DefaultAttack;
                break;
        }
    }
    
    private void DefaultAttack(float duration)
    {
        Debug.LogWarning($"{name} has no valid attack for weapon type '{weaponType}'.");
    }
    
    public void Attack(float duration)
    {
        attackFunc?.Invoke(duration);
    }
}
