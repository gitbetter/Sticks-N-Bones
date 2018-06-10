using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManagement : MonoBehaviour {

    [HideInInspector] public SNBPlayer player;

	// Use this for initialization
	void Start () {
        player = new SNBPlayer();
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
