using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipFormation : Formation {

    public void MoveShipsIntoFormation(List<Ship> ships)
    {
        for(int i = 0; i < ships.Count; i++)
        {
            ships[i].MoveTo(GetPositionAt(i));
        }
    }

    public bool TurnShipsToFace(List<Ship> ships, float angle)
    {
        bool finishedTurning = false;
        foreach (Ship ship in ships)
        {
            finishedTurning |= ship.Turn(angle);
        }
        return finishedTurning;
    }
}
