using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class FleetCommander : MonoBehaviour
{
    bool isDragingArea = false;
    Vector3 mouseDownPosition;
    Vector3 mouseDownActionPosition;
    List<Fleet> selectedList = new List<Fleet>();
    List<Fleet> highlightedList = new List<Fleet>();
    Dictionary<int, List<Fleet>> controlGroups = new Dictionary<int, List<Fleet>>();
    int selectedControlGroup = -1;

    public int teamToSelect = -1;
    public float minDragDistance = 5;
    public float defaultSelectionDistance = 1.0f;
    public List<Formation> selectionMoveFormations = new List<Formation>();
    public float attackMoveSpreadDegrees = 20.0f;

    [Header("Sounds")]
    public AudioClip selectAudio = null;

    void Update()
    {
        selectedList.RemoveAll(s => s == null);
        highlightedList.RemoveAll(s => s == null);
        foreach(List<Fleet> controlGroup in controlGroups.Values)
        {
            controlGroup.RemoveAll(s => s == null);
        }
        ProcessSelection();
        ProcessActions();
    }

    void ProcessSelection()
    {
        ProcessControlGroupSelection(1, KeyCode.Alpha1);
        ProcessControlGroupSelection(2, KeyCode.Alpha2);
        ProcessControlGroupSelection(3, KeyCode.Alpha3);
        ProcessControlGroupSelection(4, KeyCode.Alpha4);
        ProcessControlGroupSelection(5, KeyCode.Alpha5);
        ProcessControlGroupSelection(6, KeyCode.Alpha6);
        ProcessControlGroupSelection(7, KeyCode.Alpha7);
        ProcessControlGroupSelection(8, KeyCode.Alpha8);
        ProcessControlGroupSelection(9, KeyCode.Alpha9);
        ProcessControlGroupSelection(0, KeyCode.Alpha0);

        // If we press the left mouse button, begin selection and remember the location of the mouse
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            DeselectAll();
        }
        if (isDragingArea || Input.GetMouseButton(0))
        {
            if (Vector3.Distance(mouseDownPosition, Input.mousePosition) > minDragDistance)
            {
                isDragingArea = true;
                for (int i = 0; i < Fleet.g_allFleets.Count; i++)
                {
                    Fleet selectable = Fleet.g_allFleets[i];
                    if (selectable.IsHighlighted || CanHighlight(selectable))
                    {
                        bool highlight = CanSelect(selectable) && InSelectionBounds(selectable);
                        highlight &= selectable.canBeMultiSelected || highlightedList.Count == 0 || (highlightedList.Contains(selectable) && highlightedList.Count == 1);
                        if (highlight != selectable.IsHighlighted)
                        {
                            selectable.SetIsHighlighted(highlight);
                            if (highlight && !highlightedList.Contains(selectable))
                            {
                                highlightedList.Add(selectable);
                            }
                            else
                            {
                                highlightedList.Remove(selectable);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            Fleet closestSelectable = GetClosestSelectable();
            for (int i = 0; i < Fleet.g_allFleets.Count; i++)
            {
                Fleet selectable = Fleet.g_allFleets[i];
                if (selectable != closestSelectable)
                {
                    selectable.SetIsHighlighted(false);
                }
            }
            highlightedList.Clear();
            if (closestSelectable != null)
            {
                closestSelectable.SetIsHighlighted(true);
                highlightedList.Add(closestSelectable);
            }
        }
        // If we let go of the left mouse button, end selection
        if (Input.GetMouseButtonUp(0))
        {
            //perform selection
            Select(highlightedList);
            highlightedList.Clear();

            { //log selection information
                var sb = new StringBuilder();
                sb.AppendLine(string.Format("Selecting [{0}] Units", selectedList.Count));
                for (int i = 0; i < selectedList.Count; i++)
                {
                    sb.AppendLine("-> " + selectedList[i].gameObject.name);
                }
                Debug.Log(sb.ToString());
            }

            isDragingArea = false;
        }
    }

    void ProcessControlGroupSelection(int index, KeyCode keyCode)
    {
        if (Input.GetKeyDown(keyCode))
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftAlt))
            {
                RegisterControlGroup(index, selectedList);
            }
            else
            {
                SelectControlGroup(index);
            }
        }
    }

    void ProcessActions()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            MothershipFleet mothershipFleet = FindObjectOfType<MothershipFleet>();
            if (mothershipFleet != null)
            {
                for (int i = 0; i < Fleet.g_allFleets.Count; i++)
                {
                    Fleet fleet = Fleet.g_allFleets[i];
                    if (CanSelect(fleet))
                    {
                        fleet.DefendOtherFleet(mothershipFleet);
                    }
                }
            }
        }

        if (selectedList.Count <= 0)
            return;

        if (Input.GetMouseButtonUp(1))
        {
            if (highlightedList.Count == 1)
            {
                Fleet highlighted = highlightedList[0];
                //attack if enemy
                if (highlighted.team != teamToSelect)
                {
                    Vector3 leaderPosition = selectedList[0].transform.position;
                    Vector3 attackVector = highlighted.transform.position - leaderPosition;
                    for(int i = 0; i < selectedList.Count; i++)
                    {
                        Fleet selectable = selectedList[i];
                        if (selectable != null && selectable.canBeCommanded)
                        {
                            //move closer if we need to
                            if (attackVector.sqrMagnitude > selectable.attackRange * selectable.attackRange)
                            {
                                float rotDir = i % 2 == 0 ? 1 : -1;
                                attackVector = Quaternion.Euler(0, rotDir * i * 0.5f * attackMoveSpreadDegrees, 0.0f) * attackVector;
                                Vector3 attackPosition = highlighted.transform.position - attackVector.normalized * (selectable.attackRange - 0.5f);
                                selectable.MoveFleetTo(attackPosition);
                            }

                            //attack
                            selectable.AttackOtherFleet(highlighted);
                            //cancel defend
                            selectable.DefendOtherFleet(null);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < selectedList.Count; i++)
                    {
                        //defend
                        Fleet selectable = selectedList[i];
                        selectable.DefendOtherFleet(highlighted);
                    }
                }
            }
            else
            {//move
                Vector3 mapPosition = ScreenToMapPosition(Input.mousePosition);
                Formation formation = Formation.SelectBestFormation<Formation>(selectionMoveFormations, selectedList.Count);
                if(formation != null)
                {
                    for (int i = 0; i < selectedList.Count; i++)
                    {
                        Fleet selectable = selectedList[i];
                        if (selectable != null)
                        {
                            selectable.AttackOtherFleet(null);
                            selectable.DefendOtherFleet(null);
                            selectable.MoveFleetTo(mapPosition + formation.GetPositionAt(i));
                        }
                    }
                }
            }
        }
    }

    public void RegisterControlGroup(int index, List<Fleet> group)
    {
        //clean exising group
        if(controlGroups.ContainsKey(index))
        {
            List<Fleet> oldGroup = controlGroups[index];
            for (int i = 0; i < oldGroup.Count; i++)
            {
                oldGroup[i].ControlGroup = -1;
            }
        }
        List<Fleet> newGroup = new List<Fleet>();
        foreach (Fleet fleet in group)
        {
            if(fleet.canBeAddedToControlGroups)
            {
                //remove from old group if any
                if(controlGroups.ContainsKey(fleet.ControlGroup))
                {
                    controlGroups[fleet.ControlGroup].Remove(fleet);
                }
                //add to new group
                newGroup.Add(fleet);
                fleet.ControlGroup = index;
            }
        }
        controlGroups[index] = newGroup;
    }

    public void SelectControlGroup(int index)
    {
        if (controlGroups.ContainsKey(index))
        {
            List<Fleet> group = controlGroups[index];
            if (group != null && group.Count > 0)
            {
                if (index == selectedControlGroup)
                {
                    var playerController = GetComponent<PlayerController>();
                    if (playerController != null)
                    {
                        playerController.MoveCameraTo(group[0].transform);
                    }
                }
                Select(group);
                selectedControlGroup = index;
            }
        }
    }

    public bool CanSelect(Fleet selectable)
    {
        bool canSelect = selectable != null && (selectable.team == teamToSelect);
        return canSelect;
    }

    public bool CanHighlight(Fleet selectable)
    {
        return highlightedList.Count < Formation.GetMaxCountSupported<Formation>(selectionMoveFormations);
    }

    public bool InSelectionBounds(Fleet selectionObject)
    {
        var camera = Camera.main;
        var viewportBounds = SelectionUtils.GetViewportBounds(camera, mouseDownPosition, Input.mousePosition);
        return viewportBounds.Contains(camera.WorldToViewportPoint(selectionObject.transform.position));
    }

    public float GetSelectableDistanceSq(Fleet selectionObject)
    {
        if (selectionObject != null)
        {
            var mouseMapPos = ScreenToMapPosition(Input.mousePosition);
            return (selectionObject.transform.position - mouseMapPos).sqrMagnitude;
        }
        return float.MaxValue;
    }

    public Fleet GetClosestSelectable()
    {
        float minDistSq = float.MaxValue;
        Fleet closestSelectable = null;
        for(int i = 0; i < Fleet.g_allFleets.Count; i++)
        {
            Fleet selectable = Fleet.g_allFleets[i];
            float distSq = GetSelectableDistanceSq(selectable);
            float selectRadius = selectable.selectCircle != null ? selectable.selectCircle.outerRadius : defaultSelectionDistance;
            if (distSq < minDistSq && distSq < selectRadius)
            {
                minDistSq = distSq;
                closestSelectable = selectable;
            }
        }
        return closestSelectable;
    }

    private static Plane mapPlane = new Plane(Vector3.up, Vector3.zero);
    public static bool ScreenToMapPosition(Vector3 screenPos, out Vector3 mapPos)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        float distance = 0;
        if(mapPlane.Raycast(ray, out distance))
        {
            mapPos = ray.GetPoint(distance);
            return true;
        }
        mapPos = Vector3.zero;
        return false;
    }

    public static Vector3 ScreenToMapPosition(Vector3 screenPos)
    {
        Vector3 worldPos = Vector3.zero;
        ScreenToMapPosition(screenPos, out worldPos);
        return worldPos;
    }

    public void DeselectAll()
    {
        foreach (var selectable in selectedList)
        {
            selectable.SetIsSelected(false);
        }
        selectedList.Clear();
        selectedControlGroup = -1;
    }

    public void Select(Fleet selectable)
    {
        List<Fleet> selectableList = new List<Fleet>();
        selectableList.Add(selectable);
        Select(selectableList);
    }

    public void Select(List<Fleet> selectableList)
    {
        bool didSelect = false;
        if (selectableList != null)
        {
            //remove from list first to avoid diselecting unnecessarily
            selectedList = selectedList.Except(selectableList).ToList();
            DeselectAll();
            foreach (Fleet selectable in selectableList)
            {
                if (CanSelect(selectable))
                {
                    selectable.SetIsSelected(true);
                    selectable.SetIsHighlighted(false);
                    selectedList.Add(selectable);
                    didSelect = true;
                }
                else if(selectable != null)
                {
                    selectable.SetIsSelected(false);
                }
            }
        }
        if(didSelect)
        {
            FAFAudio.Instance.PlayOnce2D(selectAudio, transform.position);
        }
    }

    void OnGUI()
    {
        if (isDragingArea)
        {
            // Create a rect from both mouse positions
            var rect = SelectionUtils.GetScreenRect(mouseDownPosition, Input.mousePosition);
            SelectionUtils.DrawScreenRect(rect, new Color(0.8f, 0.8f, 0.95f, 0.25f));
            SelectionUtils.DrawScreenRectBorder(rect, 2, new Color(0.8f, 0.8f, 0.95f));
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 wp = ScreenToMapPosition(Input.mousePosition);
        Gizmos.color = Color.magenta;
        Gizmos.DrawCube(wp, Vector3.one * 0.1f);
    }
}