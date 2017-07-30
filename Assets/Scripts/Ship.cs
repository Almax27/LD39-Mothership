using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour {

    public int team = -1;
    public float maxTurnTime = 1.0f;

    public int maxHealth = 100;
    public int health = 0;

    public ShipWeapon weapon = null;
    
    float currentFacingAngle = 0;
    float targetFacingAngle = 0;

    Fleet targetFleet = null;

    public void MoveTo(Vector3 position)
    {
        transform.position = position;
    }

    public bool Turn(float angle)
    {
        bool finishedTurning = true;
        targetFacingAngle = angle;
        if (maxTurnTime > 0)
        {
            finishedTurning = Mathf.Abs(currentFacingAngle - targetFacingAngle) < 0.1f;
        }
        return finishedTurning;
    }

    public void AttackFleet(Fleet fleetToAttack)
    {
        targetFleet = fleetToAttack;
        if (weapon)
        {
            weapon.targetFleet = fleetToAttack;
        }
    }

    // Use this for initialization
    void Start () {
        foreach(Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            if(renderer.material)
            {
                renderer.material.color = team == 0 ? Color.cyan : Color.red;
            }
        }
        health = maxHealth;
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (maxTurnTime > 0)
        {
            currentFacingAngle = Mathf.MoveTowardsAngle(currentFacingAngle, targetFacingAngle, (360.0f * Time.deltaTime) / maxTurnTime);
        }
        else
        {
            currentFacingAngle = targetFacingAngle;
        }
        transform.rotation = Quaternion.Euler(0, currentFacingAngle, 0);
	}
}
