using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleet : MonoBehaviour {

    public int team = -1;

    public FleetFormation fleetFormationPrefab = null;
    public Ship shipPrefab = null;
    public int startingShipCount = 3;
    public int maxShipCount = 5;

    public float maxFormationSpeed = 5;

    public float engagementRange = 10;

    protected List<Ship> activeShips = new List<Ship>();

    protected bool formationDirty = true;
    protected bool formationMoving = false;
    protected ShipFormation currentFormation = null;
    protected Vector3 formationPosition = Vector3.zero;
    protected Vector3 formationVelocity = Vector3.zero;
    protected float formationAngle = 0;

    protected Fleet targetedFleet = null;


    private void Start()
    {
        Debug.Assert(fleetFormationPrefab, "No FleetFormation given");
        Debug.AssertFormat(fleetFormationPrefab.GetMaxSupportedShips() >= maxShipCount, "{0} does not support {1} ships", fleetFormationPrefab.name, maxShipCount);

        formationPosition = transform.position;

        for (int i = 0; i < startingShipCount; i++)
        {
            Reinforce();
        }
    }

    private void LateUpdate()
    {
        activeShips.RemoveAll(delegate (Ship ship) {
            if (ship == null)
            {
                formationDirty = true;
                return true;
            }
            return false;
        });
        if(activeShips.Count == 0)
        {
            Destroy(gameObject);
            return;
        }
        UpdateAttack();
        UpdateFormation();
    }

    public void UpdateFormation()
    {
        if (formationDirty)
        {
            currentFormation = fleetFormationPrefab.GetBestShipFormation(activeShips.Count);
            formationDirty = false;
        }
        
        if(currentFormation)
        {
            if (currentFormation.TurnShipsToFace(activeShips, formationAngle) && formationMoving)
            {
                Vector3.SmoothDamp(transform.position, formationPosition, ref formationVelocity, 0.4f, maxFormationSpeed);
                formationMoving = formationVelocity.sqrMagnitude > 0.01f;
            }
            else
            {
                float deceleration = 10.0f;
                formationVelocity = Vector3.MoveTowards(formationVelocity, Vector3.zero, Time.deltaTime * deceleration);
                //Vector3.SmoothDamp(transform.position, transform.position, ref formationVelocity, 0.8f);
            }
            transform.position += formationVelocity * Time.deltaTime;
            currentFormation.transform.position = transform.position;
            currentFormation.MoveShipsIntoFormation(activeShips);
        }
    }

    void UpdateAttack()
    {
        if(targetedFleet)
        {
            Vector3 attackVector = targetedFleet.transform.position - transform.position;
            if (attackVector.sqrMagnitude > engagementRange * engagementRange)
            {
                Vector3 attackPosition = targetedFleet.transform.position - attackVector.normalized * engagementRange;
                MoveFleetTo(attackPosition);

                //stop ships attacking while we move
                foreach (Ship ship in activeShips)
                {
                    ship.AttackFleet(null);
                }
            }
            else
            {
                //aim once we've stopped moving
                if (!formationMoving)
                {
                    formationAngle = Quaternion.LookRotation(targetedFleet.transform.position - transform.position).eulerAngles.y;
                }
                foreach (Ship ship in activeShips)
                {
                    ship.AttackFleet(targetedFleet);
                }
            }
        }
    }

    public bool CanReinforce()
    {
        return activeShips.Count < maxShipCount;
    }

    public void Reinforce(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            if (CanReinforce())
            {
                GameObject gobj = Instantiate<GameObject>(shipPrefab.gameObject);
                Ship newShip = gobj.GetComponent<Ship>();
                newShip.team = team;
                activeShips.Add(newShip);
                formationDirty = true;
            }
        }
        UpdateFormation();
    }

    public void MoveFleetTo(Vector3 position)
    {
        if (Vector3.Distance(position, formationPosition) > 0.1f)
        {
            formationPosition = position;
            formationAngle = Quaternion.LookRotation(position - transform.position).eulerAngles.y;
            formationDirty = true;
            formationMoving = true;
            targetedFleet = null;
        }
    }

    public void AttackOtherFleet(Fleet fleetToAttack)
    {
        targetedFleet = fleetToAttack;
        formationMoving = false;
    }

    public Ship GetShipToAttack()
    {
        if (activeShips.Count <= 0) return null;
        return activeShips[Random.Range(0, activeShips.Count)];
    }

    public bool IsHighlighted { get { return isHighlighted; } }
    private bool isHighlighted = false;

    public bool IsSelected { get { return isSelected; } }
    private bool isSelected = false;

    public bool IsTargeted { get { return isTargeted; } }
    private bool isTargeted = false;

    public void SetIsHighlighted(bool _isHighlighted)
    {
        if (_isHighlighted != isHighlighted)
        {
            isHighlighted = _isHighlighted;
            if (isHighlighted)
            {
                OnHighlighted();
            }
            else
            {
                OnUnhighted();
            }
        }
    }

    void OnHighlighted()
    {

    }

    void OnUnhighted()
    {

    }

    public void SetIsSelected(bool _isSelected)
    {
        if(_isSelected != isSelected)
        {
            isSelected = _isSelected;
            if(isSelected)
            {
                OnSelected();
            }
            else
            {
                OnDeselected();
            }
        }
    }

    void OnSelected()
    {

    }

    void OnDeselected()
    {

    }

    public void SetIsTargeted(bool _isTargeted)
    {
        if (_isTargeted != isTargeted)
        {
            isTargeted = _isTargeted;
            if (isTargeted)
            {
                OnTargeted();
            }
            else
            {
                OnUntargeted();
            }
        }
    }

    void OnTargeted()
    {

    }

    void OnUntargeted()
    {

    }

    private void OnDrawGizmos()
    {
        if(isHighlighted)
        {
            DebugExtension.DrawCircle(transform.position, Color.white, 0.5f);
            DebugExtension.DrawCircle(transform.position, Color.red, engagementRange);
        }
        if (isSelected)
        {
            DebugExtension.DrawCircle(transform.position, Color.cyan, 0.4f);
        }
        if(targetedFleet)
        {
            DebugExtension.DrawArrow(transform.position, targetedFleet.transform.position - transform.position, Color.red);
        }
    }
}
