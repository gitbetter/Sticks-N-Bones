using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManagement : DispatchBehavior {

    [HideInInspector] public SNBPlayer player = new SNBPlayer();
    [SerializeField] public PlayerRole role = PlayerRole.Local;

    private MatchHandler matchHandler;

    private void Start() {
        matchHandler = FindObjectOfType<MatchHandler>();
        player.state.OnStateChanged += SendPlayerState;
    }

    private void Update() {
        DispatchActions();
    }

    private void SendPlayerState() {
        if (role == PlayerRole.Local) {
            mainThreadEvents.Enqueue(() => {
                matchHandler.SendPlayerStateToOpponent(player.state);
            });
        }
    }
}
