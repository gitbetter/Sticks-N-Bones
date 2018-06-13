using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraPivotMovement : MonoBehaviour {

    void Update() {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        float newX = players[0].transform.position.x - (players[0].transform.position.x - players[1].transform.position.x) / 2.0f;
        float newY = players[0].transform.position.y - (players[0].transform.position.y - players[1].transform.position.y) / 2.0f;
        transform.position = new Vector3(newX, newY, transform.position.z);
    }
    
}
