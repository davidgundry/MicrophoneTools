using UnityEngine;
using System.Collections;

public enum PlayerState
{
    Landing,
    OnGround,
    TakingOff,
    Flying
}

public class PlayerBehaviour : MonoBehaviour {

    // Bounds
    private float minXSpeed = 1f; //0.5f
    private float maxXSpeed = 8;
    private float maxYVelocity = 1f;
    private float maxY = 6f;

    // For aeroplane physics
    private const float density = 10;
    private const float angle = 10;

    private Rigidbody2D rb;

    private float forceTime = 0f;

    private PlayerState playerState;
    public PlayerState PlayerState
    {
        get
        {
            return playerState;
        }
    }
    private bool takenOff = false;

    public float Speed()
    {
        return rb.velocity.magnitude;
    }

    public Transform gameControllerTransform;
    private GameController gameController;

	void Start () {
        playerState = PlayerState.OnGround;
        rb = GetComponent<Rigidbody2D>();
        gameController = gameControllerTransform.GetComponent<GameController>();
    }

	
	void Update ()
    {
        transform.rotation = new Quaternion();

        switch (playerState)
        {
            case PlayerState.Landing:
                // Bring the plane gently down to the ground and trundle along to the runway.
                AddPassiveForces();
                if (transform.position.y < 0.15f)
                {
                    playerState = PlayerState.OnGround;
                    gameController.TouchDown();
                }
                break;

            case PlayerState.OnGround:
                // On ground no control
                AddPassiveForces();
                break;

            case PlayerState.TakingOff:
                // Player given control and may take off
                AddActiveForces();
                AddPassiveForces();
                if ((!takenOff) && (transform.position.y > 0.2f))
                {
                    takenOff = true;
                    gameController.TakeOff();
                }
                break;

            case PlayerState.Flying:
                // Player flying, if they get too low, they land
                AddActiveForces();
                AddPassiveForces();
                if (transform.position.y < 1f)
                    playerState = PlayerState.Landing;
                break;

        }

        ApplyPhysicalBounds();
	}


    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.tag == "Runway")
        {
            if (playerState != PlayerState.TakingOff)
            {
                gameController.EnterRunway();
                playerState = PlayerState.TakingOff;
            }
        }
        if (other.transform.tag == "FlyingStart")
            playerState = PlayerState.Flying;
    }

    private void AddPassiveForces()
    {
        float ad = rb.velocity.magnitude * density;
        float vv = rb.velocity.magnitude * angle;
        float lift = ad * vv * (1 / (transform.position.y+1));
        rb.AddForce(new Vector2(0, lift) * Time.deltaTime);
    }

    private void AddActiveForces()
    {
        if (forceTime > 0)
        {
            if (forceTime > 0.1f)
                rb.AddForce(new Vector2(1f, 0) * 220 * Time.deltaTime);
            else
                rb.AddForce(new Vector2(1f, 0) * 100 * Time.deltaTime);
            forceTime -= Time.deltaTime;
        }

        if (Input.GetKeyDown("space"))
            rb.AddForce(new Vector2(10f, 0) * 100 * Time.deltaTime);
    }

    private void ApplyPhysicalBounds()
    {
        transform.position = new Vector3(transform.position.x, Mathf.Min(transform.position.y, maxY), transform.position.z);
        rb.velocity = new Vector3(Mathf.Min(Mathf.Max(rb.velocity.x, minXSpeed), maxXSpeed), Mathf.Min(rb.velocity.y, maxYVelocity), 0);
    }


    public void Thrust()
    {
        forceTime = 0.2f;
    }

}
