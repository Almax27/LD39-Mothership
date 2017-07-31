using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserWeapon : ShipWeapon
{
    public float fireRate;
    public int damage;

    public Projectile projectilePrefab = null;

    Ship owningShip = null;
    Fleet targetFleet = null;
    Ship targetShip = null;

    float tick = 0;

    public override void AttackFleet(Ship _owningShip, Fleet _targetFleet)
    {
        owningShip = _owningShip;
        if (_targetFleet != targetFleet)
        {
            targetFleet = _targetFleet;
            targetShip = null;
            tick = Random.Range(0, fireRate);
        }
    }

    // Use this for initialization
    void Start () {
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (targetFleet)
        {
            if (targetShip)
            {
                tick += Time.deltaTime;
                if (tick > fireRate)
                {
                    tick -= fireRate;
                    Fire(targetShip.transform.position - transform.position);
                }
            }
            else
            {
                targetShip = targetFleet.GetShipToAttack();
            }
        }
	}

    void Fire(Vector3 direction)
    {
        GameObject gobj = Instantiate<GameObject>(projectilePrefab.gameObject);
        Projectile projectile = gobj.GetComponent<Projectile>();

        projectile.OnFired(targetShip.transform, owningShip.transform, damage, transform.position, direction.normalized);
    }
}
