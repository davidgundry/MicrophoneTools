using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    private PlayerControls player;

	// Use this for initialization
	void Start () {
        player = GameObject.Find("Player").GetComponent<PlayerControls>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnSoundEvent(MicTools.SoundEvent se)
    {
        if (se == MicTools.SoundEvent.SyllablePeak)
        {
            player.Speak();
        }
    }
}
