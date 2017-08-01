using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleet : MonoBehaviour {

    [Header("Fleet Configuration")]
    public int team = -1;
    public int powerCostToSpawn = 100;

    public FleetShipFormations fleetFormationPrefab = null;
    public Ship shipPrefab = null;
    public int startingShipCount = 3;
    public int maxShipCount = 5;
    public bool canBeMultiSelected = true;
    public bool canBeAddedToControlGroups = true;
    public bool willAttackWhenIdle = true;
    public bool willAttackWhenDefending = true;

    public float maxFormationSpeed = 5;

    public float attackRange = 10;
    public float engagementRange = 10;
    public float chaseRange = 5;
    public float attackRefreshRate = 5.0f;

    public CircleMeshGenerator selectCircle = null;
    public CircleMeshGenerator highlightCircle = null;
    public CircleMeshGenerator engagementRangeCircle = null;

    [Header("Fleet State")]
    protected List<Ship> activeShips = new List<Ship>();

    protected bool formationDirty = true;
    protected bool isMoving = false;
    protected bool isTurning = false;
    protected bool isChasing = false;
    protected bool isAttacking = false;
    protected ShipFormation currentFormation = null;
    protected Vector3 targetMovePosition = Vector3.zero;
    protected Vector3 moveVelocity = Vector3.zero;
    protected float targetTurnAngle = 0;

    protected Fleet targetedFleet = null;
    protected Vector3 chaseStart = Vector3.zero;
    protected Fleet defendedFleet = null;
    protected Vector3 defensivePosition = Vector3.zero;
    protected float lastAttackRefreshTime = 0;

    public int ControlGroup { get { return controlGroup; } set { controlGroup = value; } }
    protected int controlGroup = -1;

    protected virtual void Start()
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

    protected virtual void LateUpdate()
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
        UpdateDefend();
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
        if(defendedFleet && !isAttacking)
        {
            MoveFleetTo(defendedFleet.transform.position + defensivePosition);
        }
    }

    void UpdateAttack()
    {
        isAttacking = false;
        if (targetedFleet)
        {
            if (!isMoving && !isTurning)
            {
                Vector3 attackVector = targetedFleet.transform.position - transform.position;
                Vector3 chaseVector = transform.position - chaseStart;
                if (attackVector.sqrMagnitude <= (attackRange * attackRange) + 0.1f)
                {
                    isAttacking = true;
                    //aim once we've stopped moving
                    if (!isMoving)
                    {
                        targetTurnAngle = Quaternion.LookRotation(targetedFleet.transform.position - transform.position).eulerAngles.y;
                    }
                }
                else if (chaseVector.sqrMagnitude < chaseRange * chaseRange)
                {
                    Quaternion chaseRotation = Quaternion.Euler(0, Random.Range(-30, 30), 0);
                    MoveFleetTo(targetedFleet.transform.position - chaseRotation * attackVector.normalized * engagementRange);
                }
                else if(isChasing)
                {
                    MoveFleetTo(chaseStart);
                    AttackOtherFleet(null);
                }
            }
        }
        if (!isAttacking || Time.time > lastAttackRefreshTime + attackRefreshRate)
        {
            lastAttackRefreshTime = Time.time;
            isAttacking = willAttackWhenIdle && !isMoving && !isTurning;
            isAttacking |= willAttackWhenDefending && defendedFleet && Vector3.Distance(defendedFleet.transform.position + defensivePosition, transform.position) < engagementRange;
            if (isAttacking && !targetedFleet)
            {
                //search for nearby enemy fleets
                float minDistSq = engagementRange * engagementRange;
                Fleet closestFleet = null;
                foreach (Fleet fleet in FindObjectsOfType<Fleet>())
                {
                    if (fleet.team != this.team)
                    {
                        float distSq = (fleet.transform.position - this.transform.position).sqrMagnitude;
                        if (distSq < minDistSq)
                        {
                            minDistSq = distSq;
                            closestFleet = fleet;
                        }
                    }
                }
                if (closestFleet)
                {
                    AttackOtherFleet(closestFleet, true);
                }
            }
        }

        if (isAttacking)
        {
            foreach (Ship ship in activeShips)
            {
                ship.AttackFleet(targetedFleet);
            }
        }
        else
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

    public void AttackOtherFleet(Fleet fleetToAttack, bool chase = true)
    {
        targetedFleet = fleetToAttack;
        chaseStart = transform.position;
        isChasing = chase;
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

    public int GetHealth()
    {
        int totalHealth = 0;
        foreach(Ship ship in activeShips)
        {
            Health health = ship.GetComponent<Health>();
            if (health)
            {
                totalHealth += health.current;
            }
        }
        return totalHealth;
    }

    public int GetMaxHealth()
    {
        if(shipPrefab)
        {
            Health health = shipPrefab.GetComponent<Health>();
            if(health)
            {
                return health.max * maxShipCount;
            }
        }
        return 0;
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

    protected virtual void OnGUI()
    {
        if(controlGroup >= 0 && isSelected)
        {
            Color guiContentColor = GUI.contentColor;

            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Camera.main.pixelHeight - screenPos.y;

            Vector2 size = new Vector2(50, 50);
            Rect rect = new Rect(GUIUtility.ScreenToGUIPoint(screenPos) - size * 0.5f, size);

            GUIStyle style = GUI.skin.label;
            style.fontSize = 20;
            style.alignment = TextAnchor.MiddleCenter;
            GUI.contentColor = Color.black;

            Rect shadowRect = new Rect(rect.position + new Vector2(1, 1), rect.size);
            GUI.Label(shadowRect, controlGroup.ToString(), style);

            GUI.contentColor = Color.white;
            GUI.Label(rect, controlGroup.ToString(), style);

            GUI.contentColor = guiContentColor;
        }
    }
}
