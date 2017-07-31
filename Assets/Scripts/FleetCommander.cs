using UnityEngine;
using System;
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

    public int teamToSelect = -1;
    public float minDragDistance = 5;
    public float defaultSelectionDistance = 1.0f;
    public List<Formation> selectionMoveFormations = new List<Formation>();
    public float attackMoveSpreadDegrees = 20.0f;

    void Update()
    {
        ProcessSelection();
        ProcessActions();
    }

    void ProcessSelection()
    {
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
                foreach (var selectable in FindObjectsOfType<Fleet>())
                {
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
            foreach(Fleet selectable in highlightedList)
            {
                if(selectable != closestSelectable)
                {
                    selectable.SetIsHighlighted(false);
                }
            }
            highlightedList.Clear();
            if (closestSelectable)
            {
                closestSelectable.SetIsHighlighted(true);
                highlightedList.Add(closestSelectable);
            }
        }
        // If we let go of the left mouse button, end selection
        if (Input.GetMouseButtonUp(0))
        {
            //perform selection
            foreach (var selectable in highlightedList)
            {
                if (CanSelect(selectable))
                {
                    selectable.SetIsHighlighted(false);
                    selectable.SetIsSelected(true);
                    selectedList.Add(selectable);
                } 
            }
            highlightedList.Clear();

            { //log selection information
                var sb = new StringBuilder();
                sb.AppendLine(string.Format("Selecting [{0}] Units", selectedList.Count));
                foreach (var selectable in selectedList)
                {
                    sb.AppendLine("-> " + selectable.gameObject.name);
                }
                Debug.Log(sb.ToString());
            }

            isDragingArea = false;
        }
    }

    void ProcessActions()
    {
        if (selectedList.Count < 0)
            return;

        if(Input.GetKeyDown(KeyCode.R))
        {
            
        }
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

                        //move closer if we need to
                        if (attackVector.sqrMagnitude > selectable.engagementRange * selectable.engagementRange)
                        {
                            float rotDir = i % 2 == 0 ? 1 : -1;
                            attackVector = Quaternion.Euler(0, rotDir * i * 0.5f * attackMoveSpreadDegrees, 0.0f) * attackVector;
                            Vector3 attackPosition = highlighted.transform.position - attackVector.normalized * (selectable.engagementRange - 0.5f);
                            selectable.MoveFleetTo(attackPosition);
                        }

                        //attack
                        selectable.AttackOtherFleet(highlighted);
                    }
                }
                else
                {
                    for (int i = 0; i < selectedList.Count; i++)
                    {
                        //follow
                        Fleet selectable = selectedList[i];
                        selectable.DefendOtherFleet(highlighted);
                    }
                }
            }
            else
            {//move
                Vector3 mapPosition = ScreenToMapPosition(Input.mousePosition);
                Formation formation = Formation.SelectBestFormation<Formation>(selectionMoveFormations, selectedList.Count);
                if(formation)
                {
                    for (int i = 0; i < selectedList.Count; i++)
                    {
                        selectedList[i].AttackOtherFleet(null);
                        selectedList[i].DefendOtherFleet(null);
                        selectedList[i].MoveFleetTo(mapPosition + formation.GetPositionAt(i));
                    }
                }
            }
        }
    }

    public bool CanSelect(Fleet selectable)
    {
        bool canSelect = teamToSelect < 0 || selectable.team == teamToSelect;
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

    public float GetSelectableDistance(Fleet selectionObject)
    {
        if (selectionObject)
        {
            var mouseMapPos = ScreenToMapPosition(Input.mousePosition);
            return Vector3.Distance(selectionObject.transform.position, mouseMapPos);
        }
        return float.MaxValue;
    }

    public Fleet GetClosestSelectable()
    {
        float minDist = float.MaxValue;
        Fleet closestSelectable = null;
        foreach (var selectable in FindObjectsOfType<Fleet>())
        {
            float dist = GetSelectableDistance(selectable);
            float selectRadius = selectable.selectCircle ? selectable.selectCircle.outerRadius : defaultSelectionDistance;
            if (dist < minDist && dist < selectRadius)
            {
                minDist = dist;
                closestSelectable = selectable;
            }
        }
        return closestSelectable;
    }

    public static bool ScreenToMapPosition(Vector3 screenPos, out Vector3 mapPos)
    {
        Vector3 worldPos = Vector3.zero;
        var camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(screenPos);
        Plane mapPlane = new Plane(Vector3.up, Vector3.zero);
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
    }

    public void Select(Fleet selectable)
    {
        //remove from list first to avoid diselecting unnecessarily
        selectedList.Remove(selectable);
        DeselectAll();
        if (selectable)
        {
            selectable.SetIsSelected(true);
            selectedList.Add(selectable);
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