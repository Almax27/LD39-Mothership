using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleet : MonoBehaviour {

    public int team = -1;

    public FleetShipFormations fleetFormationPrefab = null;
    public Ship shipPrefab = null;
    public int startingShipCount = 3;
    public int maxShipCount = 5;

    public float maxFormationSpeed = 5;

    public float engagementRange = 10;

    protected List<Ship> activeShips = new List<Ship>();

    protected bool formationDirty = true;
    protected bool isMoving = false;
    protected bool isTurning = false;
    protected ShipFormation currentFormation = null;
    protected Vector3 targetMovePosition = Vector3.zero;
    protected Vector3 moveVelocity = Vector3.zero;
    protected float targetTurnAngle = 0;

    protected Fleet targetedFleet = null;

    private void Start()
    {
        Debug.Assert(fleetFormationPrefab, "No FleetFormation given");
        Debug.AssertFormat(fleetFormationPrefab.GetMaxSupportedShips() >= maxShipCount, "{0} does not support {1} ships", fleetFormationPrefab.name, maxShipCount);

        targetMovePosition = transform.position;

        for (int i = 0; i < startingShipCount; i++)
        {
            Reinforce();
        }
    }

    private void LateUpdate()
    {
        activeShips.RemoveAll(delegate (Ship ship) 
        {
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

        isTurning = false;
        isMoving = false;
        if (currentFormation)
        {
            isTurning = !currentFormation.TurnShipsToFace(activeShips, targetTurnAngle);
            if (!isTurning)
            {
                Vector3.SmoothDamp(transform.position, targetMovePosition, ref moveVelocity, 0.4f, maxFormationSpeed);
                isMoving = moveVelocity.sqrMagnitude > 0.01f;
            }
        }
        if(!isMoving)
        {
            float deceleration = 10.0f;
            moveVelocity = Vector3.MoveTowards(moveVelocity, Vector3.zero, Time.deltaTime * deceleration);
        }

        transform.position += moveVelocity * Time.deltaTime;

        if (currentFormation)
        {
            currentFormation.transform.position = transform.position;
            currentFormation.MoveShipsIntoFormation(activeShips);
        }
    }

    void UpdateAttack()
    {
        bool isAttacking = false;
        if(targetedFleet && !isMoving && !isTurning)
        {
            Vector3 attackVector = targetedFleet.transform.position - transform.position;
            if (attackVector.sqrMagnitude <= (engagementRange * engagementRange) + 0.1f)
            {
                isAttacking = true;
                //aim once we've stopped moving
                if (!isMoving)
                {
                    targetTurnAngle = Quaternion.LookRotation(targetedFleet.transform.position - transform.position).eulerAngles.y;
                }
                foreach (Ship ship in activeShips)
                {
                    ship.AttackFleet(targetedFleet);
                }
            }
        }
        if(!isAttacking)
        {
            foreach (Ship ship in activeShips)
            {
                ship.AttackFleet(null);
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
        if (Vector3.Distance(position, targetMovePosition) > 0.1f)
        {
            targetMovePosition = position;
            targetTurnAngle = Quaternion.LookRotation(position - transform.position).eulerAngles.y;
            formationDirty = true;
            isMoving = true;
        }
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    public void AttackOtherFleet(Fleet fleetToAttack)
    {
        targetedFleet = fleetToAttack;
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
            DebugExtension.DrawCircle(transform.position, GameManager.GetTeamColor(team), 0.4f);
        }
        if(targetedFleet)
        {
            Gizmos.color = new Color(1,0,0, 0.4f);
            Gizmos.DrawLine(transform.position, targetedFleet.transform.position);
        }
    }
}
