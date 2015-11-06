using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameController : MonoBehaviour {

    public Transform player;
    private PlayerBehaviour playerBehaviour;

    public Text distanceText;
    public Text speedText;
    public Text pointsText;

    private float distance;
    private int points;

    private float lastTakeoffX;
    

    public Transform questionTextPrefab;
    public Transform runwayPrefab;

    private MicrophoneController microphoneController;
    private MicrophoneInput microphoneInput;

	void Start ()
    {
        playerBehaviour = player.GetComponent<PlayerBehaviour>();

        microphoneController = GetComponent<MicrophoneController>();
        microphoneInput = GetComponent<MicrophoneInput>();
        microphoneController.microphoneActive = true;
        AddRunway(2);
	}
	
	void Update ()
    {
        if ((playerBehaviour.PlayerState == PlayerState.Flying) || (playerBehaviour.PlayerState == PlayerState.TakingOff) || (playerBehaviour.PlayerState == PlayerState.Landing))
            distance = player.position.x - lastTakeoffX;

        distanceText.text = (int) (distance*5) + " m";// (int)(player.position.x * 5) + "m";
        pointsText.text = ""+points;
        speedText.text = (int) (playerBehaviour.Speed()*5) + " m/s";// (int)(player.position.x * 5) + "m";
	}

    private void AddRunway(float offsetX)
    {
        Transform question = Instantiate(questionTextPrefab);
        question.position = new Vector3(player.position.x + offsetX+1, 2, 1);
        question.GetComponent<TextMesh>().text = "How are you today?";

        Transform runway = Instantiate(runwayPrefab);
        runway.position = new Vector3(player.position.x + offsetX, 0, 0);
    }

    public void TakeOff()
    {
        lastTakeoffX = player.position.x;
    }

    public void EnterRunway()
    {
        distance = 0;
    }

    public void TouchDown()
    {
        distance = player.position.x - lastTakeoffX;
        points += (int) distance;
        AddRunway(5);
    }

    public void InputEvent()
    {
        playerBehaviour.Thrust();
    }

    void OnSoundEvent(SoundEvent se)
    {
        if (se == SoundEvent.SyllablePeak)
            InputEvent();
    }

}
