using UnityEngine;
using System.Collections;

public class LevelGenerator : MonoBehaviour {

    public Transform islandPrefab;
    private float levelHead;

	// Use this for initialization
	void Start () {


	}
	
	// Update is called once per frame
	void Update () {
        if (transform.position.x + 5 > levelHead)
        {
            Transform island = Instantiate(islandPrefab);
            island.position = new Vector3(levelHead, 0, 0);
            levelHead += 10;
        }

	}
}
