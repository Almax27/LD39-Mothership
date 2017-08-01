using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MothershipFleet : Fleet {

    [Header("Power Configuration")]
    public int initialPower = 500;
    public int maxPower = 1000;

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

    //property accessors
    public float CurrentPower { get { return currentPower; } }

    protected virtual void Update()
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
        if(spline && newSplineProgress != splineProgress)
        {
            splineProgress = newSplineProgress;

            Vector3 position = spline.GetPoint(splineProgress);
            transform.localPosition = position;
            if (lookForward)
            {
                transform.LookAt(position + spline.GetDirection(splineProgress));
            }

            if(splineProgress >= 1.0f)
            {
                OnReachedEndOfSpline();
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
            GameObject gobj = Instantiate<GameObject>(prefab.gameObject, mothershipFleet.transform.position, Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0));
            if (gobj)
            {
                Fleet fleet = gobj.GetComponent<Fleet>();
                fleet.DefendOtherFleet(mothershipFleet);
                OnFleetConstructed(fleet);
                return fleet;
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

    protected override void OnGUI()
    {
        base.OnGUI();   
    }
}
