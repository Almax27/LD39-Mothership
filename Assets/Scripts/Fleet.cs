using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleet : MonoBehaviour {

    public int team = -1;

    public FleetShipFormations fleetFormationPrefab = null;
    public Ship shipPrefab = null;
    public int startingShipCount = 3;
    public int maxShipCount = 5;
    public bool canBeMultiSelected = true;

    public float maxFormationSpeed = 5;

    public float engagementRange = 10;

    public CircleMeshGenerator selectCircle = null;
    public CircleMeshGenerator highlightCircle = null;
    public CircleMeshGenerator engagementRangeCircle = null;

    protected List<Ship> activeShips = new List<Ship>();

    protected bool formationDirty = true;
    protected bool isMoving = false;
    protected bool isTurning = false;
    protected ShipFormation currentFormation = null;
    protected Vector3 targetMovePosition = Vector3.zero;
    protected Vector3 moveVelocity = Vector3.zero;
    protected float targetTurnAngle = 0;

    protected Fleet targetedFleet = null;
    protected Fleet defendedFleet = null;
    protected Vector3 defensivePosition = Vector3.zero;

    private void Start()
    {
        if(fleetFormationPrefab && fleetFormationPrefab.GetMaxSupportedShips() < maxShipCount)
        {
            Debug.LogWarningFormat("{0} does not support {1} ships", fleetFormationPrefab.name, maxShipCount);
        }

        targetMovePosition = transform.position;

        for (int i = 0; i < startingShipCount; i++)
        {
            Reinforce();
        }

        //resize engagementCircle
        if (engagementRangeCircle)
        {
            float thickness = Mathf.Max(0, engagementRangeCircle.outerRadius - engagementRangeCircle.innerRadius);
            engagementRangeCircle.innerRadius = engagementRange - thickness * 0.5f;
            engagementRangeCircle.outerRadius = engagementRange + thickness * 0.5f;
            engagementRangeCircle.Generate();
        }

        RefreshCircleColors();

        OnUnhighted();
        OnDeselected();
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
        UpdateDefend();
        UpdateAttack();
        UpdateFormation();
    }

    public void UpdateFormation()
    {
        if (fleetFormationPrefab && formationDirty)
        {
            currentFormation = fleetFormationPrefab.GetBestShipFormation(activeShips.Count);
            formationDirty = false;
        }

        isTurning = false;
        isMoving = false;

        foreach (Ship ship in activeShips)
        {
            isTurning |= ship.Turn(targetTurnAngle);
        }
        if (!isTurning)
        {
            Vector3.SmoothDamp(transform.position, targetMovePosition, ref moveVelocity, 0.4f, maxFormationSpeed);
            isMoving = moveVelocity.sqrMagnitude > 0.01f;
        }

        if(!isMoving)
        {
            float deceleration = 10.0f;
            moveVelocity = Vector3.MoveTowards(moveVelocity, Vector3.zero, Time.deltaTime * deceleration);
        }

        transform.position += moveVelocity * Time.deltaTime;

        if (currentFormation)
        {
            for (int i = 0; i < activeShips.Count; i++)
            {
                activeShips[i].MoveToLocal(currentFormation.GetPositionAt(i));
            }
        }
        else
        {
            for (int i = 0; i < activeShips.Count; i++)
            {
                activeShips[i].MoveToLocal(Vector3.zero);
            }
        }
    }

    void UpdateDefend()
    {
        if(defendedFleet)
        {
            MoveFleetTo(defendedFleet.transform.position + defensivePosition);
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

    public void RefreshCircleColors()
    {
        //update selection circle to match team colour
        if (selectCircle && selectCircle.meshRenderer)
        {
            var material = selectCircle.meshRenderer.material;
            if (material)
            {
                Color teamColor = GameManager.GetTeamColor(team);
                teamColor.a = material.color.a;
                material.color = teamColor;
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
                gobj.transform.parent = this.transform;
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

    public void DefendOtherFleet(Fleet fleetToDefend)
    {
        defendedFleet = fleetToDefend;
        if(defendedFleet)
        {
            defensivePosition = Quaternion.Euler(0, Random.Range(0.0f, 360f), 0) * Vector3.forward;
            if (defendedFleet.selectCircle)
            {
                float minRadius = defendedFleet.selectCircle.outerRadius;
                if (selectCircle) minRadius += selectCircle.outerRadius;
                defensivePosition *= Random.Range(minRadius, minRadius + 2.0f);
            }
            else
            {
                defensivePosition *= 4.0f;
            }
        }
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
        if(highlightCircle)
        {
            highlightCircle.gameObject.SetActive(true);
        }
        if(engagementRangeCircle)
        {
            engagementRangeCircle.gameObject.SetActive(true);
        }
    }

    void OnUnhighted()
    {
        if (highlightCircle)
        {
            highlightCircle.gameObject.SetActive(false);
        }
        if (engagementRangeCircle)
        {
            engagementRangeCircle.gameObject.SetActive(false);
        }
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
        if (selectCircle)
        {
            selectCircle.gameObject.SetActive(true);
        }
    }

    void OnDeselected()
    {
        if (selectCircle)
        {
            selectCircle.gameObject.SetActive(false);
        }
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
        if(targetedFleet)
        {
            Gizmos.color = new Color(1,0,0, 0.4f);
            Gizmos.DrawLine(transform.position, targetedFleet.transform.position);
        }
    }
}
