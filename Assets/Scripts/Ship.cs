using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour {

    public int team = -1;
    public float maxTurnTime = 1.0f;

    public int powerValue = 10;

    public ShipWeapon weapon = null;
    
    float currentFacingAngle = 0;
    float targetFacingAngle = 0;

    Fleet targetFleet = null;

    public void MoveToLocal(Vector3 position)
    {
        transform.localPosition = position;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="angle"></param>
    /// <returns> true if turning </returns>
    public bool Turn(float angle)
    {
        bool isTurning = false;
        targetFacingAngle = angle;
        if (maxTurnTime > 0)
        {
            isTurning = Mathf.Abs(currentFacingAngle - targetFacingAngle) > 0.1f;
        }
        return isTurning;
    }

    public void AttackFleet(Fleet fleetToAttack)
    {
        targetFleet = fleetToAttack;
        if (weapon)
        {
            weapon.AttackFleet(this, targetFleet);
        }
    }

    // Use this for initialization
    void Start () {
        Color teamColor = GameManager.GetTeamColor(team);
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            var materials = renderer.materials;
            foreach(Material mat in renderer.materials)
            {
                if(mat.name.Contains("TeamColor"))
                {
                    mat.color = teamColor;
                }
            }
        }
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

    void OnDeath()
    {
        //HACK: spawn power only for the death of non primary team ships
        if (team != 0)
        {
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager)
            {
                Health health = GetComponent<Health>();
                if (health)
                {
                    gameManager.SpawnPowerOrbs(powerValue, transform.position, health.LastDamagePacket.source);
                }
            }
        }
    }
}
