using UnityEngine;
using System.Collections;

public class BallBehaviour : MonoBehaviour {

    public MicrophoneController microphoneController;
    public MicrophoneInput microphoneInput;

    private float minXSpeed = 1f;
    private float maxYVelocity = 1.5f;

    private float distToGround;
    private Rigidbody2D rb;

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
        //    rb.AddForce(new Vector2(0, 30f) * Time.deltaTime * 100);
        if (transform.position.y < -10)
            transform.position = new Vector3(0, 1, 0);
        rb.velocity = new Vector3(Mathf.Max(rb.velocity.x,minXSpeed), Mathf.Min(rb.velocity.y,maxYVelocity), 0);
	}

    void OnSoundEvent(SoundEvent se)
    {
        //if (se == SoundEvent.SyllablePeak)
        //    Jump();
            
    }

    public void Jump()
    {
        rb.AddForce(new Vector2(0.1f, 2.2f) * Time.deltaTime * 100, ForceMode2D.Impulse);
    }

}
