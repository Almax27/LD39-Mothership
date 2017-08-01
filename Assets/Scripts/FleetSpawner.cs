using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetSpawner : MonoBehaviour {

    public enum SpawnFacing
    {
        Random,
        Outwards,
        SpawnerRotation
    }

    public Fleet fleetPrefab = null;
    [Tooltip("Number of fleets to spawn each interval")]
    public int fleetCountPerInterval = 1;
    public int team = 1;
    public float spawnRadius = 5;
    public float mothershipRadius = -1;
    public SpawnFacing spawnFacing = SpawnFacing.Random;
    public bool autoTargetMothership = false;
    [Tooltip("Override chase range of spawned fleets, negative numbers are ignored")]
    public float chaseOverride = -1.0f;

    public float firstSpawnDelay = 0;
    public float spawnInterval = 10;
    [Tooltip("Amount to +- spawn interval")]
    public float spawnIntervalRand = 0;

    [Tooltip("Max number of intervals to process")]
    public int maxIntervals = 0;
    
    float tick = 0;
    int intervals = 0;
    float nextInterval = 0;
    Mothership mothership = null;


	// Use this for initialization
	void Start () {
        nextInterval = firstSpawnDelay;
    }
	
	// Update is called once per frame
	void Update () {
        if(mothership == null)
        {
            mothership = FindObjectOfType<Mothership>();
        }
        if(mothership && mothershipRadius > 0)
        {
            if((mothership.transform.position - transform.position).sqrMagnitude > mothershipRadius * mothershipRadius)
            {
                return;
            }
        }
        if (maxIntervals < 0 || intervals < maxIntervals)
        {
            tick += Time.deltaTime;
            if (tick > nextInterval)
            {
                tick -= nextInterval;
                nextInterval = spawnInterval + Random.Range(-spawnIntervalRand, spawnIntervalRand);
                intervals++;
                DoSpawn();
            }
        }
	}

    void DoSpawn()
    {
        if(fleetPrefab == null)
        {
            Debug.LogWarning("Failed to spawn fleet: no fleetPrefab given");
        }
        for(int i = 0; i < fleetCountPerInterval; i++)
        {
            Vector3 position = transform.position + Random.insideUnitSphere * spawnRadius;
            position.y = 0;

            Quaternion rotation = new Quaternion();
            switch(spawnFacing)
            {
                case SpawnFacing.Random:
                    rotation = Quaternion.Euler(0, Random.Range(0.0f,360.0f), 0);
                    break;
                case SpawnFacing.Outwards:
                    rotation = Quaternion.LookRotation(position.normalized);
                    break;
                case SpawnFacing.SpawnerRotation:
                    rotation = transform.rotation;
                    break;
            }

            GameObject gobj = Instantiate<GameObject>(fleetPrefab.gameObject, position, rotation);
            Fleet fleet = gobj.GetComponent<Fleet>();
            fleet.team = team;

            if (autoTargetMothership)
            {
                MothershipFleet mothershipFleet = FindObjectOfType<MothershipFleet>();
                if(mothershipFleet)
                {
                    fleet.AttackOtherFleet(mothershipFleet);
                    fleet.chaseRange = float.MaxValue;
                }
            }

            if (chaseOverride >= 0)
            {
                fleet.chaseRange = chaseOverride;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Color teamColor = GameManager.GetTeamColor(team);
        DebugExtension.DrawCircle(transform.position, teamColor, spawnRadius);
        Vector3 arrowOffset = Vector3.up * 2.0f;
        if (autoTargetMothership)
        {
            DebugExtension.DrawArrow(transform.position, -transform.position.normalized * (spawnRadius + 1.0f), Color.white);
        }
        else
        {
            DebugExtension.DrawArrow(transform.position + arrowOffset, -arrowOffset, Color.white);
        }

        if(mothershipRadius > 0)
        {
            DebugExtension.DrawCircle(transform.position, Color.white, mothershipRadius);
        }
    }
}
