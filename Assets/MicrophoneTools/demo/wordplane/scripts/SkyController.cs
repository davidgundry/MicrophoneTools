using UnityEngine;
using System.Collections;

public class SkyController : MonoBehaviour {

    public float windSpeed = 1f;
    public int cloudCount = 1;
    public Transform cloudPrefab;

    public Transform camera;

    private Transform[] clouds;

	// Use this for initialization
	void Start () {
        clouds = new Transform[(int) cloudCount];
        for (int i = 0; i < cloudCount; i++)
        {
            clouds[i] = Instantiate(cloudPrefab);
            clouds[i].position = new Vector3(Random.Range(-10, 10), Random.Range(2, 15), Random.Range(5, 20));
            clouds[i].parent = transform;
            clouds[i].GetComponent<Rigidbody2D>().velocity = new Vector2(windSpeed,0)   ;
        }
	}

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < cloudCount; i++)
        {
            if (clouds[i].position.x < camera.position.x-30)
                clouds[i].position = camera.position + new Vector3(Random.Range(20, 30), Random.Range(2, 15), Random.Range(5, 20));
        }
	}
}
