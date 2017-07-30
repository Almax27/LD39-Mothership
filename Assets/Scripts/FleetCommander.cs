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
    Fleet hoverHighlightedSelectable = null;
    List<Fleet> selectedList = new List<Fleet>();

    public int teamToSelect = -1;
    public float minDragDistance = 5;
    public float maxSelectionDistance = 20;

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
        if (Input.GetMouseButton(0))
        {
            if (isDragingArea || Vector3.Distance(mouseDownPosition, Input.mousePosition) > minDragDistance)
            {
                isDragingArea = true;
                foreach (var selectable in FindObjectsOfType<Fleet>())
                {
                    bool highlight = CanSelect(selectable) && InSelectionBounds(selectable);
                    selectable.SetIsHighlighted(highlight);
                }
            }
        }
        else
        {
            Fleet closestSelectable = GetClosestSelectable();
            if (closestSelectable != hoverHighlightedSelectable)
            {
                if (hoverHighlightedSelectable) hoverHighlightedSelectable.SetIsHighlighted(false);
                if (closestSelectable) closestSelectable.SetIsHighlighted(true);
                hoverHighlightedSelectable = closestSelectable;
            }
        }
        // If we let go of the left mouse button, end selection
        if (Input.GetMouseButtonUp(0))
        {
            DeselectAll();

            if (isDragingArea)
            { //area select

                foreach (var selectable in FindObjectsOfType<Fleet>())
                {
                    if (CanSelect(selectable) && selectable.IsHighlighted)
                    {
                        selectedList.Add(selectable);
                    }
                }
            }
            else if (hoverHighlightedSelectable) //single select
            {
                if (CanSelect(hoverHighlightedSelectable))
                {
                    selectedList.Add(hoverHighlightedSelectable);
                    hoverHighlightedSelectable = null;
                }
            }

            //perform selection
            foreach (var selectable in selectedList)
            {
                selectable.SetIsHighlighted(false);
                selectable.SetIsSelected(true);
            }

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
        if (Input.GetMouseButtonUp(1))
        {
            if (hoverHighlightedSelectable)
            {
                //attack if enemy
                if(hoverHighlightedSelectable.team != teamToSelect)
                {
                    foreach (Fleet selectable in selectedList)
                    {
                        selectable.AttackOtherFleet(hoverHighlightedSelectable);
                    }
                }
            }
            else
            {//move
                Vector3 mapPosition = ScreenToMapPosition(Input.mousePosition);                
                foreach (Fleet selectable in selectedList)
                {
                    selectable.MoveFleetTo(mapPosition);
                }
            }
        }
    }

    public bool CanSelect(Fleet selectable)
    {
        bool canSelect = teamToSelect < 0 || selectable.team == teamToSelect;
        return canSelect;
    }

    public bool InSelectionBounds(Fleet selectionObject)
    {
        var camera = Camera.main;
        var viewportBounds = SelectionUtils.GetViewportBounds(camera, mouseDownPosition, Input.mousePosition);
        return viewportBounds.Contains(camera.WorldToViewportPoint(selectionObject.transform.position));
    }

    public float GetSelectableDistance(Fleet selectionObject)
    {
        var camera = Camera.main;
        Vector3 screenPos = camera.WorldToScreenPoint(selectionObject.transform.position);
        return Vector3.Distance(Input.mousePosition, screenPos);
    }

    public Fleet GetClosestSelectable()
    {
        float minDist = maxSelectionDistance;
        Fleet closestSelectable = null;
        foreach (var selectable in FindObjectsOfType<Fleet>())
        {
            float dist = GetSelectableDistance(selectable);
            if (dist < minDist)
            {
                minDist = dist;
                closestSelectable = selectable;
            }
        }
        return closestSelectable;
    }

    public Vector3 ScreenToMapPosition(Vector3 screenPos)
    {
        Vector3 worldPos = Vector3.zero;
        var camera = Camera.main;
        Ray ray = camera.ScreenPointToRay(screenPos);
        Plane mapPlane = new Plane(Vector3.up, Vector3.zero);
        float distance = 0;
        if(mapPlane.Raycast(ray, out distance))
        {
            worldPos = ray.GetPoint(distance);
        }
        return worldPos;
    }

    public void DeselectAll()
    {
        foreach (var selectable in selectedList)
        {
            selectable.SetIsSelected(false);
            selectable.SetIsHighlighted(false);
        }
        selectedList.Clear();
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