using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using WordPlane;

namespace WordPlane
{
    public class GameController : MonoBehaviour
    {

        public Transform player;
        private PlayerBehaviour playerBehaviour;

        public Text distanceText;
        public Text speedText;
        public Text pointsText;
        public Text bestDistanceText;

        private float distance;
        private int points;
        private int bestDistance = 0;

        private float lastTakeoffX;

        public Transform runwayPrefab;
        public Transform coinPrefab;

        private StimulusScript stimulusScript;

        private MicTools.MicrophoneController microphoneController;
        private MicTools.MicrophoneInput microphoneInput;

        void Start()
        {
            playerBehaviour = player.GetComponent<PlayerBehaviour>();

            //stimulusScript = GameObject.Find("Stimulus").GetComponent<StimulusScript>();

            microphoneController = GetComponent<MicTools.MicrophoneController>();
            microphoneInput = GetComponent<MicTools.MicrophoneInput>();
            microphoneController.microphoneActive = true;
            AddRunway(3f);
        }

        void Update()
        {

            TelemetryTools.Telemetry.Instance.SendFrame();
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.FrameTime, Time.time);
            TelemetryTools.Telemetry.Instance.SendStreamValue("lvl", microphoneInput.Level);
            TelemetryTools.Telemetry.Instance.SendStreamValue("x", player.position.x);
            TelemetryTools.Telemetry.Instance.SendStreamValue("y", player.position.y);

            if ((playerBehaviour.PlayerState == PlayerState.Flying) || (playerBehaviour.PlayerState == PlayerState.TakingOff) || (playerBehaviour.PlayerState == PlayerState.Landing))
            {
                distance = player.position.x - lastTakeoffX;
                distanceText.text = (int)(distance * 5) + " m";
            }

            speedText.text = (int)((playerBehaviour.Speed() * 5 * 60 * 60) / 1000) + " km/h";
            //speedText.text = ""+microphoneInput.normalisedPeakAutocorrelation;

            if (pointsText.transform.localScale.x > 0.75f)
            {
                pointsText.transform.localScale = pointsText.transform.localScale - new Vector3(1, 1, 1) * Time.deltaTime;
            }
            if (bestDistanceText.transform.localScale.x > 0.75f)
            {
                bestDistanceText.transform.localScale = bestDistanceText.transform.localScale - new Vector3(1, 1, 1) * Time.deltaTime;
            }

            //HumInput();

            if (Input.GetKey("space"))
                InputEvent();
            if (Input.GetKeyDown("c"))
                AddCoin(player.position + new Vector3(10, 0, 0));

            for (var i = 0; i < Input.touchCount; ++i)
            {
                if (Input.GetTouch(i).phase == TouchPhase.Began)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
                    AddCoin(ray.GetPoint(10));
                }
            }

            if (distance * 5 > 1500)
                AcceleratePlayer(5f);
            else if (distance * 5 > 800)
                AcceleratePlayer(3f);
            else if (distance * 5 > 300)
                AcceleratePlayer(2f);
            else if (distance * 5 > 100)
                AcceleratePlayer(1.5f);
        }

        private void AddCoin(Vector3 position)
        {
            Transform coin = Instantiate(coinPrefab);
            coin.position = position;// player.position + new Vector3(10, 0, 0);
        }

        private void AcceleratePlayer(float newMultiplier)
        {
            playerBehaviour.SpeedMultiplier = newMultiplier;
        }

        private void AddRunway(float offsetX)
        {
            //Transform question = Instantiate(questionTextPrefab);
            //question.position = new Vector3(player.position.x + offsetX+1, 2, 1);
            //question.GetComponent<TextMesh>().text = "How are you today?";

            Transform runway = Instantiate(runwayPrefab);
            runway.position = new Vector3(player.position.x + offsetX, 0, 0);
        }

        public void TakeOff()
        {
            lastTakeoffX = player.position.x;
            TelemetryTools.Telemetry.Instance.SendEvent("Takeoff");
        }

        public void EnterRunway()
        {
            lastTakeoffX = player.position.x;
            distance = 0;
            TelemetryTools.Telemetry.Instance.SendEvent("Enter Runway");
        }

        public void RunwayCheckpoint()
        {
            lastTakeoffX = player.position.x;
            TelemetryTools.Telemetry.Instance.SendEvent("Runway Checkpoint");
        }

        public void FlyingStart()
        {
            TelemetryTools.Telemetry.Instance.SendEvent("Flying Start");
        }

        public void TouchDown()
        {
            distance = player.position.x - lastTakeoffX;
            if ((int)distance * 5 > bestDistance)
                NewBestDistance((int)distance * 5);

            AddRunway(10);
            TelemetryTools.Telemetry.Instance.SendEvent("Touch Down");
            playerBehaviour.speedMultiplier = 1;
        }

        public void InputEvent()
        {
            playerBehaviour.Thrust();
            //stimulusScript.newInput = true;
        }

        public void AddPoints(int points)
        {
            this.points += points;
            pointsText.text = "" + this.points;
            pointsText.transform.localScale = new Vector3(1, 1, 1);
        }


        void OnSoundEvent(MicTools.SoundEvent se)
        {
            if (se == MicTools.SoundEvent.SyllablePeak)
                InputEvent();
        }

        private void HumInput()
        {
            //if (microphoneInput.Level > microphoneInput.NoiseIntensity)
            //if (microphoneInput.Syllable)
                //InputEvent();
        }

        private void NewBestDistance(int distance)
        {
            bestDistance = distance;
            bestDistanceText.text = "Best:  " + bestDistance + " m";
            bestDistanceText.transform.localScale = new Vector3(1, 1, 1);
        }

    }
}