using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MothershipFleet : Fleet {

    [Header("Power Configuration")]
    public int initialPower = 500;
    public int maxPower = 1000;
    public int powerRequiredForJourney = 1000;

    [Header("Fleet Construction")]
    public Fleet lightFleetPrefab = null;
    public Fleet mediumFleetPrefab = null;
    public Fleet heavyFleetPrefab = null;
    public List<GameObject> spawnOnUnitConstruction = new List<GameObject>();
    public Vector3 spawnOnUnitConstructionOffset = Vector3.zero;

    [Header("Spline movement")]
    public BezierSpline spline;
    public float splineMoveDuration;
    public bool lookForward;

    public SplineWalkerMode mode;

    //Transient state
    int currentPower = 0;
    float splineProgress = 0;
    float powerLossAccumulator = 0;

    //property accessors
    public int CurrentPower { get { return currentPower; } set { currentPower = Mathf.Min(value, maxPower); } }
    public float SplineProgress { get { return splineProgress; } }

    protected virtual void Update()
    {
        powerLossAccumulator += powerRequiredForJourney * (Time.deltaTime / splineMoveDuration);
        if(powerLossAccumulator >= 1)
        {
            int loss = Mathf.Min(currentPower, Mathf.FloorToInt(powerLossAccumulator));
            currentPower -= loss;
            powerLossAccumulator -= loss;
        }

        if (currentPower > 0)
        {
            if (Input.GetKeyDown(KeyCode.L))
            {
                TrySpawnFleet(lightFleetPrefab);
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                TrySpawnFleet(mediumFleetPrefab);
            }
            if (Input.GetKeyDown(KeyCode.H))
            {
                TrySpawnFleet(heavyFleetPrefab);
            }

            float newSplineProgress = Mathf.Clamp01(splineProgress + (Time.deltaTime / splineMoveDuration));
            if (spline && newSplineProgress != splineProgress)
            {
                splineProgress = newSplineProgress;

                Vector3 position = spline.GetPoint(splineProgress);
                transform.localPosition = position;
                if (lookForward)
                {
                    transform.LookAt(position + spline.GetDirection(splineProgress));
                }

                if (splineProgress >= 1.0f)
                {
                    OnReachedEndOfSpline();
                }
            }
        }
    }

    // Use this for initialization
    protected override void Start () {
        base.Start();
        currentPower = initialPower;
	}

    void OnReachedEndOfSpline()
    {

    }

    public Fleet TrySpawnFleet(Fleet prefab)
    {
        MothershipFleet mothershipFleet = FindObjectOfType<MothershipFleet>();
        if (prefab && mothershipFleet)
        {
            if (currentPower >= prefab.powerCostToSpawn)
            {
                GameObject gobj = Instantiate<GameObject>(prefab.gameObject, mothershipFleet.transform.position, Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0));
                if (gobj)
                {
                    currentPower -= prefab.powerCostToSpawn;

                    Fleet fleet = gobj.GetComponent<Fleet>();
                    fleet.DefendOtherFleet(mothershipFleet);
                    OnFleetConstructed(fleet);
                    return fleet;
                }
            }
        }
        return null;
    }

    void OnFleetConstructed(Fleet fleet)
    {
        foreach(GameObject gobjToSpawn in spawnOnUnitConstruction)
        {
            Instantiate<GameObject>(gobjToSpawn, transform.position + spawnOnUnitConstructionOffset, gobjToSpawn.transform.rotation, transform);
        }
    }
}
