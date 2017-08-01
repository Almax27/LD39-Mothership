using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FleetHealthUI : MonoBehaviour {

    public Image healthBar = null;

    Fleet fleet = null;
    float displayedHealth = -1;


	// Use this for initialization
	void Start () {
        fleet = GetComponentInParent<Fleet>();
        if(fleet)
        {
            if(healthBar)
            {
                healthBar.color = GameManager.GetTeamColor(fleet.team);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
		if(fleet)
        {
            if (healthBar)
            {
                float t = fleet.MaxHealth > 0 ? (float)fleet.Health / fleet.MaxHealth : 0;
                if(t != displayedHealth)
                {
                    displayedHealth = t;
                    healthBar.transform.localScale = new Vector3(t, 1, 1);
                }
            }
        }
	}
}
