using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttack : MonoBehaviour {

    [SerializeField] float punchDuration = 0.2f;
    [SerializeField] public PlayerRole role = PlayerRole.Local;
    [SerializeField] AudioClip punchHitSFX; 

    private Animator playerAnimator;
    private SNBPlayer player;

    private float punchTime;

    private void Start() {
        playerAnimator = GetComponent<Animator>();
        player = GetComponent<PlayerManagement>().player;
    }

    void Update () {
        if (role == PlayerRole.Local) {
            EndAttack();
            PlayerPunch();
        }
    }

    private void PlayerPunch() {
        if (Input.GetButton("Fire1") && !player.state.attacking) {
            player.state.attacking = true;
            punchTime = Time.time + punchDuration;
            if (!player.state.grounded) {
                playerAnimator.CrossFade("AirPunch", 0.2f);
            } else if (player.state.crouching) {
                playerAnimator.CrossFade("CrouchPunch", 0.2f);
            } else {
                playerAnimator.CrossFade("SimplePunch", 0.2f);
            }
        }
    }

    private void EndAttack() {
        if (player.state.attacking && Time.time > punchTime) {
            player.state.attacking = false;
            if (!player.state.grounded) {
                playerAnimator.CrossFade("Air", 0.2f);
            } else if (player.state.crouching) {
                playerAnimator.CrossFade("Crouch", 0.2f);
            } else if (player.state.skipping || player.state.dashing) {
                playerAnimator.CrossFade("Skip", 0.2f);
            } else {
                playerAnimator.CrossFade("Idle", 0.2f);
            }
        }
    }
}
