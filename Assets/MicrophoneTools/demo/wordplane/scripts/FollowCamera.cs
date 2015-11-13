using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

    public Transform target;
    public Vector3 offset;
    public float minY;

    void Start()
    {

    }
    void Update()
    {
        transform.position = new Vector3(target.position.x + offset.x, Mathf.Max(target.position.y+offset.y,minY), target.position.z+offset.z); 
    }
}
