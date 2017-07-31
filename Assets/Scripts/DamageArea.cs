using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageArea : MonoBehaviour
{
    public int damage = 1;
    public LayerMask damageMask = new LayerMask();

    [Header("Auto Damage")]
    public bool autoDamage = true;
    [Tooltip("How often damage is applied. <= 0 will only apply once to each target found")]
    public float autoDamageFrequency = 0;

    HashSet<Collider> colliders = new HashSet<Collider>();
    HashSet<Collider> hitCache = new HashSet<Collider>();
    float lastDamageTime = 0;

    public void ApplyDamage(bool forceAll = true)
    {
        ApplyDamage(damage);
    }

    public void ApplyDamage(int damage, bool forceAll = true)
    {
        colliders.RemoveWhere(item => item == null);
        foreach (var collider in colliders)
        {
            if (((1 << collider.gameObject.layer) & damageMask.value) != 0)
            {
                if (forceAll || !hitCache.Contains(collider))
                {
                    hitCache.Add(collider);

                    DamagePacket packet = new DamagePacket();
                    packet.value = damage;
                    packet.direction = transform.forward;
                    packet.Send(collider.transform, this.transform);
                }
            }
        }
    }

    private void OnEnable()
    {
        colliders.Clear();
        hitCache.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        colliders.Add(other);
    }

    private void OnTriggerStay(Collider other)
    {
        colliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        colliders.Remove(other);
    }

    private void Update()
    {
        if(autoDamage && Time.time > lastDamageTime + autoDamageFrequency)
        {
            ApplyDamage(damage, false);
            lastDamageTime = Time.time;
        }
    }
}
