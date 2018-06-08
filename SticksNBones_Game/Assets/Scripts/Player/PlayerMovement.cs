using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    [SerializeField] float dashSpeed = 9.0f;
    [SerializeField] float jumpVelocity = 12.0f;
    [SerializeField] PlayerRole role = PlayerRole.Local;

    private Animator playerAnimator;
    private bool grounded = true;

	// Use this for initialization
	void Start () {
        playerAnimator = GetComponent<Animator>();
	}
	
	// Update is called once per frame
	void Update () {
        if (role == PlayerRole.Local) {
            MoveCharacter();
            CharacterJump();
        }
    }

    private void CharacterJump() {
        if (Input.GetButtonDown("Jump") && grounded) {
            GetComponent<Rigidbody>().velocity = new Vector3(0, jumpVelocity);
            playerAnimator.SetTrigger("jump");
            grounded = false;
        }
    }

    private void MoveCharacter() {
        float horizontalThrow = Input.GetAxis("Horizontal");
        if (horizontalThrow != 0) {
            float newX = transform.position.x + horizontalThrow * dashSpeed * Time.deltaTime;
            transform.position = new Vector3(newX, transform.position.y, transform.position.z);
            playerAnimator.SetBool("dashing", true);
        } else {
            playerAnimator.SetBool("dashing", false);
        }
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")) {
            grounded = true;
        }
    }
}
