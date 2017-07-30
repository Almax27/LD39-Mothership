using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePacket
{
    public int value = 0;
    public Vector3 direction = Vector3.zero;
    public AudioClip hitSound = null;

    public void Send(GameObject gobj)
    {
        foreach(Health health in gobj.GetComponents<Health>())
        {
            health.Damage(this);
        }
    }
}
