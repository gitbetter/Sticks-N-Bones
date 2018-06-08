using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingSpinner : MonoBehaviour {

    [SerializeField] float rotateSpeed = 500f;

    public bool rotating {
        get { return _rotating; }
        set {
            _rotating = value;
            if (!_rotating) ResetRectRotation();
        }
    }

    private bool _rotating = true;
    private RectTransform rect; 

    void Start () {
        rect = GetComponent<RectTransform>();
	}
	
	void Update () {
        if (rotating) {
            rect.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
        }
	}

    private void ResetRectRotation() {
        rect.Rotate(0f, 0f, 0f);
    }

}
