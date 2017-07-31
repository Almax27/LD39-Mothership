using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePacket
{
    public DamagePacket(DamagePacket other)
    {
        value = other.value;
        direction = other.direction;
        source = other.source;
    }
    public DamagePacket() { }

    public int value = 0;
    public Vector3 direction = Vector2.zero;
    public Transform source = null;

    public void Send(Transform target, Transform sender)
    {
        source = sender;
        foreach (Health health in target.GetComponents<Health>())
        {
            health.Damage(new DamagePacket(this));
        }
    }
}
