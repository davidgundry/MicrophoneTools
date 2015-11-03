using UnityEngine;
using System.Collections;

public class BallBehaviour : MonoBehaviour {

    public MicrophoneController microphoneController;
    public MicrophoneInput microphoneInput;

    private float minXSpeed = 0.5f;
    private float maxXSpeed = 4;
    private float maxYVelocity = 1f;

    private float distToGround;
    private Rigidbody2D rb;

    private float forceTime = 0f;

	// Use this for initialization
	void Start () {
        distToGround = (float) GetComponent<Collider2D>().bounds.extents.y;
        rb = GetComponent<Rigidbody2D>();
        microphoneController = GetComponent<MicrophoneController>();
        microphoneInput = GetComponent<MicrophoneInput>();
        microphoneController.microphoneActive = true;
    }
 
    bool IsGrounded() {
       return Physics.Raycast(transform.position, -Vector3.up, distToGround + 0.1f);
    }
	
	// Update is called once per frame
	void Update () {
        //transform.position = new Vector3(transform.position.x + 0.07f, transform.position.y, 0);
        transform.rotation = new Quaternion();
        //if (microphoneInput.Syllable)
        //   rb.AddForce(new Vector2(100, 0) * Time.deltaTime);
        if (transform.position.y < -1)
            transform.position = new Vector3(0, 1, 0);
        
        float density = 10;
        float angle = 10;// +Mathf.Abs(Mathf.Atan2(rb.velocity.y, rb.velocity.x));
        float ad = rb.velocity.magnitude * density;
        float vv = rb.velocity.magnitude * angle;
        float lift = ad * vv * (1/transform.position.y);

        rb.AddForce(new Vector2(0, lift) * Time.deltaTime);

        if (forceTime > 0)
        {
            if (forceTime > 0.1f)
                rb.AddForce(new Vector2(1f, 0) * 200 * Time.deltaTime);
            else
                rb.AddForce(new Vector2(1f, 0) * 100 * Time.deltaTime);
            forceTime -= Time.deltaTime;
        }

        if (Input.GetKeyDown("space"))
            rb.AddForce(new Vector2(10f, 0) * 100 * Time.deltaTime);

        transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y,2), transform.position.z);

        rb.velocity = new Vector3(Mathf.Min(Mathf.Max(rb.velocity.x, minXSpeed),maxXSpeed), Mathf.Min(rb.velocity.y, maxYVelocity), 0);
	}

    void OnSoundEvent(SoundEvent se)
    {
        if (se == SoundEvent.SyllablePeak)
            Jump();
            
    }

    public void Jump()
    {
        forceTime = 0.2f;
    }

}
