using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetFormation : MonoBehaviour {

    public List<ShipFormation> ShipFormations = new List<ShipFormation>();

    public int GetMaxSupportedShips()
    {
        int maxFormations = 0;
        foreach(ShipFormation formation in ShipFormations)
        {
            maxFormations = Mathf.Max(maxFormations, formation.FormationNodes.Count);
        }
        return maxFormations;
    }

    public ShipFormation GetBestShipFormation(int shipCount)
    {
        int bestShipCount = int.MaxValue;
        ShipFormation bestFormation = null;
        foreach (ShipFormation formation in ShipFormations)
        {
            int shipsInFormation = formation.FormationNodes.Count;
            if (shipsInFormation >= shipCount && shipsInFormation < bestShipCount)
            {
                bestShipCount = shipsInFormation;
                bestFormation = formation;
            }
        }
        return bestFormation;
    }
}
