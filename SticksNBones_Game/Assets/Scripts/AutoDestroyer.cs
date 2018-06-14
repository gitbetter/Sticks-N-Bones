using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroyer : MonoBehaviour {
    [SerializeField] float lifetime = 1.5f;
	void Start () {
        Invoke("SelfDestruct", lifetime);
	}

    private void SelfDestruct() {
        Destroy(gameObject);
    }
}
