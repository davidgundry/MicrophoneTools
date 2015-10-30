using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

    public Transform target;
    private Vector2 offset;

    void Start()
    {
        offset = new Vector2(-1, 0.2f);
    }
    void Update()
    {
        transform.position = new Vector3(target.position.x - offset.x, target.position.y - offset.y, target.position.z-10); 
    }
}
