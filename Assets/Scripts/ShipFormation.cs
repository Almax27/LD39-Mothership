using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipFormation : MonoBehaviour {

    public List<Transform> FormationNodes = new List<Transform>();

    [ContextMenu("Use Children As Nodes")]
    private void UseChildrenAsNodes()
    {
        FormationNodes.Clear();
        foreach (Transform child in transform)
        {
            FormationNodes.Add(child);
        }
    }

    public void MoveShipsIntoFormation(List<Ship> ships)
    {
        for(int i = 0; i < FormationNodes.Count; i++)
        {
            if (i >= ships.Count) break;

            Transform node = FormationNodes[i];
            Ship ship = ships[i];

            ship.MoveTo(node.position);
        }
    }

    public bool TurnShipsToFace(List<Ship> ships, float angle)
    {
        bool finishedTurning = false;
        foreach(Ship ship in ships)
        {
            finishedTurning |= ship.Turn(angle);
        }
        return finishedTurning;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (Transform formationPoint in FormationNodes)
        {
            if (formationPoint)
            {
                Gizmos.DrawSphere(formationPoint.position, 0.05f);
            }
        }
    }
}
