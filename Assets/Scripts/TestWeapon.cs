using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestWeapon : ShipWeapon{

    Ship owningShip = null;
    Fleet targetFleet = null;
    Ship targetShip = null;

    public float attackTick = 0;
    public float attackRate = 0.5f;
    public int damage = 10;

    public override void AttackFleet(Ship _owningShip, Fleet _targetFleet)
    {
        owningShip = _owningShip;
        targetFleet = _targetFleet;
    }

    private void Start()
    {
        attackTick = -Random.Range(0, attackRate);
    }

    // Update is called once per frame
    void Update () {

		if(owningShip && targetFleet)
        {
            if (targetShip)
            {
                attackTick += Time.deltaTime;
                if (attackTick > attackRate)
                {
                    attackTick -= attackRate;

                    DamagePacket packet = new DamagePacket();
                    packet.value = damage;
                    packet.direction = (targetShip.transform.position - transform.position).normalized;
                    packet.Send(targetShip.gameObject);

                    Debug.DrawLine(transform.position, targetShip.transform.position, GameManager.GetTeamColor(owningShip.team), 0.2f);
                }
            }
            else
            {
                targetShip = targetFleet.GetShipToAttack();
            }
        }
        else
        {
            targetShip = null;
        }
	}
}
