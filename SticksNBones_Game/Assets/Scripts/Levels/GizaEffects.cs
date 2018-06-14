using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizaEffects : MonoBehaviour {

    [SerializeField] ParticleSystem dustKickupParticles;

    private void OnCollisionStay(Collision collision) {
        if (collision.gameObject.tag == "Player") {
            SNBPlayer player = collision.gameObject.GetComponent<PlayerManagement>().player;
            if (player.state.dashing) {
                foreach (ContactPoint contact in collision.contacts) {
                    ParticleSystem dustKickup = Instantiate(dustKickupParticles);
                    dustKickup.transform.position = contact.point;
                    dustKickup.Play();
                }
            }
        }
    }
}
