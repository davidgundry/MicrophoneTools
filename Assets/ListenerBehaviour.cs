using UnityEngine;
using System.Collections;

public class ListenerBehaviour : MonoBehaviour {

    private Vector3 target;
    public float speed;
    public float turnSpeed;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, target - new Vector3(0, 1, 0), step);

        transform.LookAt(target, Vector3.up);
        transform.Rotate(new Vector3(0,180,0));
	}

    public void HearNoise(Vector3 position)
    {
        target = position;
    }
}
