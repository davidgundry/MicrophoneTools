using UnityEngine;
using System.Collections;
using Collect;

namespace Collect
{
    public class GameController : MonoBehaviour
    {

        private PlayerControls player;

        // Use this for initialization
        void Start()
        {
            player = GameObject.Find("Player").GetComponent<PlayerControls>();
        }

        // Update is called once per frame
        void Update()
        {
            TelemetryTools.Telemetry.Instance.SendFrame();
            TelemetryTools.Telemetry.Instance.SendStreamValue(TelemetryTools.Stream.FrameTime, Time.time);
        }

        void OnSoundEvent(MicTools.SoundEvent se)
        {
            if (se == MicTools.SoundEvent.SyllablePeak)
            {
                player.Speak();
            }
        }
    }
}
