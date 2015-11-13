using UnityEngine;
using System.Collections;

public class FollowCam : MonoBehaviour {

    public Transform target;
    public Vector3 offset;

    void Start()
    {

    }
    void Update()
    {
        transform.position = new Vector3(target.position.x + offset.x, target.position.y+offset.y, target.position.z+offset.z); 
    }
}
