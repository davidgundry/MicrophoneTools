using MicTools;

namespace MicTools
{

    public enum SoundEvent
    {
        PermissionRequired,
        PermissionGranted,
        MicrophoneReady,
        SyllableStart,
        SyllableEnd,
        InputStart,
        InputEnd,
        AudioStart,
        AudioEnd,
        SyllablePeak
    }

    public class EventRecord
    {

        private string key;
        public string Key
        {
            get
            {
                return key;
            }
        }
        private string value;
        public string Value
        {
            get
            {
                return value;
            }
        }
        private long time;
        public long Time
        {
            get
            {
                return time;
            }
        }

        public EventRecord(SoundEvent soundEvent)
        {
            key = SoundEventToString(soundEvent);
            value = "";
            time = System.DateTime.Now.Ticks;
        }

        public EventRecord(string key)
        {
            this.key = key;
            value = "";
            time = System.DateTime.Now.Ticks;
        }

        public EventRecord(string key, System.Object value)
        {
            this.key = key;
            this.value = value.ToString();
        }


        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("\"record\":{\"key\":\"");
            sb.Append(key);
            sb.Append("\", \"value:\"");
            sb.Append(value);
            sb.Append("\", \"time:\"");
            sb.Append(time);
            sb.Append("\"}");

            return sb.ToString();
        }

        private static string SoundEventToString(SoundEvent e)
        {
            switch (e)
            {
                case SoundEvent.PermissionRequired:
                    return "Permission Required";
                case SoundEvent.PermissionGranted:
                    return "Permission Granted";
                case SoundEvent.MicrophoneReady:
                    return "Microphone Ready";
                case SoundEvent.SyllableStart:
                    return "Syllable Start";
                case SoundEvent.SyllableEnd:
                    return "Syllable End";
                case SoundEvent.InputStart:
                    return "Input Start";
                case SoundEvent.InputEnd:
                    return "Input End";
                case SoundEvent.AudioStart:
                    return "Audio Start";
                case SoundEvent.AudioEnd:
                    return "Audio End";
                case SoundEvent.SyllablePeak:
                    return "Syllable Peak";
            }
            return "Unrecognised Event";
        }

    }
}