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

    private string soundEvent;
    private long time;

    public EventRecord(SoundEvent e)
    {
        soundEvent = SoundEventToString(e);
        time = System.DateTime.Now.Ticks;
    }

    public EventRecord(string e)
    {
        soundEvent = e;
        time = System.DateTime.Now.Ticks;
    }

    public override string ToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("\"record\":{\"event\":\"");
        sb.Append(soundEvent);
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