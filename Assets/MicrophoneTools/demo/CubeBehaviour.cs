﻿using UnityEngine;
using System.Collections;

public class CubeBehaviour : MonoBehaviour {

	// Use this for initialization
	void Start () {
        GetComponent<Rigidbody>().angularVelocity = new Vector3(0, 0.2f, 0);
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
