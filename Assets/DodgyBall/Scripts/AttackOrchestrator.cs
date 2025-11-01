using System.Collections.Generic;
using UnityEngine;

public class AttackOrchestrator : MonoBehaviour
{
    [Header("Timing")]
    [Min(0.01f)] public float intervalMin = 0.5f;
    [Min(0.01f)] public float intervalMax = 1.5f;
    [Min(0.01f)] public float intervalSpeed = 1f;
    
    [Header("Attack Duration (applied per child before Attack())")]
    [Min(0.01f)] public float durationMin = 0.5f;
    [Min(0.01f)] public float durationMax = 1.5f;
    
    [Header("Order")]
    public bool randomizeOrderEachCycle = true;
    
    private readonly List<AttackHandler> _attackers = new();
    private readonly List<int> _order = new();
    private int _orderIndex = 0;
    
    private readonly Dictionary<AttackHandler, float> _busyUntil = new();  // for tracking busyness
    private float _nextFireAt;
    
    void OnEnable()
    {
        RefreshChildren();
        ResetOrder();
        ScheduleNext();
    }
    
    void OnTransformChildrenChanged()
    {
        RefreshChildren();
        ResetOrder();
    }

    void Update()
    {
        if (_attackers.Count == 0) return;

        if (Time.time < _nextFireAt) return;
        
        var attacker = NextAvailableAttacker();
        if (attacker == null)
        {
            // No one is available: wait until the soonest one becomes free
            _nextFireAt = Mathf.Max(Time.time + 0.01f, SoonestAvailableTime());
            return;
        }
        
        // set per-attack duration, then attack
        float useDuration = Random.Range(durationMin, durationMax);
        attacker.Attack(useDuration);

        // mark this attacker as busy
        _busyUntil[attacker] = Time.time + useDuration;

        // schedule next fire — cannot be before the soonest available attacker
        ScheduleNext();
    }
    
    public void RefreshChildren()
    {
        _attackers.Clear();
        foreach (Transform t in transform)
        {
            var a = t.GetComponent<AttackHandler>();
            if (a != null) _attackers.Add(a);
        }
        // compress order to available count
        _order.Clear();
        for (int i = 0; i < _attackers.Count; i++) _order.Add(i);
        
        // sync busy map
        var now = Time.time;
        var keysToRemove = new List<AttackHandler>();
        foreach (var kv in _busyUntil)
        {
            if (!_attackers.Contains(kv.Key)) keysToRemove.Add(kv.Key);
        }
        foreach (var k in keysToRemove) _busyUntil.Remove(k);

        foreach (var a in _attackers)
        {
            if (!_busyUntil.ContainsKey(a)) _busyUntil[a] = 0f; // free now
        }
    }
    
    private void ResetOrder()
    {
        _orderIndex = 0;
        if (randomizeOrderEachCycle) Shuffle(_order);
    }
    
    private void ScheduleNext()
    {
        float interval = Random.Range(intervalMin, intervalMax);
        interval = Mathf.Max(0.01f, interval / Mathf.Max(0.01f, intervalSpeed));
        float candidate = Time.time + interval;

        // Don’t schedule earlier than the soonest attacker being free
        float soonestFree = SoonestAvailableTime();
        _nextFireAt = Mathf.Max(candidate, soonestFree);
    }
    
    private float SoonestAvailableTime()
    {
        if (_busyUntil.Count == 0) return Time.time;
        float min = float.PositiveInfinity;
        foreach (var kv in _busyUntil)
        {
            if (kv.Value < min) min = kv.Value;
        }
        // If all are already free, return now
        return float.IsPositiveInfinity(min) ? Time.time : Mathf.Min(min, Time.time) == Time.time ? Time.time : min;
    }
    
    private AttackHandler NextAvailableAttacker()
    {
        if (_attackers.Count == 0) return null;

        int attempts = 0;
        while (attempts < _order.Count)
        {
            if (_orderIndex >= _order.Count)
            {
                // new cycle
                _orderIndex = 0;
                if (randomizeOrderEachCycle) Shuffle(_order);
            }

            int idx = _order[_orderIndex++];
            attempts++;

            if (idx < 0 || idx >= _attackers.Count) continue;

            var a = _attackers[idx];
            if (a == null || !a.isActiveAndEnabled || !a.gameObject.activeInHierarchy) continue;

            // available?
            float freeAt = _busyUntil.TryGetValue(a, out var tFree) ? tFree : 0f;
            if (Time.time >= freeAt) return a;
        }

        return null; // none currently available
    }
    
    private static void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
