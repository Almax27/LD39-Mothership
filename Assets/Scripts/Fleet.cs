using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fleet : MonoBehaviour {

    public static List<Fleet> g_allFleets = new List<Fleet>();

#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // do something
        g_allFleets.Clear();
        g_allFleets.AddRange(FindObjectsOfType<Fleet>());
    }
#endif

    public enum FleetState
    {
        Idle,
        Moving,
        Defending,
        Attacking
    }

    [Header("Fleet Configuration")]
    public int team = -1;
    public int powerCostToSpawn = 100;
    public int powerGainedWhenMaxShipCountKilled = 200;

    public FleetShipFormations fleetFormationPrefab = null;
    public Ship shipPrefab = null;
    public int startingShipCount = 3;
    public int maxShipCount = 5;
    public bool canBeMultiSelected = true;
    public bool canBeAddedToControlGroups = true;
    public bool canBeCommanded = true;
    public bool canAttackFromIdle = true;
    public bool canAttackFromDefending = true;
    public bool canAttackWhileMoving = false;

    public float maxFormationSpeed = 5;

    public float attackRange = 10;
    public float engagementRange = 10;
    public float chaseRange = 5;
    public float attackRefreshRate = 5.0f;

    public CircleMeshGenerator selectCircle = null;
    public CircleMeshGenerator highlightCircle = null;
    public CircleMeshGenerator engagementRangeCircle = null;

    protected FleetState currentState;

    protected List<Ship> activeShips = new List<Ship>();

    protected bool isAttacking = false;
    protected bool isAutoAttacking = false;
    protected bool isMoving = false;
    protected bool isTurning = false;
    
    protected ShipFormation currentFormation = null;
    protected Vector3 targetMovePosition = Vector3.zero;
    protected Vector3 idlePosition = Vector3.zero;
    protected Vector3 moveVelocity = Vector3.zero;
    protected float targetTurnAngle = 0;

    protected Fleet targetedFleet = null;
    protected Vector3 chaseStart = Vector3.zero;
    protected Vector3 chaseOffset = Vector3.zero;
    protected Fleet defendedFleet = null;
    protected Vector3 defensivePosition = Vector3.zero;
    protected float lastAttackRefreshTime = 0;

    public int ControlGroup { get { return controlGroup; } set { controlGroup = value; } }
    protected int controlGroup = -1;

    protected void Awake()
    {
        g_allFleets.Add(this);
        g_allFleets.RemoveAll(f => f == null);
    }

    protected void OnDestroy()
    {
        g_allFleets.Remove(this);
        g_allFleets.RemoveAll(f => f == null);
    }

    protected virtual void Start()
    {
        if (fleetFormationPrefab && fleetFormationPrefab.GetMaxSupportedShips() < maxShipCount)
        {
            Debug.LogWarningFormat("{0} does not support {1} ships", fleetFormationPrefab.name, maxShipCount);
        }

        targetMovePosition = transform.position;

        for (int i = 0; i < startingShipCount; i++)
        {
            Reinforce();
        }

        if (fleetFormationPrefab != null)
        {
            currentFormation = fleetFormationPrefab.GetBestShipFormation(activeShips.Count);
            if (currentFormation != null)
            {
                for (int i = 0; i < activeShips.Count; i++)
                {
                    activeShips[i].MoveToLocal(currentFormation.GetPositionAt(i));
                }
            }
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

        lastAttackRefreshTime = Time.time + Random.Range(0, attackRefreshRate);
    }

    protected virtual void LateUpdate()
    {
        activeShips.RemoveAll(s => s== null);
        if(activeShips.Count == 0)
        {
            Destroy(gameObject);
            return;
        }
        UpdateState();
        UpdateFormation();
        UpdateHealthCache();
    }

    void UpdateState()
    {
        bool canUpdateTarget = false;
        isAttacking = false;
        isMoving = false;

        switch (currentState)
        {
            case FleetState.Idle:
                canUpdateTarget = canAttackFromIdle;
                if (defendedFleet)
                    SetState(FleetState.Defending);
                break;
            case FleetState.Moving:
                canUpdateTarget = canAttackWhileMoving;
                if ((transform.position - targetMovePosition).sqrMagnitude > 0.1f)
                {
                    isMoving = true;
                }
                else
                {
                    //reached our destination
                    SetState(FleetState.Idle);
                }
                break;
            case FleetState.Defending:
                canUpdateTarget = canAttackFromDefending;
                if (defendedFleet)
                {
                    targetMovePosition = defendedFleet.transform.position + defensivePosition;
                    isMoving = true;
                }
                else
                {
                    SetState(FleetState.Idle);
                }
                break;
            case FleetState.Attacking:
                if (targetedFleet)
                {
                    Vector3 attackVector = targetedFleet.transform.position - transform.position;
                    Vector3 chaseVector = transform.position - chaseStart;
                    canUpdateTarget = true;
                    if (attackVector.sqrMagnitude < ((attackRange+0.5f) * (attackRange+0.5f)))
                    {
                        //in range
                        isAttacking = !isTurning;
                    }
                    //handle chasing after target, always chase if not an auto attack
                    else if (!isAutoAttacking || chaseVector.sqrMagnitude < chaseRange * chaseRange)
                    {
                        //in chase range
                        isMoving = true;
                        targetMovePosition = targetedFleet.transform.position + chaseOffset;
                    }
                    else if (isAutoAttacking)
                    {
                        //out of range
                        targetedFleet = null;
                        MoveFleetTo(chaseStart);
                    }
                }
                else
                {
                    //no target
                    SetState(FleetState.Idle);
                }
                break;
        }

        if (canUpdateTarget)
        {
            UpdateTarget();
        }
        Fleet fleetToAttack = isAttacking ? targetedFleet : null;
        for(int i = 0; i < activeShips.Count; i++)
        {
            activeShips[i].AttackFleet(fleetToAttack);
        }
    }

    private void UpdateHealthCache()
    {
        cachedHealth = 0;
        cachedMaxHealth = 0;
        if(shipPrefab && shipPrefab.Health)
        {
            cachedMaxHealth = shipPrefab.Health.max * maxShipCount;
        }
        for (int i = 0; i < activeShips.Count; i++)
        {
            Ship ship = activeShips[i];
            if (ship.Health != null)
            {
                cachedHealth += ship.Health.current;
            }
        }
    } 

    void SetState(FleetState newState)
    {
        switch (newState)
        {
            case FleetState.Idle:
                targetMovePosition = transform.position;
                break;
            case FleetState.Moving:
                break;
            case FleetState.Defending:
                break;
            case FleetState.Attacking:
                chaseStart = transform.position;
                if (targetedFleet)
                {
                    Vector3 attackVector = targetedFleet.transform.position - transform.position;
                    Quaternion chaseRotation = Quaternion.Euler(0, Random.Range(-30, 30), 0);
                    chaseOffset = chaseRotation * -attackVector.normalized * Mathf.Max(0, attackRange - 0.1f);
                    targetTurnAngle = Quaternion.LookRotation(attackVector.normalized).eulerAngles.y;
                }
                break;
        }
        currentState = newState;
    }

    public void UpdateFormation()
    {
        //do moving
        if (isMoving && !isTurning)
        {    
            Vector3 pos = transform.position;
            Vector3 moveVector = targetMovePosition - pos;
            if (moveVector.sqrMagnitude > 0.1f)
            {
                Vector3.SmoothDamp(pos, targetMovePosition, ref moveVelocity, 0.4f, maxFormationSpeed);
                targetTurnAngle = Quaternion.LookRotation(moveVector.normalized).eulerAngles.y;
            }
            else
            {
                moveVelocity = Vector3.zero;
            }
        }
        else
        { 
            float deceleration = 10.0f;
            moveVelocity = Vector3.MoveTowards(moveVelocity, Vector3.zero, Time.deltaTime * deceleration);
        }

        //do turning
        isTurning = false;
        for(int i = 0; i < activeShips.Count; i++)
        {
            isTurning |= activeShips[i].Turn(targetTurnAngle);
        }
        if (moveVelocity.sqrMagnitude > 0.01f)
        {
            transform.position += moveVelocity * Time.deltaTime;
        }
    }

    public void UpdateTarget()
    {
        bool shouldRefreshTarget = Time.time > lastAttackRefreshTime + attackRefreshRate;
        if (targetedFleet == null && shouldRefreshTarget)
        {
            lastAttackRefreshTime = Time.time;

            //search for nearby enemy fleets
            float minDistSq = engagementRange * engagementRange;
            Fleet closestFleet = null;
            for (int i = 0; i < Fleet.g_allFleets.Count; i++)
            {
                Fleet fleet = Fleet.g_allFleets[i];
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
            if (closestFleet != null)
            {
                targetedFleet = closestFleet;
                isAutoAttacking = true;
                SetState(FleetState.Attacking);
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
                newShip.powerValue = powerGainedWhenMaxShipCountKilled / maxShipCount;
                activeShips.Add(newShip);
            }
        }
        UpdateFormation();
    }

    public void MoveFleetTo(Vector3 position)
    {
        Vector3 moveVector = position - transform.position;
        if (moveVector.sqrMagnitude > 0.1f)
        {
            targetMovePosition = position;
            SetState(FleetState.Moving);
        }
    }

    public void StopMoving()
    {
        if(currentState == FleetState.Moving)
        {
            SetState(FleetState.Idle);
        }
    }

    public void AttackOtherFleet(Fleet fleetToAttack)
    {
        targetedFleet = fleetToAttack;
        isAutoAttacking = false;
        if (targetedFleet)
        {
            SetState(FleetState.Attacking);
        }
    }

    public void DefendOtherFleet(Fleet fleetToDefend)
    {
        defendedFleet = fleetToDefend;
        if(defendedFleet != null)
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
            SetState(FleetState.Defending);
        }
    }

    public Ship GetShipToAttack()
    {
        if (activeShips.Count <= 0) return null;
        return activeShips[Random.Range(0, activeShips.Count)];
    }

    public int Health { get { return cachedHealth; } }
    private int cachedHealth = 0;
    public int MaxHealth { get { return cachedMaxHealth; } }
    private int cachedMaxHealth = 0;

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
        if(highlightCircle != null)
        {
            highlightCircle.gameObject.SetActive(true);
        }
        if(engagementRangeCircle != null)
        {
            engagementRangeCircle.gameObject.SetActive(true);
        }
    }

    void OnUnhighted()
    {
        if (highlightCircle != null)
        {
            highlightCircle.gameObject.SetActive(false);
        }
        if (engagementRangeCircle != null)
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
        if (selectCircle != null)
        {
            selectCircle.gameObject.SetActive(true);
        }
    }

    void OnDeselected()
    {
        if (selectCircle != null)
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
        if(targetedFleet != null)
        {
            Gizmos.color = new Color(1,0,0, 0.4f);
            Gizmos.DrawLine(transform.position, targetedFleet.transform.position);
        }
    }

    static GUIStyle controlGroupTextStyle;
    static bool initControlGroupStyle = false;
    protected virtual void OnGUI()
    {
        if(controlGroup >= 0 && isSelected)
        {
            if(!initControlGroupStyle)
            {
                initControlGroupStyle = true;
                controlGroupTextStyle = GUI.skin.label;
                controlGroupTextStyle.fontSize = 20;
                controlGroupTextStyle.alignment = TextAnchor.MiddleCenter;
            }

            Color guiContentColor = GUI.contentColor;

            Vector2 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            screenPos.y = Camera.main.pixelHeight - screenPos.y;

            Vector2 size = new Vector2(50, 50);
            Rect rect = new Rect(GUIUtility.ScreenToGUIPoint(screenPos) - size * 0.5f, size);

            GUI.contentColor = Color.black;

            Rect shadowRect = new Rect(rect.position + new Vector2(1, 1), rect.size);
            GUI.Label(shadowRect, controlGroup.ToString(), controlGroupTextStyle);

            GUI.contentColor = Color.white;
            GUI.Label(rect, controlGroup.ToString(), controlGroupTextStyle);

            GUI.contentColor = guiContentColor;
        }
    }
}
