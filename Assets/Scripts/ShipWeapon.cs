using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShipWeapon : MonoBehaviour {

    public abstract void AttackFleet(Ship _owningShip, Fleet _targetFleet);
}
