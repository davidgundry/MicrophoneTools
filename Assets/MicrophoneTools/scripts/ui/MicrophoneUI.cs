using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public interface MicrophoneUI {

    /*
     *  This is called before first requesting user authorisation for use of the microphone.
     */
    void MicrophoneWarning();

    /*
     *  This is called if no available microphones were found when setting the microphone.
     */
    void NoMicrophonesFound();

    /*
     *  This is called if multiple devices were found when setting the microphone, and useDefaultMic is false.
     */
    void ChooseDevice(string[] devices);

    /*
     *  Should return whether MicrophoneController should call and wait for the UI's prewarning.
     */
    bool AskPermission();

    /*
     *  Should return whether UI should be called to pick which microphone to use
     */
    bool UseDefaultMic();

}
