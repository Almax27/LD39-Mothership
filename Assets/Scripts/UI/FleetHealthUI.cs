using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FleetHealthUI : MonoBehaviour {

    public Image healthBar = null;

    Fleet fleet = null;


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
                int health = fleet.GetHealth();
                int maxHealth = fleet.GetMaxHealth();
                float t = maxHealth > 0 ? (float)health / maxHealth : 0;
                healthBar.transform.localScale = new Vector3(t, 1, 1);
            }
        }
	}
}
