using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Health : MonoBehaviour
{
    [System.Serializable]
    public class SpawnDefinition
    {
        public GameObject gameobject = null;
        public bool addAsChild = false;
        public bool copyPosition = true;
        public bool copyRotation = false;
        public bool copyLocalScale = false;
    }

    [Header("Config")]
    public bool isInvulnerable = false;
    public int max = 1;
    public int current = 0;
    public bool destroyOnDeath = false;

    [Header("SpawnOnEvent")]
    public SpawnDefinition[] spawnOnStart = new SpawnDefinition[0];
    public SpawnDefinition[] spawnOnDamage = new SpawnDefinition[0];
    public SpawnDefinition[] spawnOnDeath = new SpawnDefinition[0];

    [Header("Visualisation")]
    public Color defaultColor = Color.grey;
    public Color flashColor = Color.white;
    public float flashDuration = 0.0f;

    DamagePacket lastDamagePacket = null;

    float flashTick = float.MaxValue;

    public bool IsDead()
    {
        return current <= 0;
    }

    public void Start()
    {
        Reset();
        ProcessSpawns(spawnOnStart, transform.position, transform.rotation, transform.localScale);
    }

    private void Update()
    {
        if (flashDuration > 0)
        {
            if (flashTick < flashDuration)
            {
                flashTick += Time.deltaTime;
            }
            float t = Mathf.Clamp01(flashTick / flashDuration);
            var renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.material.color = Color.Lerp(flashColor, defaultColor, t);
            }
        }
    }

    public void Reset()
    {
        current = max;
    }

    public void Kill()
    {
        if (!IsDead())
        {
            current = 0;
            Death();
        }
    }

    private void Death()
    {
        ProcessSpawns(spawnOnDeath, transform.position, transform.rotation, transform.localScale);
        BroadcastMessage("OnDeath", SendMessageOptions.DontRequireReceiver);
        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void Damage(DamagePacket packet)
    {
        if (IsDead() || isInvulnerable)
        {
            return;
        }

        current -= packet.value;

        lastDamagePacket = packet;

        flashTick = 0;

        ProcessSpawns(spawnOnDamage, transform.position, Quaternion.LookRotation(packet.direction, Vector3.up), transform.localScale);

        BroadcastMessage("OnDamage", packet, SendMessageOptions.DontRequireReceiver);

        if (IsDead())
        {
            Death();
        }
    }

    void ProcessSpawns(SpawnDefinition[] spawnDefinitions, Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        foreach (SpawnDefinition def in spawnDefinitions)
        {
            if (def.gameobject)
            {
                var gobj = Instantiate(def.gameobject);
                if (def.addAsChild)
                {
                    gobj.transform.parent = transform;
                }
                if (def.copyPosition)
                {
                    gobj.transform.position = position;
                }
                if (def.copyRotation)
                {
                    gobj.transform.rotation = rotation;
                }
                if (def.copyLocalScale)
                {
                    gobj.transform.localScale = localScale;
                }
            }
        }
    }
}
