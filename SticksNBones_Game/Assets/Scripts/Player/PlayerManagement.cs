using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManagement : MonoBehaviour {

    [HideInInspector] public SNBPlayer player = new SNBPlayer();
    [SerializeField] public PlayerRole role = PlayerRole.Local;

    private MatchHandler matchHandler;

    private void Start() {
        matchHandler = FindObjectOfType<MatchHandler>();
        player.state.OnStateChanged += SendPlayerState;
    }

    private void SendPlayerState() {
        matchHandler.SendPlayerStateToOpponent(player.state);
    }
}
