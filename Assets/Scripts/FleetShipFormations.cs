using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FleetShipFormations : MonoBehaviour {

    public List<ShipFormation> ShipFormations = new List<ShipFormation>();

    public int GetMaxSupportedShips()
    {
        return Formation.GetMaxCountSupported<ShipFormation>(ShipFormations);
    }

    public ShipFormation GetBestShipFormation(int shipCount)
    {
        return Formation.SelectBestFormation<ShipFormation>(ShipFormations, shipCount);
    }
}
