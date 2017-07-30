using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour {

    static public Color GetTeamColor(int team)
    {
        switch(team)
        {
            case 0:
                return Color.cyan;
            case 1:
                return Color.magenta;
            default:
                return Color.white;
        }
    }


	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
