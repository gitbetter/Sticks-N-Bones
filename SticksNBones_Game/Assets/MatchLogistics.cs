using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchLogistics : MonoBehaviour {

    [SerializeField] GameObject[] spawnPoints;

    private MatchHandler matchHandler;

    GameObject thisCharacter;
    GameObject opponentCharacter;

	void Start () {
        matchHandler = FindObjectOfType<MatchHandler>();

        thisCharacter = Instantiate(matchHandler.characterPrefabs[(int)SNBGlobal.thisUser.character], matchHandler.isServer ? spawnPoints[0].transform : spawnPoints[1].transform);
        opponentCharacter = Instantiate(matchHandler.characterPrefabs[(int)matchHandler.opponent.character], matchHandler.isServer ? spawnPoints[1].transform : spawnPoints[0].transform);
        opponentCharacter.GetComponent<PlayerMovement>().role = PlayerRole.Opponent;
        opponentCharacter.GetComponent<PlayerAttack>().role = PlayerRole.Opponent;
	}
}
