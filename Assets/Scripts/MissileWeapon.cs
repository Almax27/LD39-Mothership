using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileWeapon : ShipWeapon
{
    public float fireRate = 1.0f;
    public int salvoCount = 5;
    public int damagePerMissile = 1;
    public float missileAngleSpread = 45;

    public Projectile projectilePrefab = null;

    Ship owningShip = null;
    Fleet targetFleet = null;

    float tick = 0;

    public override void AttackFleet(Ship _owningShip, Fleet _targetFleet)
    {
        owningShip = _owningShip;
        if (_targetFleet != targetFleet)
        {
            targetFleet = _targetFleet;
            tick = Random.Range(0, fireRate);
        }
    }

    // Use this for initialization
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        tick += Time.deltaTime;
        if (tick > fireRate)
        {
            tick -= fireRate;
            Fire();
        }
    }

    void Fire()
    {
        if (targetFleet)
        {
            for (int i = 0; i < salvoCount; i++)
            {
                Ship shipToAttack = targetFleet.GetShipToAttack();
                if (shipToAttack)
                {
                    GameObject gobj = GameObjectPoolManager.Instance.GetOrCreate(projectilePrefab.gameObject);
                    if (gobj != null)
                    {
                        Projectile projectile = gobj.GetComponent<Projectile>();
                        Vector3 direction = shipToAttack.transform.position - transform.position;
                        direction.Normalize();
                        direction = Vector3.Cross(direction, Vector3.up);
                        direction *= Random.value > 0.5f ? 1 : -1;
                        direction = Quaternion.Euler(0, Random.Range(-missileAngleSpread, missileAngleSpread), 0) * direction;

                        projectile.OnFired(shipToAttack.transform, owningShip.transform, damagePerMissile, transform.position, direction);
                    }
                }
            }
        }
    }
}
