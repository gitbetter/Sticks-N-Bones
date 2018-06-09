using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    [SerializeField] float dashSpeed = 9.0f;
    [SerializeField] float skipSpeed = 1.23f;
    [SerializeField] float jumpVelocity = 12.0f;
    [SerializeField] PlayerRole role = PlayerRole.Local;

    private Animator playerAnimator;

	void Start () {
        playerAnimator = GetComponent<Animator>();

        if (role == PlayerRole.Local) {
            SNBGlobal.thisPlayer.state.OnComboEvent += HandleComboEvent;
        }
	}
	
	void Update () {
        if (role == PlayerRole.Local) {
            RespondToHAxis();
            CharacterJump();
            LookAtOpponent();
        }
    }

    private void LookAtOpponent() {
        PlayerMovement[] players = GameObject.FindObjectsOfType<PlayerMovement>();
        foreach (PlayerMovement p in players) {
            if (p.gameObject != this.gameObject) {
                Vector3 dist = p.gameObject.transform.position - transform.position;
                transform.rotation = dist.x < 0 ? Quaternion.Euler(0, -90f, 0) : Quaternion.Euler(0, 90f, 0);   // warning: 90f might act as magic number
            }
        }
    }

    private void CharacterJump() {
        if (Input.GetButtonDown("Jump") && SNBGlobal.thisPlayer.state.grounded) {
            GetComponent<Rigidbody>().velocity = new Vector3(0, jumpVelocity);
            playerAnimator.SetTrigger("jump");
            SNBGlobal.thisPlayer.state.grounded = false;
        }
    }

    private void RespondToHAxis() {
        float horizontalMove = Input.GetAxisRaw("Horizontal");
        SNBGlobal.thisPlayer.state.lastHorizontal = horizontalMove;
        Move();
    }

    private void Move() {
        if (SNBGlobal.thisPlayer.state.lastHorizontal != 0) {
            if (!SNBGlobal.thisPlayer.state.dashing && !SNBGlobal.thisPlayer.state.skipping) {
                SNBGlobal.thisPlayer.ExecuteMove(BasicMove.Move);
            }

            if (SNBGlobal.thisPlayer.state.dashing) {
                Dash();
            } else {
                Skip();
            }
        } else {
            StopDash();
            StopSkip();
        }
    }

    private void Dash() {
        float newX = transform.position.x + SNBGlobal.thisPlayer.state.lastHorizontal * dashSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        if (!SNBGlobal.thisPlayer.state.dashing) {
            SNBGlobal.thisPlayer.state.dashing = true;
            playerAnimator.Play("Dash", 0, 0f);
        }
    }

    private void Skip() {
        float newX = transform.position.x + SNBGlobal.thisPlayer.state.lastHorizontal * skipSpeed * Time.deltaTime;
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
        if (!SNBGlobal.thisPlayer.state.skipping) {
            SNBGlobal.thisPlayer.state.skipping = true;
            playerAnimator.Play("Skip", 0, 0f);
        }
    }

    private void StopDash() {
        if (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) {
            playerAnimator.Play("Idle", 0, 0f);
        }
        SNBGlobal.thisPlayer.state.dashing = false;
    }

    private void StopSkip() {
        if (!playerAnimator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) {
            playerAnimator.Play("Idle", 0, 0f);
        }
        SNBGlobal.thisPlayer.state.skipping = false;
    }

    private void HandleComboEvent(ComboType combo) {
        switch (combo) {
            case ComboType.Dash:
                Dash();
                break;
            default:
                break;
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
            SNBGlobal.thisPlayer.state.grounded = true;
        }
    }
}
