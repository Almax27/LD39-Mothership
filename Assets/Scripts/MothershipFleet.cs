using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MothershipFleet : Fleet {

    [Header("Power Configuration")]
    public int initialPower = 500;
    public int maxPower = 1000;

    [Header("Spline movement")]
    public BezierSpline spline;
    public float splineMoveDuration;
    public bool lookForward;

    public SplineWalkerMode mode;

    //Transient state
    int currentPower = 0;
    float splineProgress = 0;

    protected virtual void Update()
    {
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

    protected override void OnGUI()
    {
        base.OnGUI();   
    }
}
